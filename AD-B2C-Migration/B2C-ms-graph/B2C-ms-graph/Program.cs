// See https://aka.ms/new-console-template for more information
using Azure.Identity;
using B2C_ms_graph.Models;
using B2C_ms_graph.Services;
using Microsoft.Graph;

Console.WriteLine("Hello, World!");
AppSettings config = AppSettingsFile.ReadFromJsonFile();

// Initialize the client credential auth provider
var scopes = new[] { "https://graph.microsoft.com/.default" };
var clientSecretCredential = new ClientSecretCredential(config.TenantId, config.AppId, config.ClientSecret);
var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
//</ms_docref_set_auth_provider>
PrintCommands();


while (true)
{
    Console.Write("Enter command, then press ENTER: ");
    string decision = Console.ReadLine();
    switch (decision.ToLower())
    {
        case "1":
            await UserService.ListUsers(graphClient);
            break;
        case "2":
            await UserService.GetUserById(graphClient);
            break;
        case "3":
            await UserService.GetUserBySignInName(config, graphClient);
            break;
        case "4":
            await UserService.DeleteUserById(graphClient);
            break;
        case "5":
            await UserService.SetPasswordByUserId(graphClient);
            break;
        case "6":
            await UserService.BulkCreate(config, graphClient);
            break;
        case "7":
            await UserService.CreateUser(graphClient, config.TenantId);
            break;
        case "8":
            await UserService.CreateUserWithCustomAttribute(graphClient, config.B2cExtensionAppClientId, config.TenantId);
            break;
        case "9":
            await UserService.ListUsersWithCustomAttribute(graphClient, config.B2cExtensionAppClientId);
            break;
        case "10":
            await UserService.CountUsers(graphClient);
            break;
        case "help":
           PrintCommands();
            break;
        case "exit":
            return;
        default:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid command. Enter 'help' to show a list of commands.");
            Console.ResetColor();
            break;
    }

    Console.ResetColor();
}

         static void PrintCommands()
        {
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Command  Description");
            Console.WriteLine("====================");
            Console.WriteLine("[1]      Get all users");
            Console.WriteLine("[2]      Get user by object ID");
            Console.WriteLine("[3]      Get user by sign-in name");
            Console.WriteLine("[4]      Delete user by object ID");
            Console.WriteLine("[5]      Update user password");
            Console.WriteLine("[6]      Create users (bulk import)");
            Console.WriteLine("[7]      Create user");
            Console.WriteLine("[8]      Create user with custom attributes and show result");
            Console.WriteLine("[9]      Get all users (one page) with custom attributes");
            Console.WriteLine("[10]      Get the number of useres in the directory");
            Console.WriteLine("[help]   Show available commands");
            Console.WriteLine("[exit]   Exit the program");
            Console.WriteLine("-------------------------");
        }