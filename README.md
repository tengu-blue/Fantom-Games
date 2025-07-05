# Project information

This project contains multiple C# libraries as well as an Avalonia GUI application all concerning the board game Scotland Yard.

# Project structure

```
FantomGamesCore/
FantomGameSystem/
html/
publish_linux64/
publish_win64/
```

# `FantomGamesCore`
This folder contains a standalone C# library called the Games Core. It can be used to implement the aforementioned board game as well as other versions of it using some settings.

# `FantomGameSystem`
This one contains multiple projects which together form the Avalonia GUI based application running the standard Scotland Yard built using the the Games Core.

It also adds computer opponents to the game.

# `html`
Is a Doxygen generated documentation for the Game Core describing the public interface. Read it by opening the `index.html` file via a web-browser of your choice.

# `publish_linux64` and `publish_win64`
Finally these folders contain the actual runnable application. They were built using the .NET publish feature. Both are self-contained single-file builds for Linux and Windows respectively. 

As such it they should run on the target platform without any other user actions necessary.

The runnable files are `AvaloniaFantomGamesFacade.Desktop` and `AvaloniaFantomGamesFacade.Desktop.exe` respectively.

