using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic;

namespace MGCBOnline.Pages;

[RequestSizeLimit(1000000)]
public class OpenAssetImportLibraryPageModel : PageModel
{
    public class Input
    {

        [DisplayName("Open Asset Import Library File")]
        public IFormFile? File { get; set; } = null;

        [Required]
        public Platform Platform { get; set; } = Platform.DesktopGL;

        [Required]
        [DisplayName("Graphics Profile")]
        public GraphicsProfile GraphicsProfile { get; set; } = GraphicsProfile.Reach;

        [DisplayName("Color Key Color")]
        public Color ColorKeyColor { get; set; } = Color.FromArgb(0, 0, 0, 0);

        [DisplayName("Color Key Enabled")]
        public bool ColorKeyEnabled { get; set; } = false;

        [DisplayName("Default Effect")]
        public DefaultEffect DefaultEffect { get; set; } = DefaultEffect.BasicEffect;

        [DisplayName("Generate Mipmaps")]
        public bool GenerateMipMaps { get; set; } = true;

        [DisplayName("Generate Tangent Frames")]
        public bool GenerateTangentFrames { get; set; } = false;

        [DisplayName("Premultiply Texture Alpha")]
        public bool PremultiplyTextureAlpha { get; set; } = true;

        [DisplayName("Premultiply Vertex Colors")]
        public bool PremultiplyVertextColors { get; set; } = true;

        [DisplayName("Resize Textures to Power of Two")]
        public bool ResizeTextureToPowerOfTwo { get; set; } = false;

        [DisplayName("Rotation X")]
        public float RotationX { get; set; } = 0.0f;

        [DisplayName("Rotation Y")]
        public float RotationY { get; set; } = 0.0f;

        [DisplayName("Rotation Z")]
        public float RotationZ { get; set; } = 0.0f;

        [DisplayName("Scale")]
        public float Scale { get; set; } = 1.0f;

        [DisplayName("Swap Winding Order")]
        public bool SwapWindingOrder { get; set; } = false;

        [DisplayName("Texture Format")]
        public TextureFormat TextureFormat { get; set; } = TextureFormat.Compressed;
    }

    private readonly IWebHostEnvironment _environment;

    [BindProperty]
    public Input Form { get; set; } = new Input();

    public List<string> Output { get; set; } = new List<string>();
    public bool Failed { get; set; } = false;

    public OpenAssetImportLibraryPageModel(IWebHostEnvironment environment)
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
        mgcbInfo.ArgumentList.Add($"/importer:OpenAssetImporter");
        mgcbInfo.ArgumentList.Add($"/processor:ModelProcessor");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.ColorKeyColor)}={Form.ColorKeyColor.R},{Form.ColorKeyColor.G},{Form.ColorKeyColor.A}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.ColorKeyEnabled)}={Form.ColorKeyEnabled}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.DefaultEffect)}={Form.DefaultEffect}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.GenerateMipMaps)}={Form.GenerateMipMaps}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.GenerateTangentFrames)}={Form.GenerateTangentFrames}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.PremultiplyTextureAlpha)}={Form.PremultiplyTextureAlpha}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.PremultiplyVertextColors)}={Form.PremultiplyVertextColors}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.ResizeTextureToPowerOfTwo)}={Form.ResizeTextureToPowerOfTwo}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.RotationX)}={Form.RotationX}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.RotationY)}={Form.RotationY}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.RotationZ)}={Form.RotationZ}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.RotationX)}={Form.Scale}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.SwapWindingOrder)}={Form.SwapWindingOrder}");
        mgcbInfo.ArgumentList.Add($"/processorParam:{nameof(Input.TextureFormat)}={Form.TextureFormat}");
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
