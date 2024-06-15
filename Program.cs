using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using TechnoDanger;

public class Program
{
    private static readonly List<TDAction> GlobalActions = new List<TDAction>();
    private static Dictionary<string, (string command, string args)> aliases = new Dictionary<string, (string, string)>();
    private static readonly string AliasFilePath = "aliases.txt";
    private static readonly List<string> commandHistory = new List<string>();
    private static readonly string CommandFilePath = "commands.txt";
    private static string currentDirectory = Directory.GetCurrentDirectory();

    public static void Main()
    {
        Console.BackgroundColor = ConsoleColor.Green;
        Console.WriteLine("Compiling...");
        Console.BackgroundColor = ConsoleColor.Black;
        Console.Clear();
        Console.Title = "TD Console";

        LoadAliasesFromFile();
        LoadCommandsFromFile();
        InitConsoleBehaviour();

        while (true)
        {

            Console.Title = $"TD Console {DateTime.Now.ToString("yyyy-MM-dd")}";
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write($"TD> ");
            Console.ForegroundColor = ConsoleColor.White;
            string? userInput = Console.ReadLine();

            if (string.IsNullOrEmpty(userInput))
            {
                Console.WriteLine("Input is empty. Please try again.");
                continue;
            }

            commandHistory.Add(userInput);

            string[] inputParts = userInput.Split(new[] { ' ' }, 2);
            string command = inputParts[0].Trim();
            string args = inputParts.Length > 1 ? inputParts[1].Trim() : string.Empty;

            if (aliases.ContainsKey(command))
            {
                var (originalCommand, aliasArgs) = aliases[command];
                command = originalCommand;
                args = !string.IsNullOrEmpty(aliasArgs) ? $"{aliasArgs} {args}".Trim() : args;
            }

            var foundAction = GlobalActions.Find(action => action.ActionName.Equals(command, StringComparison.OrdinalIgnoreCase));

            if (foundAction != null)
            {
                string result = foundAction.Execute(args);
                Console.WriteLine(result);
            }
            else
            {
                Console.WriteLine("Action not found!");
            }
        }
    }

    public static void InitConsoleBehaviour()
    {
        GlobalActions.Add(new TDAction("exit", "Console shutdown", () =>
        {
            Environment.Exit(0);
            return "";
        }));

        GlobalActions.Add(new TDAction("call", "Launches a program. Usage: call \\\"<path_to_executable>\\\" [args]", filePathAndArgs =>
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(filePathAndArgs, "^\"(?<path>[^\"]+)\"(?:(?:\\s)(?!$)(?<args>.+))?$");
                string filePath, args;

                if (match.Success)
                {
                    filePath = match.Groups["path"].Value;
                    args = match.Groups["args"].Value;
                }
                else
                {
                    var firstSpaceIndex = filePathAndArgs.IndexOf(' ');
                    if (firstSpaceIndex == -1)
                    {
                        filePath = filePathAndArgs;
                        args = string.Empty;
                    }
                    else
                    {
                        filePath = filePathAndArgs.Substring(0, firstSpaceIndex).Trim();
                        args = filePathAndArgs.Substring(firstSpaceIndex).Trim();
                    }
                }

                filePath = $"\"{filePath}\"";

                Process process = new Process();
                process.StartInfo.FileName = filePath;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = false;

                process.Start();
                return $"Program '{filePath}' launched successfully with arguments '{args}'.";
            }
            catch (Exception ex)
            {
                return $"Error launching program '{filePathAndArgs}': {ex.Message}";
            }
        }));

        GlobalActions.Add(new TDAction("say", "Outputs the provided message. Usage: say \"<message>\"", message =>
        {
            return message;
        }));

        GlobalActions.Add(new TDAction("clear", "Clears the console window. Usage: clear", () =>
        {
            Console.Clear();
            return string.Empty;
        }));

        GlobalActions.Add(new TDAction("help", "Provides command info. Usage: help [commandName] \nExample: help call | help", commandName =>
        {
            if (string.IsNullOrEmpty(commandName))
            {
                List<string> commands = new List<string>();

                Console.WriteLine("\n");

                foreach (var comm in GlobalActions)
                {
                    commands.Add($"{comm.ActionName} => {comm.ActionDescription} \n\n");
                }

                return string.Join("", commands);
            }
            else
            {
                var foundAction = GlobalActions.Find(action => action.ActionName.Equals(commandName, StringComparison.OrdinalIgnoreCase));
                return foundAction?.ActionDescription ?? "No info!";
            }
        }));

        GlobalActions.Add(new TDAction("cmd", "Launches Command Prompt with args. Usage: cmd [args]", args =>
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {args}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = false;

                process.Start();
                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                return !string.IsNullOrEmpty(error) ? error : output;
            }
            catch (Exception ex)
            {
                return $"Error launching Command Prompt: {ex.Message}";
            }
        }));

        GlobalActions.Add(new TDAction("history", "Shows command history. Usage: history", () =>
        {
            return string.Join(Environment.NewLine, commandHistory);
        }));

        GlobalActions.Add(new TDAction("time", "Shows current date and time. Usage: time", () =>
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }));

        GlobalActions.Add(new TDAction("env", "Shows environment variables. Usage: env", () =>
        {
            var envVars = Environment.GetEnvironmentVariables();
            string output = string.Empty;
            foreach (DictionaryEntry de in envVars)
            {
                output += $"{de.Key}={de.Value}\n";
            }
            return output;
        }));

        GlobalActions.Add(new TDAction("chdir", "Changes the current directory. Usage: chdir \"<path>\"", directory =>
        {
            if (Directory.Exists(directory))
            {
                currentDirectory = directory;
                Directory.SetCurrentDirectory(currentDirectory);
                return $"Current directory changed to {currentDirectory}.";
            }
            else
            {
                return $"Directory '{directory}' does not exist.";
            }
        }));

        GlobalActions.Add(new TDAction("ccom", "Creates a new command. Usage: createCommand commandName \"commandDescription\" \"commandImplementation\" \n" +
            "Usage: createcommand hello \"Prints Hello World\" \"return \\\"Success\\\";\"", CreateCommand));

        GlobalActions.Add(new TDAction("rcom", "Removes a custom command. Usage: removeCommand commandName \nExample: removeCommand hello", args =>
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                return "Usage: removeCommand commandName";
            }

            var commandName = args.Trim();
            var result = RemoveCommand(commandName);
            return result ? $"Command '{commandName}' removed successfully." : $"Command '{commandName}' not found.";
        }));

        #region Aliases
        GlobalActions.Add(new TDAction("alias", "Manages aliases. Usage: alias [add|rename|remove|list] params", ManageAliases));

        string ManageAliases(string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                return "Usage: alias [add|rename|remove|list] params";
            }

            string[] parts = args.Split(new[] { ' ' }, 2);
            string subCommand = parts[0].ToLower();
            string subArgs = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            switch (subCommand)
            {
                case "add":
                    return AddAlias(subArgs);

                case "rename":
                    return RenameAlias(subArgs);

                case "remove":
                    return RemoveAlias(subArgs);

                case "list":
                    return ListAliases();

                default:
                    return "Invalid subcommand. Usage: alias [add|rename|remove|list] params";
            }
        }

        string AddAlias(string args)
        {
            string[] parts = args.Split(new[] { ' ' }, 2);
            if (parts.Length < 2)
            {
                return "Usage: alias add aliasName originalCommand [originalArgs]";
            }

            string alias = parts[0];
            string commandAndArgs = parts[1].Trim();

            string[] commandParts = commandAndArgs.Split(new[] { ' ' }, 2);
            string originalCommand = commandParts[0];
            string originalArgs = commandParts.Length > 1 ? commandParts[1] : string.Empty;

            if (GlobalActions.Exists(action => action.ActionName.Equals(originalCommand, StringComparison.OrdinalIgnoreCase)))
            {
                aliases[alias] = (originalCommand, originalArgs);
                SaveAliasToFile(alias, originalCommand, originalArgs);
                return $"Alias '{alias}' added for command '{originalCommand}' with args '{originalArgs}'.";
            }
            else
            {
                return $"Original command '{originalCommand}' does not exist.";
            }
        }

        string RenameAlias(string args)
        {
            string[] parts = args.Split(new[] { ' ' }, 2);
            if (parts.Length < 2)
            {
                return "Usage: alias rename oldAliasName newAliasName";
            }

            string oldAlias = parts[0];
            string newAlias = parts[1];

            if (aliases.ContainsKey(oldAlias))
            {
                var (command, aliasArgs) = aliases[oldAlias];
                aliases.Remove(oldAlias);
                aliases[newAlias] = (command, aliasArgs);
                SaveAllAliasesToFile();
                return $"Alias '{oldAlias}' renamed to '{newAlias}'.";
            }
            return $"Alias '{oldAlias}' not found.";
        }

        string RemoveAlias(string args)
        {
            if (aliases.ContainsKey(args))
            {
                aliases.Remove(args);
                SaveAllAliasesToFile();
                return $"Alias '{args}' removed.";
            }
            return $"Alias '{args}' not found.";
        }

        string ListAliases()
        {
            if (aliases.Count == 0)
            {
                return "No aliases found.";
            }

            string output = "Saved aliases:\n";
            foreach (var alias in aliases)
            {
                output += $"{alias.Key} -> {alias.Value.command} {alias.Value.args}\n";
            }

            return output;
        }

        void SaveAllAliasesToFile()
        {
            using (StreamWriter writer = new StreamWriter(AliasFilePath))
            {
                foreach (var alias in aliases)
                {
                    writer.WriteLine($"{alias.Key},{alias.Value.command},{alias.Value.args}");
                }
            }
        }
        #endregion
    }

    #region Custom Commands
    public static bool RemoveCommand(string commandName)
    {
        var actionToRemove = GlobalActions.Find(action => action.ActionName.Equals(commandName, StringComparison.OrdinalIgnoreCase));

        if (actionToRemove == null)
        {
            return false;
        }

        GlobalActions.Remove(actionToRemove);
        RemoveCommandFromFile(commandName);

        return true;
    }

    public static void RemoveCommandFromFile(string commandName)
    {
        if (File.Exists(CommandFilePath))
        {
            var lines = File.ReadAllLines(CommandFilePath).ToList();
            var newLines = lines.Where(line => !line.StartsWith(commandName + " ")).ToList();
            File.WriteAllLines(CommandFilePath, newLines);
        }
    }

    public static string CreateCommand(string args)
    {
        string[] parts = args.Split(new[] { ' ' }, 2);
        if (parts.Length < 2)
        {
            return "Usage: createCommand commandName \"commandDescription\" \"commandImplementation\"";
        }

        string commandName = parts[0].Trim();
        string[] descAndImpl = parts[1].Split(new[] { '\"' }, StringSplitOptions.RemoveEmptyEntries);

        if (descAndImpl.Length < 2)
        {
            return "Usage: createCommand commandName \"commandDescription\" \"commandImplementation\"";
        }

        string commandDescription = descAndImpl[0].Trim();
        string commandImplementation = descAndImpl[1].Trim();

        var result = CompileCommand(commandName, commandDescription, commandImplementation);
        return result ? $"Command '{commandName}' created successfully." : $"Failed to create command '{commandName}'.";
    }

    public static bool CompileCommand(string commandName, string commandDescription, string commandImplementation)
    {
        string code = $@"
                        using System;
                        using System.Collections.Generic;
                        public class DynamicCommand
                        {{
                            public static string Execute(string args)
                            {{
                                try
                                {{
                                    {commandImplementation}
                                    Console.WriteLine(""Executed successfully"");
                                    return ""Success"";
                                }}
                                catch (Exception ex)
                                {{
                                    Console.WriteLine(""Error: "" + ex.Message);
                                    return ""Execution failed"";
                                }}
                            }}
                        }}";

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

        var referencedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .ToList();

        referencedAssemblies.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        referencedAssemblies.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
        referencedAssemblies.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));

        CSharpCompilation compilation = CSharpCompilation.Create(
            $"Dynamic{commandName}",
            new[] { syntaxTree },
            referencedAssemblies,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var ms = new MemoryStream())
        {
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Console.Error.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }

                return false;
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());
                Type type = assembly.GetType("DynamicCommand");
                MethodInfo method = type.GetMethod("Execute");

                GlobalActions.Add(new TDAction(commandName, commandDescription, (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), method)));

                SaveCommandToFile(commandName, commandDescription, commandImplementation);

                return true;
            }
        }
    }

    public static void SaveCommandToFile(string commandName, string commandDescription, string commandImplementation)
    {
        using (StreamWriter writer = new StreamWriter(CommandFilePath, true))
        {
            writer.WriteLine($"{commandName} \"{commandDescription}\" \"{commandImplementation}\"");
        }
    }

    public static void LoadCommandsFromFile()
    {
        if (File.Exists(CommandFilePath))
        {
            var lines = File.ReadAllLines(CommandFilePath);
            foreach (var line in lines)
            {
                string[] parts = line.Split(new[] { '\"' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    string commandName = parts[0].Trim();
                    string commandDescription = parts[1].Trim();
                    string commandImplementation = parts[2].Trim();

                    if (GlobalActions.Any(action => action.ActionName.Equals(commandName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    CompileCommand(commandName, commandDescription, commandImplementation);
                }
            }
        }
    }
    #endregion

    #region Alias methods
    public static void LoadAliasesFromFile()
    {
        if (File.Exists(AliasFilePath))
        {
            var lines = File.ReadAllLines(AliasFilePath);
            foreach (var line in lines)
            {
                // format: alias,command,args
                var parts = line.Split(new[] { ',' }, 3);
                if (parts.Length >= 2)
                {
                    string alias = parts[0];
                    string command = parts[1];
                    string args = parts.Length > 2 ? parts[2] : string.Empty;
                    aliases[alias] = (command, args);
                }
            }
        }
    }

    public static void SaveAliasToFile(string alias, string command, string args)
    {
        using (StreamWriter writer = new StreamWriter(AliasFilePath, true))
        {
            writer.WriteLine($"{alias},{command},{args}");
        }
    }
    #endregion
}