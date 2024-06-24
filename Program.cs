using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerAuthChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse command-line arguments
            if (args.Length < 2 || args.Length > 4 || args.Length % 2 != 0)
            {
                Console.WriteLine("Usage: ControllerAuthChecker -a <assembly-path> [-n <controller-name-filter>]");
                return;
            }

            string? assemblyPath = null;
            string? controllerNameFilter = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-a" && i + 1 < args.Length)
                {
                    assemblyPath = args[i + 1];
                    i++;
                }
                else if (args[i] == "-n" && i + 1 < args.Length)
                {
                    controllerNameFilter = args[i + 1];
                    i++;
                }
            }

            if (assemblyPath == null)
            {
                Console.WriteLine("Error: Assembly path is required.");
                return;
            }

            if (!File.Exists(assemblyPath))
            {
                Console.WriteLine($"Error: Assembly not found at path '{assemblyPath}'.");
                return;
            }

            CheckControllers(assemblyPath, controllerNameFilter);
        }

        static void CheckControllers(string assemblyPath, string? controllerNameFilter)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Could not load assembly from path '{assemblyPath}'. Exception: {ex.Message}");
                return;
            }

            // Find all controller types
            var controllerTypes = assembly.GetTypes()
                                          .Where(type => typeof(ControllerBase).IsAssignableFrom(type) 
                                                      && type.IsPublic 
                                                      && !type.IsAbstract);

            // Apply the controller name filter if provided
            if (!string.IsNullOrEmpty(controllerNameFilter))
            {
                controllerTypes = controllerTypes.Where(type => type.Name.Contains(controllerNameFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Dictionary to store controllers and their actions
            Dictionary<string, List<string>> controllerActions = new Dictionary<string, List<string>>();

            // Process each controller
            foreach (var controller in controllerTypes)
            {
                var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                        .Where(method => method.IsPublic 
                                                      && !method.IsDefined(typeof(NonActionAttribute))
                                                      && !method.IsDefined(typeof(AuthorizeAttribute)))
                                        .Select(method => method.Name)
                                        .ToList();

                if (actions.Count != 0)
                {
                    controllerActions[controller.Name] = actions;
                }
            }

            // Output results
            if (controllerActions.Count == 0)
            {
                Console.WriteLine("No controllers found with actions missing [Authorize] attribute.");
            }
            else
            {
                foreach (var kvp in controllerActions)
                {
                    Console.WriteLine($"------------------------------------------------------------------------------");
                    Console.WriteLine($"Checking controller: {kvp.Key}");
                    Console.WriteLine($"------------------------------------------------------------------------------");

                    foreach (var action in kvp.Value)
                    {
                        Console.WriteLine($"Action: {action}\t  Missing [Authorize] attribute!!!");
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("Check completed.");
        }
    }
}
