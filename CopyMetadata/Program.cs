// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Directory = System.IO.Directory;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

const string metaDir = @"d:\GOPRO\raw\";
const string streamDir = @"d:\GOPRO\converted\";
const string fixedDir = @"d:\GOPRO\fixed\";

IterateAllFiles(metaDir);
Console.WriteLine("Done!");
Console.ReadLine();

void IterateAllFiles(string dir)
{
    foreach (var file in Directory.GetFiles(dir))
    {
        CopyMeta(file);
    }
}

void CopyMeta(string metaFile)
{
    var filename = Path.GetFileName(metaFile)
        .Replace("X", "x")
        .Replace("MP4", "m4v");
    
    var streamFile = Path.Combine(streamDir, filename);
    var fixedFile = Path.Combine(fixedDir, filename);

    var command = $"ffmpeg -i {metaFile} -i {streamFile} -map_metadata 0 -map 1 -c copy {fixedFile}";
    var process = Process.Start(new ProcessStartInfo("cmd.exe", "/K " + command)
    {
        CreateNoWindow = true,
        UseShellExecute = true
    });
}