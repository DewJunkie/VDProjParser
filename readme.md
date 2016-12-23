I got tired of working with visual studio setup projects, but not so tired of them that i wanted to move over to Installshield Lite.
In my experience, they are much easier to get a working install.  I have only done enough testing on this to get it to be useful for
me.

    Usage: [OPTIONS] projectfile.vdproj
    Options
      -b, --browse               Browse for project file to open
      -o, --output=VALUE         Write output to file
          --stdOut               Write output to console
          --stdIn                Read input from StdIn
          --xmlIn                Read file in as XML
          --xmlOut               Write output as xml
      -r, --remove=VALUE         Remove file from setup project if it's SourcePath
                                   contains this string
      -q=VALUE                   Return the value the XPath query
          --increment            Increment the build version
      -?, -h, --help             Show help

## Some examples   ##
* Update the version number of a project. And write the new version to stdOut  
    ProjectParser.exe -q=//ProductVersion --increment -o=c:\temp\Setup.vdproj c:\temp\Setup.vdproj
* Same thing, but reading from standard input.  
    type c:\temp\Setup.vdproj | ProjectParser.exe --stdIn -q=//ProductVersion --increment -o=c:\temp\Setup.vdproj
* Convert a vdproj to xml and then back to vdproj format.  
    ProjectParser.exe c:\temp\Setup.vdproj --stdOut --xmlOut | ProjectParser.exe --stdIn --xmlIn --stdOut
