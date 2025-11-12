using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Vowlt.Api.Features.Auth.Models;

namespace Vowlt.Api.Features.OAuth;

/// <summary>
/// OAuth login page for browser-based OAuth flow.
/// Displays HTML login form when user is not authenticated.
/// </summary>
[ApiController]
[Route("oauth")]
public class OAuthLoginPageController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<OAuthLoginPageController> logger) : ControllerBase
{
    /// <summary>
    /// GET /oauth/login - Shows HTML login form
    /// </summary>
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult ShowLoginPage([FromQuery] string? returnUrl)
    {
        var html = $$"""
          <!DOCTYPE html>
          <html>
          <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Login - Vowlt</title>
              <style>
                  * { margin: 0; padding: 0; box-sizing: border-box; }
                  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif; background: #f5f5f5; display: flex; align-items: center; justify-content: center; min-height:100vh; }
                  .container { background: white; padding: 2rem; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); width: 100%; max-width: 400px; }
                  h1 { font-size: 1.5rem; margin-bottom: 0.5rem; color: #333; }
                  p { color: #666; margin-bottom: 1.5rem; font-size: 0.875rem; }
                  label { display: block; margin-bottom: 0.5rem; font-weight: 500; color: #333; font-size: 0.875rem; }
                  input { width: 100%; padding: 0.75rem; border: 1px solid #ddd; border-radius: 4px; font-size: 1rem; margin-bottom: 1rem; }
                  input:focus { outline: none; border-color: #2563eb; }
                  button { width: 100%; padding: 0.75rem; background: #2563eb; color: white; border: none; border-radius: 4px; font-size: 1rem; font-weight: 500; cursor: pointer; }
                  button:hover { background: #1d4ed8; }
                  button:disabled { background: #9ca3af; cursor: not-allowed; }
                  .error { background: #fee; border: 1px solid #fcc; color: #c33; padding: 0.75rem; border-radius: 4px; margin-bottom: 1rem; font-size: 0.875rem; display: none; }
                  .error.show { display: block; }
              </style>
          </head>
          <body>
              <div class="container">
                  <h1>Sign in to Vowlt</h1>
                  <p>Sign in to authorize the application</p>

                  <div id="error" class="error"></div>

                  <form id="loginForm">
                      <input type="hidden" name="returnUrl" value="{{returnUrl ?? ""}}">

                      <label for="email">Email</label>
                      <input type="email" id="email" name="email" required autocomplete="email" autofocus>

                      <label for="password">Password</label>
                      <input type="password" id="password" name="password" required autocomplete="current-password">

                      <button type="submit" id="submitBtn">Sign In</button>
                  </form>
              </div>

              <script>
                  const form = document.getElementById('loginForm');
                  const errorDiv = document.getElementById('error');
                  const submitBtn = document.getElementById('submitBtn');

                  form.addEventListener('submit', async (e) => {
                      e.preventDefault();

                      errorDiv.classList.remove('show');
                      submitBtn.disabled = true;
                      submitBtn.textContent = 'Signing in...';

                      const formData = new FormData(form);
                      const data = {
                          email: formData.get('email'),
                          password: formData.get('password')
                      };

                      try {
                          const response = await fetch('/oauth/login', {
                              method: 'POST',
                              headers: { 'Content-Type': 'application/json' },
                              body: JSON.stringify(data),
                              credentials: 'include'
                          });

                          const result = await response.json();

                          if (response.ok) {
                              // Login successful - redirect to returnUrl or authorize page
                              const returnUrl = formData.get('returnUrl');
                              if (returnUrl) {
                                  window.location.href = returnUrl;
                              } else {
                                  window.location.href = '/oauth/authorize';
                              }
                          } else {
                              errorDiv.textContent = result.error_description || 'Login failed';
                              errorDiv.classList.add('show');
                              submitBtn.disabled = false;
                              submitBtn.textContent = 'Sign In';
                          }
                      } catch (error) {
                          errorDiv.textContent = 'Network error. Please try again.';
                          errorDiv.classList.add('show');
                          submitBtn.disabled = false;
                          submitBtn.textContent = 'Sign In';
                      }
                  });
              </script>
          </body>
          </html>
          """;

        return Content(html, "text/html");
    }
}

