# FtpMultiUpload

Uploads recently changed web files (html/css...)

Arguments:

`FtpMultiUpload.exe TargetAddress Username Password SourceDirectory LogFilename`

Files that exist in the source directory will overwrite any files on the target, and the log file will overwrite any existing file.

Example:

`FtpMultiUpload.exe ftp://ftp.mysite.com/myfiles sven p@ssw0rd C:\MyFiles C:\Temp\FtpMultiUpload.log`
