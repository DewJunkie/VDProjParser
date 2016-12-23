cd "%~dp0%"
for /f "delims=" %%A in ('ProjectParser setup.vdproj -q=//ProductVersion') do set "version=%%A"
echo %version%
set name=ProjectParser

pushd Release

rem create archives
rem "c:\Program Files\7-Zip\7z" a -tzip -mx=9 DvrServerSetup-%version%.zip *
"c:\Program Files\7-Zip\7z" a -t7z -mx=9 %name%-%version%.7z *
copy /b /y "C:\Program Files\7-Zip\7zs.sfx" + ..\config.txt + %name%-%version%.7z %name%-%version%.exe

rem Move to destination
rem move DvrServerSetup-%version%.zip ..\
del /y %name%-%version%.7z ..\
move /y %name%-%version%.exe ..\
popd

rem unfortunately, once a build is started, the file is loaded, and changes don't take effect.
rem the best we can do is get it setup for the next time.
ProjectParser setup.vdproj -o=setup.vdproj --increment

