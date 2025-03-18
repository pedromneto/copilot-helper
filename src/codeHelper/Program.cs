using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Net.Http.Headers;

string GITHUB_APP_NAME = "poc-copilot-helper";
string GITHUB_COPILOT_COMPLETIONS_URL = "https://api.githubcopilot.com/chat/completions";

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();



app.MapGet("/", () => "Hello Copilot");

app.MapGet("/callback", () => "You may close this tab and " + 
    "return to GitHub.com (where you should refresh the page " +
    "and start a fresh chat). If you're using VS Code or " +
    "Visual Studio, return there.");

app.MapPost("/agent", async (
    [FromHeader(Name = "X-GitHub-Token")] string githubToken,
    [FromBody] Request userRequest
) =>
{
    var octoKitClient = new GitHubClient(new Octokit.ProductHeaderValue(GITHUB_APP_NAME))
    {
        Credentials = new Credentials(githubToken)
    };
    var user = await octoKitClient.User.Current();

    userRequest.Messages.Insert(0, new Message
    {
        Role = "system",
        Content =
        $"Start every response with the user's name, which is @{user.Login}"
    });
    userRequest.Messages.Insert(0, new Message
    {
        Role = "system",
        Content = "You are a helpful assistant that replies to user messages as if you were Blackbeard the Pirate."
    });

    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
    userRequest.Stream = true;
    var copilotLLMResponse = await httpClient.PostAsJsonAsync(GITHUB_COPILOT_COMPLETIONS_URL, userRequest);
    var responseStream = await copilotLLMResponse.Content.ReadAsStreamAsync();

    return Results.Stream(responseStream, "application/json");

});

app.Run();
