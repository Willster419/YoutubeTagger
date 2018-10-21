ECHO OFF
rem display date for command
for /f "skip=1" %%x in ('wmic os get localdatetime') do if not defined MyDate set MyDate=%%x
set today=%MyDate:~0,4%%MyDate:~4,2%%MyDate:~6,2%
echo %today%


rem EDM_Treasures
set /p EDM_Treasures=<EDM_Treasures\part_playlist_download_settings.bat
set EDM_Treasures_1=%EDM_Treasures:~0,45%
set EDM_Treasures_2=%EDM_Treasures:~53%
rem modify and save command
set EDM_Treasures_3=%EDM_Treasures_1%%today%%EDM_Treasures_2%
rem save comamnd to disk
echo %EDM_Treasures_3%>EDM_Treasures\part_playlist_download_settings.bat

rem ElectroDanceMovement
set /p ElectroDanceMovement=<ElectroDanceMovement\part_playlist_download_settings.bat
set ElectroDanceMovement_1=%ElectroDanceMovement:~0,45%
set ElectroDanceMovement_2=%ElectroDanceMovement:~53%
rem modify and save command
set ElectroDanceMovement_3=%ElectroDanceMovement_1%%today%%ElectroDanceMovement_2%
rem save comamnd to disk
echo %ElectroDanceMovement_3%>ElectroDanceMovement\part_playlist_download_settings.bat

rem Eric_Clapman
set /p Eric_Clapman=<Eric_Clapman\part_playlist_download_settings.bat
set Eric_Clapman_1=%Eric_Clapman:~0,45%
set Eric_Clapman_2=%Eric_Clapman:~53%
rem modify and save command
set Eric_Clapman_3=%Eric_Clapman_1%%today%%Eric_Clapman_2%
rem save comamnd to disk
echo %Eric_Clapman_3%>Eric_Clapman\part_playlist_download_settings.bat

rem Hands_Up_Generation
set /p Hands_Up_Generation=<Hands_Up_Generation\part_playlist_download_settings.bat
set Hands_Up_Generation_1=%Hands_Up_Generation:~0,45%
set Hands_Up_Generation_2=%Hands_Up_Generation:~53%
rem modify and save command
set Hands_Up_Generation_3=%Hands_Up_Generation_1%%today%%Hands_Up_Generation_2%
rem save comamnd to disk
echo %Hands_Up_Generation_3%>Hands_Up_Generation\part_playlist_download_settings.bat

rem Hands_Up_Music
set /p Hands_Up_Music=<Hands_Up_Music\part_playlist_download_settings.bat
set Hands_Up_Music_1=%Hands_Up_Music:~0,45%
set Hands_Up_Music_2=%Hands_Up_Music:~53%
rem modify and save command
set Hands_Up_Music_3=%Hands_Up_Music_1%%today%%Hands_Up_Music_2%
rem save comamnd to disk
echo %Hands_Up_Music_3%>Hands_Up_Music\part_playlist_download_settings.bat

rem HHH_Sounds
set /p HHH_Sounds=<HHH_Sounds\part_playlist_download_settings.bat
set HHH_Sounds_1=%HHH_Sounds:~0,45%
set HHH_Sounds_2=%HHH_Sounds:~53%
rem modify and save command
set HHH_Sounds_3=%HHH_Sounds_1%%today%%HHH_Sounds_2%
rem save comamnd to disk
echo %HHH_Sounds_3%>HHH_Sounds\part_playlist_download_settings.bat

rem NCS_House
set /p NCS_House=<NCS_House\part_playlist_download_settings.bat
set NCS_House_1=%NCS_House:~0,45%
set NCS_House_2=%NCS_House:~53%
rem modify and save command
set NCS_House_3=%NCS_House_1%%today%%NCS_House_2%
rem save comamnd to disk
echo %NCS_House_3%>NCS_House\part_playlist_download_settings.bat

rem PARADISE_Mixes
set /p PARADISE_Mixes=<PARADISE_Mixes\part_playlist_download_settings.bat
set PARADISE_Mixes_1=%PARADISE_Mixes:~0,45%
set PARADISE_Mixes_2=%PARADISE_Mixes:~53%
rem modify and save command
set PARADISE_Mixes_3=%PARADISE_Mixes_1%%today%%PARADISE_Mixes_2%
rem save comamnd to disk
echo %PARADISE_Mixes_3%>PARADISE_Mixes\part_playlist_download_settings.bat

rem PARADISE_Releases
set /p PARADISE_Releases=<PARADISE_Releases\part_playlist_download_settings.bat
set PARADISE_Releases_1=%PARADISE_Releases:~0,45%
set PARADISE_Releases_2=%PARADISE_Releases:~53%
rem modify and save command
set PARADISE_Releases_3=%PARADISE_Releases_1%%today%%PARADISE_Releases_2%
rem save comamnd to disk
echo %PARADISE_Releases_3%>PARADISE_Releases\part_playlist_download_settings.bat

rem Waltrus_HandsUp
set /p Waltrus_HandsUp=<Waltrus_HandsUp\part_playlist_download_settings.bat
set Waltrus_HandsUp_1=%Waltrus_HandsUp:~0,45%
set Waltrus_HandsUp_2=%Waltrus_HandsUp:~53%
rem modify and save command
set Waltrus_HandsUp_3=%Waltrus_HandsUp_1%%today%%Waltrus_HandsUp_2%
rem save comamnd to disk
echo %Waltrus_HandsUp_3%>Waltrus_HandsUp\part_playlist_download_settings.bat

rem Waltrus_Hardstyle
set /p Waltrus_Hardstyle=<Waltrus_Hardstyle\part_playlist_download_settings.bat
set Waltrus_Hardstyle_1=%Waltrus_Hardstyle:~0,45%
set Waltrus_Hardstyle_2=%Waltrus_Hardstyle:~53%
rem modify and save command
set Waltrus_Hardstyle_3=%Waltrus_Hardstyle_1%%today%%Waltrus_Hardstyle_2%
rem save comamnd to disk
echo %Waltrus_Hardstyle_3%>Waltrus_Hardstyle\part_playlist_download_settings.bat
