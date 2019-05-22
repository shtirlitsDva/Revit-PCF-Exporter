**PCF Exporter** is an add-in for [Autodesk Revit](http://www.autodesk.com/products/revit-family/overview) aimed to facilitate the export of piping data from Revit to the widely used PCF (Piping Component File) file format.

To get the most out of the program (in truth it is useless without knowledge of the format specification) it is strongly recommended to read the PCF reference guide which contains the format specifics. Please see [this](http://www.intergraph.com/assets/pressreleases/2015/05-12-2015.aspx) web page to obtain the specification of the PCF file format.

## Installation

At this point the only way to get a working copy is to open the solution in Visual Studio and compile the .dll. Use the latest branch. Reference RevitAPI.dll and RevitAPIUI.dll from the Revit installation directory. Installation of certain NuGet packages is also required for compilation to succeed. Compile and then use the standard Autodesk Revit procedure of installing an add-in. That is copy the compiled .dll and the [PCF-Exporter.addin](https://github.com/shtirlitsDva/pcf-exporter/blob/master/PCF_Exporter.addin) file for current user to: C:\Users\\[USERNAME]\AppData\Roaming\Autodesk\Revit\Addins\\[REVIT VERSION]\ or for all users to: C:\ProgramData\Autodesk\Revit\Addins\\[REVIT VERSION]\\.

## Dependencies



## Documentation

See the [Wiki](https://github.com/shtirlitsDva/pcf-exporter/wiki) for documentation.

## Communication

- Open an issue in the [issue tracker](https://github.com/shtirlitsDva/pcf-exporter/issues) to get in touch with the project.
- Send me an email: app at mgtek dot dk.
 
## Disclaimer

- Beware, this application was made by a Civil Engineer and not a programmer. Use at your own risk.
- The application has not been tested on an Imperial project.

\#Revit \#PCF
