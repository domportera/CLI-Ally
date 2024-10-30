# CLI Ally
[![NuGet](https://img.shields.io/nuget/v/domportera.CLIAlly.svg)](https://www.nuget.org/packages/domportera.CLIAlly/)

An easy-to-use, AOT-friendly library for creating flexible CLI applications for use in the terminal and shell scripts with automatic help-file generation and JSON argument support.

This library makes very few assumptions about how you will use it and dictates very little about the structure of your application. For a more in-depth library for creating nice console-focused applications, you may want to check out [Spectre](https://github.com/spectreconsole/spectre.console).

It is *very* early in development, and I am dog-fooding it from this point forward. However, it seems to work plenty well at the moment! Please report any issues, bugs, or feature requests to the [issue tracker](https://github.com/domportera/CLI-Ally/issues). I can't recommend it for production use at the moment.

I made this library because re-inventing wheels is an awesome way to spend my time. /jokes
In reality, most other CLI libraries I came across (including the upcoming Microsoft .NET library) seemed either too big and complex, were no longer maintained, or lacked quality of life features I'd expect from a CLI library.

I intend to keep this library relatively small, but from my experience developing this library, some advanced features can be added very cheaply. Ultimately, the goal is to make CLI applications as frictionless as possible.

## Features
- Automatic help text generation that's rendered responsively in the terminal with help text
- Automatic JSON argument support - your arguments can be specified in a json file (or as a json string in the command line if you're feeling a little silly) and the arguments will be parsed automatically*
  - `MyApp.exe -j './my-rgs.json'
- `[JsonText]` arguments for complex data structures that support both file input and inline json 
  - ex: `[JsonText] public Person PersonDetails;`
- AOT-friendly with `System.Text.Json` (see the Example project and [this repo](https://github.com/domportera/NetJsonAOT) for more details)
- `[Required]` arguments
- `[Range]` arguments for numeric values
- `[Description]` and `[VerboseDescription]` attributes
- Case-sensitivity options for both long and short names of arguments
- Automatic argument naming for both full and short names (e.g. `int MyArgument` becomes `--MyArgument` and `--m`)
- Explicit naming for arguments
- `[DefaultValue(value)]` support
- Multi-short-code syntax support (e.g. `git clean -fxd`)
- Multiple commands can be specified in sequence with unique arguments
- Provided `--version` implementation
- Ordered argument support
- Default command support (no command name provided? no problem! unless there is a problem! up to you really)
- `[Path]` attribute support for path string argument validation
- Arbitrary return type support/printing via ToString() methods
- ExitCode return type support with both numbers and messages
- `Printer` helper class for `Console.Write` methods that strips ANSI escape codes from the output if the output is not a terminal - for the sake of fearless inter-process communication and stylish output for those running in the terminal
- A fun little StringBuilder-based interface and extension methods for more efficient string building (unrelated but I made it public because it seems cool - is subject to change or deletion if I later realize that it's actually stupid)
- Probably other things too I'm forgetting and will document once I have the time and willpower to create proper documentation for this


*Note: this is a somewhat under-developed feature and lacks some of the robust validation that non-json arguments receive in response to your attributes. It's still incredibly handy for quickly running tests, presets, or whatever you can think up. Normal `System.Text.Json` deserialization rules apply

## Usage

To-do, but see the [example project](https://github.com/domportera/CLI-Ally/tree/main/Example) for an example showcasing some of its features. Feel free to clone the project, make a build, and play with some of the presets included!

Using the example and exploring its features should tell you quite a lot about how it works, even without documentation, as there is very little going on there.

Please let me know if the example works cross-platform! So far, it is only tested on Windows.

## Roadmap
- Configurable machine-readable output formats for script-based execution
- Any CLI standards that I am currently unaware of and are worth supporting (feature requests welcome!)
- ...

### Nice-to-have
This I'm not quite sold on, but they sound nice:
- Command arguments defined by method signature
  - (-) This would make json support difficult - could be really nice to have without it though
  - (-) Could make defining arguments cumbersome if you want them to be validated via attributes
  - (+) Dead simple to define arguments
  - (+) Could make parameter types simpler to re-use
  - (-) Benefits to ease-of-use could be outweighed by the validation required by the command implementation
  - (-) Could turn command method signatures into a nasty mess of attributes and arguments
  - (+) A big step towards making this library "invisible" in a "just works" kind of way
- Process chaining support
  - I'm certain there's a way to make chaining processes together easier, but I don't know significant gains on this front would require the process implements this library. This kind of plays into the machine-readable output formats on the roadmap above
- ...
