#!/bin/bash
set -e

die () {
	echo "$1"
	exit 1
}

parse_token () {
	echo "$2" | sed -nre "/^\\s*\"$1\":\\s*\"?/{s///;s/\"?,?\\s*$//;p}"
}

user="$1"
[ -z "$user" ] && die "GitHub username required"
tag="$2"
version="${tag#v*}"
[ -z "$tag" ] && die "Version tag required"
[ -n "$(git tag -l "$tag")" ] && die "Tag already exists"

echo "Bumping version..."
echo "$tag" > ./version
sed -ri "s/^(\[assembly:\s+AssemblyVersion)\(\"[^\"]+\"\)/\1(\"$version.*\")/" \
       ItemCollageUI/Properties/AssemblyInfo.cs

echo "Compiling..."
cmd "/c C:\\Windows\\Microsoft.Net\\Framework\\v4.0.30319\\MSBuild.exe \
            /nologo /p:Configuration=Release /verbosity:q"

file="ItemCollage-$tag.exe"
path="ItemCollageUI/bin/Release/ItemCollage-$tag.exe"
mv ItemCollageUI/bin/Release/ItemCollage.exe "$path"

echo "Preparing commit and tag"
git add version
git commit -m "Bump version to $tag"
git tag "$tag"

echo "Uploading file"
size="$(du -b "$path" | awk '{print $1}')"

resp=$(curl -s -u "$user" \
            -d "{ \"name\": \"$file\", \
                 \"size\": $size, \
                 \"description\": \"Testing build script $tag\" }" \
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
