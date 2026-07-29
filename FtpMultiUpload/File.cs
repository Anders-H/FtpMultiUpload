using Renci.SshNet;

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

    public bool Upload(StreamWriter log, string ftpTarget, SftpClient client)
    {
        var target = $"{ftpTarget}{(ftpTarget.EndsWith('/') ? "" : "/")}{ServerName}";
        CreateDirectories(log, target, client);

        try
        {
            using var fileStream = System.IO.File.OpenRead(Fullname);
            client.UploadFile(fileStream, target);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($@"Failed to upload ""{ServerName}"": ""{e.Message}""");
            log.WriteLine($@"Failed to upload ""{ServerName}"": ""{e.Message}""");
            return false;
        }
    }

    private static void CreateDirectories(TextWriter log, string target, SftpClient client)
    {
        try
        {
            var parts = target.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                return;

            var dir = "";

            for (var i = 0; i < parts.Length - 1; i++)
            {
                dir += "/" + parts[i];

                if (CreatedDirectories.Exists(x => x == dir))
                    continue;

                CreatedDirectories.Add(dir);

                if (client.Exists(dir))
                    continue;

                Console.WriteLine($"Create dir: {dir}");
                log.WriteLine($"Create dir: {dir}");

                try
                {
                    client.CreateDirectory(dir);
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