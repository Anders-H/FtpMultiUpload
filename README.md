# FtpMultiUpload

SFTP uploader (not FTP) for multiple files, written for .NET 10.

Uploads recently changed web files (html/css...)

Arguments:

`FtpMultiUpload.exe TargetAddress Username Password FtpRootPath SourceDirectory LogFilename`

Files that exist in the source directory will overwrite any files on the target, and the log file will overwrite any existing file.

Example:

`FtpMultiUpload.exe mysite.com sven p@ssw0rd "customers/wwwroot/me" C:\MyFiles C:\Temp\FtpMultiUpload.log`
