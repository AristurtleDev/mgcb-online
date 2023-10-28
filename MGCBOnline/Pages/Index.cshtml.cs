using System.Diagnostics;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic;

namespace MGCBOnline.Pages;

[DisableRequestSizeLimit]
public class IndexModel : PageModel
{
    private IWebHostEnvironment _environment;

    [BindProperty]
    public List<IFormFile> Uploads { get; set; } = new List<IFormFile>();

    public IndexModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }


    public async Task<ActionResult> OnPostAsync()
    {
        //  Create unique id
        Guid id = Guid.NewGuid();

        //  Create a directory to upload the files too based on the GUID created
        string contentDir = Path.Combine(_environment.ContentRootPath, "uploads", id.ToString());
        Directory.CreateDirectory(contentDir);

        //  Go through each file and add them to the directory. 
        //  Ignore any files in the /bin and /obj directory
        foreach (var file in Uploads)
        {
            var filePath = Path.Combine(contentDir, file.FileName);

            if (filePath.Contains("Content\\bin") || filePath.Contains("Content\\obj") ||
                filePath.Contains("Content/bin") || filePath.Contains("Content/obj"))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            catch { }

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
        }

        //  Run dotnet new tool-manifest
        ProcessStartInfo pToolManifestInfo = new ProcessStartInfo();
        pToolManifestInfo.WorkingDirectory = contentDir;
        pToolManifestInfo.FileName = "dotnet";
        pToolManifestInfo.Arguments = "new tool-manifest";
        Process? pToolManifest = Process.Start(pToolManifestInfo);
        await pToolManifest?.WaitForExitAsync();

        //  Run dotnet tool install dotnet-mgcb
        ProcessStartInfo pToolInstallInfo = new ProcessStartInfo();
        pToolInstallInfo.WorkingDirectory = contentDir;
        pToolInstallInfo.FileName = "dotnet";
        pToolInstallInfo.Arguments = "tool install dotnet-mgcb";
        Process? pToolInstal = Process.Start(pToolInstallInfo);
        await pToolInstal?.WaitForExitAsync();

        //  run dotnet mgcb ./Content/Content.mgcb
        ProcessStartInfo pContentBuildInfo = new ProcessStartInfo();
        pContentBuildInfo.WorkingDirectory = contentDir;
        pContentBuildInfo.FileName = "dotnet";
        pContentBuildInfo.Arguments = "mgcb ./Content/Content.mgcb";
        Process? pContentBuild = Process.Start(pContentBuildInfo);
        await pContentBuild?.WaitForExitAsync();

        //  Buidl paths
        string contentPath = Path.Combine(contentDir, "Content");
        string outputPath = Path.Combine(contentPath, "bin");
        string intermeddiatePath = Path.Combine(contentPath, "obj");
        string resultPath = Path.Combine(contentDir, "BuildResult");
        string zipFilePath = Path.Combine(contentDir, "Content.zip");

        //  Move /bin and /obj into a common directory
        Directory.CreateDirectory(resultPath);
        Directory.Move(outputPath, Path.Combine(resultPath, "bin"));
        Directory.Move(intermeddiatePath, Path.Combine(resultPath, "obj"));

        //  Create the zip file
        ZipFile.CreateFromDirectory(resultPath, zipFilePath);

        //  Read all bytes from the zip file
        byte[] zipBytes = System.IO.File.ReadAllBytes(zipFilePath);

        //  Delete the files the user uploaded
        Directory.Delete(contentDir);

        //  Return the zip file
        return File(zipBytes, "application/zip", "Content.zip");
    }

    public void OnGet(string? id)
    {
    }
}
