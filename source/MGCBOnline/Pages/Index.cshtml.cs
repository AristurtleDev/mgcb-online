using System.Diagnostics;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualBasic;

namespace MGCBOnline.Pages;

public class IndexModel : PageModel
{
    private IWebHostEnvironment _environment;

    [BindProperty]
    public List<IFormFile> Uploads { get; set; } = new List<IFormFile>();

    public IndexModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public void OnGet()
    {
    }
}
