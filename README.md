# TD Console

TD Console - terminal for executing commands and automating tasks.

## Description
The TD Console is a powerful and extensible terminal utility designed to streamline your workflow. With TD Console, you can execute a variety of commands, manage aliases, create and remove custom commands, and interact with your system environment. The console is easily extensible, allowing you to add new commands dynamically.

## Main Features
1. **Execute system commands and custom scripts.**
2. **Manage command aliases for quicker access.**
3. **Create and remove custom commands directly from the console.**

## Usage Examples
### Launch a program
```sh
call "C:\Path\To\YourProgram.exe" [args]

Add a custom command
ccom hello "Prints Hello World" "Console.WriteLine(\"Hello World\"); return \"Success\";"

Remove a custom command
rcom hello

Add an alias
alias add h hello

View aliased commands
alias list

Show environment variables
env

Commands
For a list of all commands and their descriptions, type help.

Example of adding and using an alias

1. Create an alias:
alias add hw hello

2. Execute the alias:
hw
```
License
This project is licensed under the MIT License.
