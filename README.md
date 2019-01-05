# ConsoleArgumentParser

A library to create simple, class based commands to control your console application without writing gigantic switch statements.

Here is an example of a super simple command:
```cs
[Command("-add", "Adds two integers")]
public class ExampleCommand : ICommand
{
    private int _num1;
    private int _num2;

    public ExampleCommand(int num1, int num2)
    {
        _num1 = num1;
        _num2 = num2;
    }

    public void Execute()
    {
        Console.WriteLine(_num1 + _num2);
    }
}
```

The parser just need to get created with the symbols you wish to indicate a command and all commands that should be available to this parser need to be added.
```cs
public static Parser Parser { get; private set; }
public static void Main(string[] args)
{
    Parser = new Parser("-", "--");
    Parser.RegisterCommand(typeof(ExampleCommand));
    Parser.ParseCommands(args);
}
```

Any arguments given right after the commandname will be passed to the constructor. Finally the Execute() method will be called.

The library tries to parse any input to the specified type given in the command. If further type parsing is needed, you can add custom typeparsers using the `AddCustomTypeParser` method.

#### Subcommands

Additionally to a command followed by simple arguments you can also add Subcommands. You might be familiar with that idea if you ever used git via the command line.

Lets look at another example command:
```cs
[Command("-print", "Prints a string")]
public class AnotherExampleCommand : ICommand
{
    private string _s;
    private bool _red;

    public AnotherExampleCommand(string s)
    {
        _s = s;
    }

    [CommandArgument("--red")]
    private void Red(bool toggle)
    {
        _red = toggle;
    }

    public void Execute()
    {
        if (_red) Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(_s);
    }
}
```

The `--red` Subcommand is optional, but can be applied to color the output red.

#### Helptext

The library also offers the ability to generate a simple help text for all registered commands simply by calling `GetHelpString()`.
The parser with the two example commands would generate the following string:
```
-print s [--red toggle]
        Prints a string
-add num1 num2
        Adds two integers
```
