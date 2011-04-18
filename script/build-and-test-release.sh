#! /bin/bash
find . -name obj -type d -exec rm -rfv {} \;
find . -name bin -type d -exec rm -rfv {} \;
rm -rf TestResult.xml
xbuild /p:Configuration=Release
nunit-color-console -labels "$@" bin/Release/FluentXml.Specs.dll
