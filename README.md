# TinyPngApi
Extremely basic, inefficient and unstructured blob of hacked together C# code, as a C# Console app to compress images using the TinyPNG.com API.

# TinyPNG
You can get a free API key here: https://tinypng.com/developers

It's free up to 500 images, there-after you pay per image - pricing is on the above link/page.

# Parameters
* -target			=	Directory with png/jpg files ("current" can be entered for the current directory of the .exe file)
* -key			=	The TinyPNG.com API key to use
* -recursive		=	If present, all subdirectories will be searched for png and jpg files (Default = true)

The target and TinyPNG Api Key arguments will prompt for input if they are not present.  

Another option for the API key, is to place it in a "TinyPngApi.key" file in the same folder as the .exe

Although basic, can be really useful if scheduled with Task Scheduler to regularely compress those big and nasty images clients uploads using CMS systems that doesn't optimize them!

Have fun!