#! /bin/bash
find . -name obj -type d -exec rm -rfv {} \;
find . -name bin -type d -exec rm -rfv {} \;
rm -rf TestResult.xml
xbuild
nunit-color-console -labels "$@" bin/Debug/FluentXml.Specs.dll
