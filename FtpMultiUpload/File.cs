using System.Net;

namespace FtpMultiUpload;

public class File
{
    private static List<string> CreatedDirectories { get; } = new();
    public string Fullname { get; }
    public string BaseDirectory { get; }
    public string ServerName { get; }

    public File(string fullname, string baseDirectory)
    {
        Fullname = fullname;
        BaseDirectory = baseDirectory;
        ServerName = fullname.Replace(baseDirectory, "").Replace(@"\", "/");

        if (ServerName.StartsWith('/'))
            ServerName = ServerName[1..];
    }

    public bool Upload(StreamWriter log, string ftpTarget, WebClient client)
    {
        var target = $"{ftpTarget}{(ftpTarget.EndsWith('/') ? "" : "/")}{ServerName}";
        CreateDirectories(log, target, (NetworkCredential)client.Credentials!);
        
        try
        {
            client.UploadFile(target, "STOR", Fullname);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($@"Failed to upload ""{ServerName}"": ""{e.Message}""");
            log.WriteLine($@"Failed to upload ""{ServerName}"": ""{e.Message}""");
            return false;
        }
    }

    private static void CreateDirectories(TextWriter log, string target, NetworkCredential credential)
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
                    ftpRequest.Credentials = credential;
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