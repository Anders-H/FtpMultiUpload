using System.Net;
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

var files = new List<File>();
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
                    files.Insert(0, new File(f.FullName, baseDirectory));
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

public class File
{
    private static List<string> CreatedDirectories { get; }
    public string Fullname { get; }
    public string BaseDirectory { get; }
    public string ServerName { get; }

    static File()
    {
        CreatedDirectories = new List<string>();
    }

    public File(string fullname, string baseDirectory)
    {
        Fullname = fullname;
        BaseDirectory = baseDirectory;
        ServerName = fullname.Replace(baseDirectory, "").Replace(@"\", @"/");

        if (ServerName.StartsWith("/"))
            ServerName = ServerName.Substring(1);
    }

    public void Upload(StreamWriter log, string ftpTarget, string ftpUsername, string ftpPassword)
    {
        var target = $"{ftpTarget}{(ftpTarget.EndsWith(@"/") ? "" : @"/")}{ServerName}";
        CreateDirectories(log, target, ftpUsername, ftpPassword);

        try
        {
            using var client = new WebClient();
            client.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
            client.UploadFile(target, "STOR", Fullname);
        }
        catch (Exception e)
        {
            Console.WriteLine($@"Failed to upload ""{ServerName}"": ""{e.Message}""");
            log.WriteLine($@"Failed to upload ""{ServerName}"": ""{e.Message}""");
        }
    }

    private static void CreateDirectories(StreamWriter log, string target, string ftpUsername, string ftpPassword)
    {
        try
        {
            var parts = target.Split('/');

            if (parts.Length < 3)
                return;

            var dir = "ftp:/";

            for (var i = 2; i < parts.Length - 1; i++)
            {
                dir += "/";
                dir += parts[i];

                if (i < 3)
                    continue;

                if (CreatedDirectories.Exists(x => x == dir))
                    continue;

                Console.WriteLine($"Create dir: {dir}");
                log.WriteLine($"Create dir: {dir}");
                CreatedDirectories.Add(dir);

                try
                {
                    var ftpRequest = (FtpWebRequest)WebRequest.Create(dir);
                    ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                    ftpRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                    using var response = (FtpWebResponse)ftpRequest.GetResponse();
                    Console.WriteLine(response.StatusCode);
                    log.WriteLine(response.StatusCode);
                    response.Close();
                }
                catch (Exception createException)
                {
                    Console.WriteLine($"Failed to create directory on target machine: {dir}");
                    Console.WriteLine($"{createException.GetType().Name}: {createException.Message}");
                    log.WriteLine($"Failed to create directory on target machine: {dir}");
                    log.WriteLine($"{createException.GetType().Name}: {createException.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed in create directory method: {target}");
            Console.WriteLine($"{e.GetType().Name}: {e.Message}");
            log.WriteLine($"Failed in create directory method: {target}");
            log.WriteLine($"{e.GetType().Name}: {e.Message}");
        }
    }
}