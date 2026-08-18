using SheetMusic.Api.Users.Errors;
using SheetMusic.Api.Users.RequestModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Services;

public sealed class ProfilePictureProcessor
{
    public const int MaxFileSize = 5 * 1024 * 1024;
    private const int MaxDimension = 4096;
    private const int OutputDimension = 512;

    public async Task<MemoryStream> ProcessAsync(Stream source, ProfilePictureCropRequest crop, CancellationToken cancellationToken)
    {
        await using var input = await CopyBoundedAsync(source, cancellationToken);
        input.Position = 0;

        IImageFormat format;
        try
        {
            format = await Image.DetectFormatAsync(input, cancellationToken)
                ?? throw new InvalidProfilePictureError("The uploaded file is not a supported image");
        }
        catch (InvalidProfilePictureError)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidProfilePictureError("The uploaded file is not a valid image");
        }

        if (format is not JpegFormat and not PngFormat and not WebpFormat)
            throw new InvalidProfilePictureError("Profile pictures must be JPEG, PNG, or WebP images");

        input.Position = 0;
        try
        {
            using var image = await Image.LoadAsync(input, cancellationToken);
            if (image.Width > MaxDimension || image.Height > MaxDimension)
                throw new InvalidProfilePictureError($"Profile pictures cannot exceed {MaxDimension} x {MaxDimension} pixels");

            if (image.Frames.Count != 1)
                throw new InvalidProfilePictureError("Animated profile pictures are not supported");

            image.Mutate(context => context.AutoOrient());
            ValidateCrop(image.Width, image.Height, crop);
            image.Mutate(context => context.Crop(new Rectangle(crop.X, crop.Y, crop.Size, crop.Size)).Resize(OutputDimension, OutputDimension));

            var output = new MemoryStream();
            await image.SaveAsWebpAsync(output, new WebpEncoder(), cancellationToken);
            output.Position = 0;
            return output;
        }
        catch (InvalidProfilePictureError)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidProfilePictureError("The uploaded file is not a valid image");
        }
    }

    private static void ValidateCrop(int width, int height, ProfilePictureCropRequest crop)
    {
        if (crop.Size <= 0 || crop.X < 0 || crop.Y < 0 || crop.X > width - crop.Size || crop.Y > height - crop.Size)
            throw new InvalidProfilePictureError("The submitted crop must be a square within the source image");
    }

    private static async Task<MemoryStream> CopyBoundedAsync(Stream source, CancellationToken cancellationToken)
    {
        var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (output.Length + read > MaxFileSize)
            {
                await output.DisposeAsync();
                throw new InvalidProfilePictureError("Profile pictures cannot exceed 5 MB");
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return output;
    }
}