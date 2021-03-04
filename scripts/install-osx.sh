#!/bin/sh
set -e

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/.. ; pwd -P )"

dotnet publish -r osx-x64 $ROOT/client/cli -v quiet --nologo -p:PublishSingleFile=true
dotnet publish -r osx-x64 $ROOT/client/daemon -v quiet --nologo -p:PublishSingleFile=true

rm -rf /usr/local/share/gitpolicy/cli /usr/local/share/gitpolicy/daemon
mkdir -p /usr/local/share/gitpolicy/cli /usr/local/share/gitpolicy/daemon

cp -r $ROOT/client/cli/bin/Debug/net5.0/osx-x64/publish/ /usr/local/share/gitpolicy/cli
cp -r $ROOT/client/daemon/bin/Debug/net5.0/osx-x64/publish/ /usr/local/share/gitpolicy/daemon

if [ -e /usr/local/bin/git-policy ]; then
	rm /usr/local/bin/git-policy
fi

ln -s /usr/local/share/gitpolicy/cli/git-policy /usr/local/bin/git-policy

if [ -e ~/Library/LaunchAgents/com.mjcheetham.gitpolicy.daemon.plist ]; then
	rm ~/Library/LaunchAgents/com.mjcheetham.gitpolicy.daemon.plist
fi

cp $ROOT/client/daemon/com.mjcheetham.gitpolicy.daemon.plist ~/Library/LaunchAgents
