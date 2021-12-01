GerberView Project

05/01/2019
Revision 1.0.0.0 [Initial Release]

17/03/2019
GerberView
Revision 1.0.0.1
Added support for Excellon drill file statistics.
Added support for object selection and display object selection properties.

LibGerberVS Version 1.0.0.1
Updates to GerberVS library to support object selection.

01/06/2019
Revsion 1.2.2.0
Added support for Printing/Print preview
Added support for saving and loading Projects.
Project files are xml format with .gpf extension.
Added View menu options.
Added Layer menu options.
 
LibGerberVS Version 1.0.1.1
Added support for layer (file) reloading.
Some refactoring with "user selection code"
Added FileCount member to the Project class.
Moved the common GerberUserTransform class from the GerberProject class to the GerberFileInformation class.
Each GerberFileInformation now contains its own GerberUserTransform class.
Not sure why you would want to have individual transformations for each layer within the same project!!
But for now aligns with GerbV.

30/06/2019
Revision 1.3.0.0
First Beta release.

11/12/2019
LibGerberVS Version 1.0.3.2
Gerber.cs
	Fix issues with bounding box calculations for flashed and macro apertures types.

GerberImage.cs
	Added method ImageCopy for creating a deep copy of the RS274X GerberImage.

ExportRS274X.cs
	Added class for exporting to, and saving a GerberImage to RS274X format.

GerberView Version 1.3.1.0
	Misc bug fixes.
	Added features.

05/01/2021
LibGerberVS Version 1.0.5.0 
Gerber.cs	
	Fix issue with calculating final image size when using nested macros apertures
	Update circular (arc) boundingbox code.
LibGerberVS.cs
	Some improved point type hit testing.

GerberView Version 1.3.2.0
	Some minor updates to the GUI

15/01/2021
LibGerberVS Version 1.0.5.1
	Bug fix point type hit testing.

GerberView Version 1.3.2.1
	Code audits for GUI.

06/04/2021
LibGerberVS Version 1.0.5.2
	In file Gerber.cs, fix issue with processing Format Statement (FS command)

GerberView Version 1.3.3.0
	GUI updates.
	Bug fix Drag n Drop on to FileListBox;

14/08/2021
GerberView 1.3.4.0
	GUI updates.
	Form usercontrol updates:
	1. Ruler Control
	2. FileList Control.
	3. Color Dialog with transparecy support.

27/11/2021
Migrate project to VS2017 Net 4.6.1
GerberView 1.3.5.0
LibGerberVS 1.2.0.0
	GUI updates.
	Code audits and refactoring.
	Some bug fixes.
