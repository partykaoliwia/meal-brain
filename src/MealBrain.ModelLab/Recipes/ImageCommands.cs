namespace MealBrain.ModelLab.Recipes;

public static class ImageCommands
{
    public static bool Handles(string command) => command is "--extract-image" or "--image-suite" or
        "--check-image-suite" or "--freeze-image-suite";

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args is ["--extract-image", var model, var image])
                return await ImageExtractionConsole.RunAsync(model, image, false);
            if (args is ["--image-suite", var suiteModel, var manifest])
                return await ImageExtractionConsole.RunAsync(suiteModel, manifest, true);
            if (args.Length == 2 && args[0] is "--check-image-suite" or "--freeze-image-suite")
                return ImageExtractionConsole.Prepare(args[0], args[1]);
            Console.Error.WriteLine("S1: --extract-image <model> <obraz.png/jpg> | --image-suite <model> <suite.json> | --check-image-suite <suite.json> | --freeze-image-suite <suite.json>");
            return 1;
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("S1: " + ex.Message);
            return 1;
        }
    }
}
