// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using ExifLibrary;
using GoogleTakeoutDateFix.Google;
using MetadataExtractor;
using Directory = System.IO.Directory;

const string path = @"path_to_files";
IterateAllFiles(path);
Console.WriteLine("Done!");
Console.ReadLine();

void IterateAllFiles(string dir)
{
    foreach (var file in Directory.GetFiles(dir))
    {
        FixMeta(file);
    }

    foreach (var directory in Directory.GetDirectories(dir))
    {
        IterateAllFiles(directory);
    }
}

void FixMeta(string file)
{
    var ext = Path.GetExtension(file).ToLowerInvariant();
    if (ext != ".jpg" && ext != ".jpeg") return;
    FixJpgMeta(file);
}

void FixJpgMeta(string file)
{
    var meta = ImageMetadataReader.ReadMetadata(file);
    var exif = meta.FirstOrDefault(x => x.Name == "Exif IFD0");
    var exifSub = meta.FirstOrDefault(x => x.Name == "Exif SubIFD");
    var dateTime = exif?.Tags.FirstOrDefault(x => x.Name == "Date/Time");
    var dateTimeOriginal = exifSub?.Tags.FirstOrDefault(x => x.Name == "Date/Time Original");
    if (dateTime != null && dateTimeOriginal != null) return;

    var googleDate = GetGooglePhotoTakenTime(file);
    if (googleDate == null) return;

    var formatted = googleDate.Value.ToString("yyyy:MM:dd HH:mm:ss");
    var imageFile = ImageFile.FromFile(file);
    if (dateTime == null)
    {
        imageFile.Properties.Set(ExifTag.DateTime, formatted);
    }
    if (dateTimeOriginal == null)
    {
        imageFile.Properties.Set(ExifTag.DateTimeOriginal, formatted);
    }
    imageFile.Save(file);
}

DateTime? GetGooglePhotoTakenTime(string file)
{
    var jsonPath = file + ".json";
    if (!File.Exists(jsonPath)) return null;

    var json = File.ReadAllText(jsonPath);
    var googleMeta = JsonSerializer.Deserialize<GoogleMeta>(json);
    var timestamp = googleMeta?.photoTakenTime?.timestamp;
    if (string.IsNullOrEmpty(timestamp)) return null;

    var unixTimestamp = int.Parse(timestamp);
    var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    dateTime = dateTime.AddSeconds(unixTimestamp);
    return dateTime;
}