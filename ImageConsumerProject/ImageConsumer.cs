using MassTransit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
public class ImageConsumer : IConsumer<ModelLibrary.ImageMessageRabbitMQ>
{
    public Task Consume(ConsumeContext<ModelLibrary.ImageMessageRabbitMQ> context)
    {
        var filePath = context.Message.FilePath;
        // Process the message
        System.Console.WriteLine($"Processing image: {filePath}");
        ResizeImageOnPhysicalMachine(filePath, filePath, 250, 200);
        return Task.CompletedTask;
    }

    public static void ResizeImageOnPhysicalMachine(string inputPath, string outputPath, int width, int height)
    {
        // Ensure that inputPath and outputPath are not null
        if (string.IsNullOrEmpty(inputPath)) throw new ArgumentNullException(nameof(inputPath));
        if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException(nameof(outputPath));

        // Get the directory and filename parts of the outputPath
        var outputDirectory = Path.GetDirectoryName(outputPath);
        var outputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputPath);
        var outputFileExtension = Path.GetExtension(outputPath);

        // Ensure that outputDirectory and outputFileNameWithoutExtension are not null
        if (outputDirectory == null) throw new ArgumentException("Invalid output path: directory name is null.", nameof(outputPath));
        if (outputFileNameWithoutExtension == null) throw new ArgumentException("Invalid output path: file name without extension is null.", nameof(outputPath));

        // Create a temporary file path
        string tempOutputPath = Path.Combine(outputDirectory, outputFileNameWithoutExtension + "_temp" + outputFileExtension);

        using (FileStream inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
        {
            using (var image = Image.Load(inputStream))
            {
                image.Mutate(x => x.Resize(width, height));
                string extension = Path.GetExtension(inputPath).ToLower();

                using (FileStream outputStream = new FileStream(tempOutputPath, FileMode.Create, FileAccess.Write))
                {
                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            image.SaveAsJpeg(outputStream, new JpegEncoder { Quality = 90 });
                            break;
                        case ".png":
                            image.SaveAsPng(outputStream);
                            break;
                        default:
                            throw new NotSupportedException("Unsupported image format: " + extension);
                    }
                }
            }
        }

        // Replace the original file with the resized file
        File.Delete(outputPath);
        File.Move(tempOutputPath, outputPath);
    }




}
