using System.Text;
using FileUtility;

if (args.Length != 5)
{
    ShowHelp();
    return;
}

var ftpTarget = args[0];
var ftpUsername = args[1];
var ftpPassword = args[2];
var sourceDirectory = new DirectoryInfo(args[3]);
var logFilePath = args[4];

if (!sourceDirectory.Exists)
{
    Console.WriteLine($"Directory does not exist: {sourceDirectory.FullName}");
    return;
}

var files = new List<FtpMultiUpload.File>();
var filesOnDiskCount = 0;
var tooOldCount = 0;
var wrongTypeCount = 0;

var i = 1;
using var log = new StreamWriter(logFilePath, false, Encoding.UTF8);
AddFilesFromDirectory(sourceDirectory, sourceDirectory.FullName);

Console.WriteLine($"Files on disk: {filesOnDiskCount}");
Console.WriteLine($"Too old to include: {tooOldCount}");
Console.WriteLine($"Wrong type: {wrongTypeCount}");
log.WriteLine($"Files on disk: {filesOnDiskCount}");
log.WriteLine($"Too old to include: {tooOldCount}");
log.WriteLine($"Wrong type: {wrongTypeCount}");

var startTime = DateTime.Now;
log.WriteLine($"Start time: {startTime:yyyy-MM-dd hh:mm:ss}");
Console.WriteLine($"Start time: {startTime:yyyy-MM-dd hh:mm:ss}");

foreach (var file in files)
{
    log.WriteLine($"{i:000}/{files.Count:000}: Uploading {file.ServerName}");
    Console.WriteLine($"{i:000}/{files.Count:000}: Uploading {file.ServerName}");
    file.Upload(log, ftpTarget, ftpUsername, ftpPassword);
    i++;
}


var endTime = DateTime.Now;
log.WriteLine($"End time: {endTime:yyyy-MM-dd hh:mm:ss}");
Console.WriteLine($"End time: {endTime:yyyy-MM-dd hh:mm:ss}");

log.Flush();
log.Close();

void AddFilesFromDirectory(DirectoryInfo dir, string baseDirectory)
{
    const double ageInHours = 1.0;

    var allFiles = new PathInfo(dir).GetDirectoryContent(20);

    foreach (var f in allFiles)
    {
        if (!f.ContainsFile)
            continue;

        var file = f.FileInfo!;

        filesOnDiskCount++;
        switch (file.Extension.ToLower())
        {
            case ".html":
            case ".css":
            case ".xml":
            case ".js":
            case ".json":
            case ".rss":
            case ".jpg":
            case ".gif":
            case ".png":
                if (DateTime.Now.Subtract(file.LastWriteTime).TotalHours < ageInHours)
                {
                    files.Insert(0, new FtpMultiUpload.File(f.FullName, baseDirectory));
                    Console.WriteLine($"File added: {f.CompactPathForDisplay(40)}");
                    log.WriteLine($"File added: {f.CompactPathForDisplay(40)}");
                }
                else
                {
                    tooOldCount++;
                    Console.WriteLine($"File unchanged: {f.CompactPathForDisplay(40)}");
                    log.WriteLine($"File unchanged: {f.CompactPathForDisplay(40)}");
                }
                break;
            case ".htaccess":
            case ".ico":
                Console.WriteLine($"Ignoring file: {f.CompactPathForDisplay(40)}");
                log.WriteLine($"Ignoring file: {f.CompactPathForDisplay(40)}");
                break;
            default:
                Console.WriteLine($"Unexpected file extension: {file.Extension} on file {f.CompactPathForDisplay(40)}");
                log.WriteLine($"Unexpected file extension: {file.Extension} on file {f.CompactPathForDisplay(40)}");
                wrongTypeCount++;
                break;
        }
    }
}

void ShowHelp() =>
    Console.WriteLine(@"Arguments:
 - Target address (ftp://ftp.mysite.com/myfolder - existing files will be overwritten)
 - FTP username
 - FTP password
 - Source directory (""C:\MyFiles"")
 - Log filename (""C:\Temp\FtpMultiUpload.log"" - will be overwritten)");