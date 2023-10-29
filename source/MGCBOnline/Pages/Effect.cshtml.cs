using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic;

namespace MGCBOnline.Pages;

[RequestSizeLimit(1000000)]
public class EffectPageModel : PageModel
{
    public class Input
    {

        [DisplayName("Effect File")]
        public IFormFile? File { get; set; } = null;

        [Required]
        public Platform Platform { get; set; } = Platform.DesktopGL;

        [Required]
        [DisplayName("Graphics Profile")]
        public GraphicsProfile GraphicsProfile { get; set; } = GraphicsProfile.Reach;

        [Required]
        [DisplayName("Debug Mode")]
        public DebugMode DebugMode { get; set; } = DebugMode.Auto;

        public string? Defines { get; set; } = string.Empty;
    }

    private readonly IWebHostEnvironment _environment;

    [BindProperty]
    public Input Form { get; set; } = new Input();

    public List<string> Output { get; set; } = new List<string>();
    public bool Failed { get; set; } = false;

    public EffectPageModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }


    public async Task<ActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Form.File is null) { return Page(); }

        //  Create unique id
        Guid id = Guid.NewGuid();

        //  Create a directory to upload the files too based on the GUID created
        string contentDir = Path.Combine(_environment.ContentRootPath, "uploads", id.ToString());
        Directory.CreateDirectory(contentDir);


        string filePath = Path.Combine(contentDir, Path.GetFileName(Form.File.FileName));

        //  Copy the file to the temporary directory
        using (FileStream fileStream = System.IO.File.Create(filePath))
        {
            await Form.File.CopyToAsync(fileStream);
        }

        ProcessStartInfo toolManifestInfo = new ProcessStartInfo();
        toolManifestInfo.WorkingDirectory = contentDir;
        toolManifestInfo.FileName = "dotnet";
        toolManifestInfo.Arguments = "new tool-manifest";
        toolManifestInfo.UseShellExecute = false;
        toolManifestInfo.RedirectStandardOutput = true;
        toolManifestInfo.RedirectStandardError = true;
        toolManifestInfo.CreateNoWindow = true;
        Process? toolManifestProcess = Process.Start(toolManifestInfo);
        toolManifestProcess?.WaitForExit();


        ProcessStartInfo toolInstallInfo = new ProcessStartInfo();
        toolInstallInfo.WorkingDirectory = contentDir;
        toolInstallInfo.FileName = "dotnet";
        toolInstallInfo.Arguments = "tool install dotnet-mgcb";
        toolInstallInfo.UseShellExecute = false;
        toolInstallInfo.RedirectStandardOutput = true;
        toolInstallInfo.RedirectStandardError = true;
        toolInstallInfo.CreateNoWindow = true;
        Process? toolInstallProcess = Process.Start(toolInstallInfo);
        toolInstallProcess?.WaitForExit();


        string fileName = Path.GetFileName(filePath);

        ProcessStartInfo mgcbInfo = new ProcessStartInfo();
        mgcbInfo.WorkingDirectory = contentDir;
        mgcbInfo.FileName = "dotnet";
        mgcbInfo.RedirectStandardError = true;
        mgcbInfo.RedirectStandardOutput = true;
        mgcbInfo.ArgumentList.Add("mgcb");
        mgcbInfo.ArgumentList.Add($"/platform:{Form.Platform}");
        mgcbInfo.ArgumentList.Add($"/profile:{Form.GraphicsProfile}");
        mgcbInfo.ArgumentList.Add($"/importer:EffectImporter");
        mgcbInfo.ArgumentList.Add($"/processor:EffectProcessor");
        mgcbInfo.ArgumentList.Add($"/processorParam:DebugMode={Form.DebugMode}");
        if (!string.IsNullOrEmpty(Form.Defines))
        {
            mgcbInfo.ArgumentList.Add($"/processorParam:Defines={Form.Defines}");
        }
        mgcbInfo.ArgumentList.Add($"/build:{fileName};{fileName}");
        Process? mgcbProcess = Process.Start(mgcbInfo);
        mgcbProcess?.WaitForExit();


        while (mgcbProcess?.StandardOutput.EndOfStream == false)
        {
            string line = mgcbProcess?.StandardOutput.ReadLine() ?? string.Empty;
            if (line.Contains("1 failed"))
            {
                Failed = true;
            }
            Output.Add(line);
        }

        if (Failed)
        {
            Directory.Delete(contentDir, true);
            return Page();
        }

        string xnbName = Path.GetFileNameWithoutExtension(fileName) + ".xnb";


        byte[] bytes = System.IO.File.ReadAllBytes(Path.Combine(contentDir, xnbName));
        Directory.Delete(contentDir, true);
        return File(bytes, "application/octet-stream", xnbName);
    }

    public void OnGet()
    {
    }
}
