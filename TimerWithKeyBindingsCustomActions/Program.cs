namespace TimerWithKeyBindingsCustomActions
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimerWithKeyBindings");

            if (Directory.Exists(appDataFolder))
            {
                try
                {
                    Directory.Delete(appDataFolder, true);
                    Console.WriteLine("AppData folder removed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error removing AppData folder: {ex.Message}");
                }
            }
        }
    }
}
