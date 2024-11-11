﻿using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Faluf.Portfolio.Blazor.Controllers;

[Route("[controller]/[action]")]
public sealed class CultureController : Controller
{
    public IActionResult Set(string culture, string redirectUri)
    {
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)));

        return LocalRedirect(redirectUri);
    }
}