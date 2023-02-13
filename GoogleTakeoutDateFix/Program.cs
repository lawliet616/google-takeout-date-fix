// See https://aka.ms/new-console-template for more information

using System.Globalization;
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
    foreach (var file in Directory.GetFiles(dir).OrderBy(x => x))
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
    if (file.EndsWith(".json")) return;

    var ext = Path.GetExtension(file).ToLowerInvariant();
    if (ext is ".jpg" or ".jpeg" or ".avi" or ".png") return;

    if (ext is ".jpg" or ".jpeg")
    {
        Console.WriteLine($"Fix .jpg: {file}");
        FixJpgMeta(file);
        return;
    }
    
    if (ext is ".png")
    {
        Console.WriteLine($"Fix .png: {file}");
        FixPngMeta(file);
        return;
    }

    if (ext is ".avi")
    {
        Console.WriteLine($"Fix .avi: {file}");
        FixAviMeta(file);
        return;
    }

    if (ext is ".m4v")
    {
        Console.WriteLine($"Fix .m4v: {file}");
        FixM4vMeta(file);
        return;
    }

    Console.WriteLine($"Skip: {file}");
}

void FixJpgMeta(string file)
{
    var meta = ImageMetadataReader.ReadMetadata(file);
    var exif = meta.FirstOrDefault(x => x.Name == "Exif IFD0");
    var exifSub = meta.FirstOrDefault(x => x.Name == "Exif SubIFD");
    var dateTimeTag = exif?.Tags.FirstOrDefault(x => x.Name == "Date/Time");
    var dateTimeOriginalTag = exifSub?.Tags.FirstOrDefault(x => x.Name == "Date/Time Original");

    var googleDate = GetGooglePhotoTakenTimeUtc(file);
    var dateTaken = googleDate ?? ParseDateTimeFromExifDescriptionUtc(dateTimeOriginalTag);
    if (dateTaken == null) return;

    if (dateTimeTag == null || dateTimeOriginalTag == null)
    {
        var formatted = dateTaken.Value.ToString("yyyy:MM:dd HH:mm:ss");
        var imageFile = ImageFile.FromFile(file);
        imageFile.Properties.Set(ExifTag.DateTime, formatted);
        imageFile.Properties.Set(ExifTag.DateTimeOriginal, formatted);
        imageFile.Save(file);
    }

    FixFileStats(file, dateTaken.Value);
}

void FixPngMeta(string file)
{
    var meta = ImageMetadataReader.ReadMetadata(file);
    var createTag = meta
        .Where(x => x.Name == "PNG-tEXt")
        .SelectMany(x => x.Tags)
        .FirstOrDefault(x => x.Description != null && x.Description.StartsWith("date:create: 2012-05-19T06:26:11-05:00"));
    var createdAtStr = createTag?.Description?.Replace("date:create: ", "");
    
    var googleDate = GetGooglePhotoTakenTimeUtc(file);
    var dateTaken = googleDate ?? ParseDateTimeUtc(createdAtStr, "yyyy-MM-ddTHH:mm:sszzz");
    if (dateTaken == null) return;

    FixFileStats(file, dateTaken.Value);
}

void FixAviMeta(string file)
{
    var meta = ImageMetadataReader.ReadMetadata(file);
    var avi = meta.FirstOrDefault(x => x.Name == "AVI");
    var dateTimeOriginalTag = avi?.Tags.FirstOrDefault(x => x.Name == "Date/Time Original");

    var googleDate = GetGooglePhotoTakenTimeUtc(file);
    var dateTaken = googleDate ?? ParseDateTimeFromExifDescriptionUtc(dateTimeOriginalTag);
    if (dateTaken == null) return;

    FixFileStats(file, dateTaken.Value);
}

void FixM4vMeta(string file)
{
    var meta = ImageMetadataReader.ReadMetadata(file);
    var avi = meta.FirstOrDefault(x => x.Name == "AVI");
    var dateTimeOriginalTag = avi?.Tags.FirstOrDefault(x => x.Name == "Date/Time Original");

    var googleDate = GetGooglePhotoTakenTimeUtc(file);
    var dateTaken = googleDate ?? ParseDateTimeFromExifDescriptionUtc(dateTimeOriginalTag);
    if (dateTaken == null) return;

    FixFileStats(file, dateTaken.Value);
}

void FixFileStats(string file, DateTime dateTakenUtc)
{
    File.SetCreationTimeUtc(file, dateTakenUtc);
    File.SetLastWriteTimeUtc(file, dateTakenUtc);
}

DateTime? ParseDateTimeFromExifDescriptionUtc(Tag? tag)
{
    if (tag == null) return null;
    return ParseDateTimeUtc(tag.Description, "yyyy:MM:dd HH:mm:ss");
}

DateTime? ParseDateTimeUtc(string? value, string pattern)
{
    if (string.IsNullOrEmpty(value)) return null;
    if (DateTime.TryParseExact(value, pattern,
            CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var dateTime)) return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    return null;
}

DateTime? GetGooglePhotoTakenTimeUtc(string file)
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