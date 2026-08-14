using System.Diagnostics;
using AspiraHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers
{
    // This controller exists mainly so Program.cs's production error handler
    // (app.UseExceptionHandler("/Home/Error")) has somewhere real to land.
    // Without it, any unhandled exception in production would itself 404,
    // hiding the original error from both the user and the logs.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // The app's real landing page is AuthController.Index() (see the
        // default route in Program.cs). This is just a safety-net redirect
        // in case something links straight to "/Home".
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Auth");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            if (feature?.Error != null)
            {
                _logger.LogError(feature.Error,
                    "Unhandled exception on {Path} (RequestId: {RequestId})",
                    feature.Path, requestId);
            }

            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
