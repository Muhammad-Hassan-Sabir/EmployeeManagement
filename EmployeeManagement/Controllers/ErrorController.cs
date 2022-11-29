using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            this.logger = logger;
        }
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var resultStatusCode = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Sorry, the resource you requested could not be found";
                    logger.LogError($"404 Error  originalPath={resultStatusCode.OriginalPath}  OriginalQueryString={resultStatusCode.OriginalQueryString} ");
                    break;
            }

            return View("NotFound");
        }
        [Route("Error")]
        [AllowAnonymous]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
           
            logger.LogError($@"500 Error  ExceptionPath={exceptionHandlerPathFeature.Path} 
                                          ExceptionMessage={exceptionHandlerPathFeature.Error.Message}
                                          StackTrace={exceptionHandlerPathFeature.Error.StackTrace}");

            return View("Error");
        }
    }
}
