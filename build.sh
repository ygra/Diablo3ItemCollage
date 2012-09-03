#!/bin/bash
set -e

die () {
	echo "$1"
	exit 1
}

parse_token () {
	echo "$2" | sed -nre "/^\\s*\"$1\":\\s*\"?/{s///;s/\"?,?\\s*$//;p}"
}

if [[ "$1" == "-o" ]]; then
  # don't do any git stuff, don't upload the build
	offline=1
	shift
fi

user="$1"
[ -z "$user" ] && die "GitHub username required"

tag="$2"
[ -z "$tag" ] && die "Version tag required"

nightly=""
if [[ "$tag" = "nightly" ]]; then
	tag="$(git describe)"
	# cut off revision number
	tag="${tag%-*}"
	nightly=1
else
	[ -n "$(git tag -l "$tag")" ] && die "Tag already exists"
fi

# remove v prefix
version="${tag#v*}"
# use revision as build number
version="${version//-/.}"

echo "Compiling..."
cmd "/c C:\\Windows\\Microsoft.Net\\Framework\\v4.0.30319\\MSBuild.exe \
            /nologo /p:Configuration=Release /verbosity:q"

echo "Running tests..."
if ! Test/bin/Release/Test.exe -q; then
  echo "Tests failed!"
  exit 1
fi

echo "Bumping version..."
# echo \r\n so git doesn't complain about line endings
echo -ne "$version\r\n" > ./version
sed -bri "s/^(\[assembly:\s+AssemblyVersion)\(\"[^\"]+\"\)/\1(\"$version.0\")/" \
       ItemCollageUI/Properties/AssemblyInfo.cs

file="ItemCollage-$tag.exe"
path="ItemCollageUI/bin/Release/$file"
mv ItemCollageUI/bin/Release/ItemCollage.exe "$path"

[[ -n "$offline" ]] && exit 0

echo "Preparing commit and tag"
git add version
git add ItemCollageUI/Properties/AssemblyInfo.cs
git commit -m "Bump version to $tag"
[ -n "$nightly"] && git tag "$tag"

echo "Uploading file"
size="$(du -b "$path" | awk '{print $1}')"

resp=$(curl -s -u "$user" \
            -d "{ \"name\": \"$file\", \
                 \"size\": $size, \
                 \"description\": \"Release version $tag\" }" \
            https://api.github.com/repos/ygra/Diablo3ItemCollage/downloads
      )

if [ -z "$(parse_token acl "$resp")" ]; then
	echo "Failed to get AWS token"
	echo "$resp"
	die "Bad credentials?"
fi

awsresp="$(curl -F "key=$(parse_token path "$resp")" \
                -F "acl=$(parse_token acl "$resp")" \
                -F "success_action_status=201" \
                -F "Filename=$(parse_token name "$resp")" \
                -F "AWSAccessKeyId=$(parse_token accesskeyid "$resp")" \
                -F "Policy=$(parse_token policy "$resp")" \
                -F "Signature=$(parse_token signature "$resp")" \
                -F "Content-Type=$(parse_token mime_type "$resp")" \
                -F "file=@${path}" \
                -s -w "\n%{http_code}" \
                https://github.s3.amazonaws.com/)"

[[ "$(echo "$awsresp" | tail -1)" != "201" ]] && die "Upload failed: $awsresp"

echo "Done! Remember to push to GitHub with"
echo "   git push --tags origin master"
