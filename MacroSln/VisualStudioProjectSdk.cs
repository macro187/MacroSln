namespace
MacroSln
{


public class
VisualStudioProjectSdk
{


public static readonly VisualStudioProjectSdk
DotNetCore = new VisualStudioProjectSdk("Microsoft.NET.Sdk");


public static readonly VisualStudioProjectSdk
DotNetCoreWeb = new VisualStudioProjectSdk("Microsoft.NET.Sdk.Web");


public static readonly VisualStudioProjectSdk
DotNetCoreRazor = new VisualStudioProjectSdk("Microsoft.NET.Sdk.Razor");


public static readonly VisualStudioProjectSdk
DotNetCoreWorker = new VisualStudioProjectSdk("Microsoft.NET.Sdk.Worker");


public static readonly VisualStudioProjectSdk
DotNetCoreWindowsDesktop = new VisualStudioProjectSdk("Microsoft.NET.Sdk.WindowsDesktop");


private
VisualStudioProjectSdk(string id)
{
    Id = id;
}


public string
Id { get; }


}
}
