using JobTriggerPlatform.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace JobTriggerPlatform.Infrastructure.Services;

/// <summary>
/// Implementation of the <see cref="IJenkinsService"/> interface for interacting with Jenkins CI/CD.
/// Uses token-based authentication and implements retry policies with exponential backoff.
/// </summary>
public class JenkinsService : IJenkinsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JenkinsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="JenkinsService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public JenkinsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<JenkinsService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => 
                (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                3, // Retry 3 times
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2, 4, 8 seconds
                (result, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retrying Jenkins API request after {RetryTimeSpan}s delay. Retry attempt {RetryCount}. Status code: {StatusCode}",
                        timeSpan.TotalSeconds,
                        retryCount,
                        result.Result?.StatusCode);
                });
    }

    /// <inheritdoc/>
    public async Task<JenkinsBuildResult> TriggerBuildAsync(string jobName, Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Triggering Jenkins build for job {JobName} with parameters: {@Parameters}", jobName, parameters);

            // Get Jenkins configuration
            var jenkinsUrl = _configuration["Jenkins:Url"] ?? throw new InvalidOperationException("Jenkins:Url not configured");
            var apiToken = _configuration["Jenkins:ApiToken"] ?? throw new InvalidOperationException("Jenkins:ApiToken not configured");
            var username = _configuration["Jenkins:Username"] ?? throw new InvalidOperationException("Jenkins:Username not configured");

            // Create HTTP client
            var client = _httpClientFactory.CreateClient("JenkinsClient");
            client.BaseAddress = new Uri(jenkinsUrl.TrimEnd('/') + "/");

            // Set up token authentication
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{apiToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            // Build the URL with parameters
            var urlBuilder = new StringBuilder($"job/{Uri.EscapeDataString(jobName)}/buildWithParameters?");

            // Add parameters to the URL
            foreach (var param in parameters)
            {
                urlBuilder.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}&");
            }

            // Remove the trailing '&'
            var url = urlBuilder.ToString().TrimEnd('&');

            // Execute the request with retry policy
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                // Get CSRF crumb if needed
                await AddCsrfCrumbIfNeededAsync(client, cancellationToken);

                // POST to the Jenkins API
                var result = await client.PostAsync(url, null, cancellationToken);

                // Log the response details
                if (!result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Jenkins API returned non-success status code: {StatusCode}, Content: {Content}",
                        result.StatusCode,
                        content);
                }

                return result;
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to trigger Jenkins build. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);

                return new JenkinsBuildResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Failed to trigger Jenkins build. Status: {response.StatusCode}, Error: {errorContent}"
                };
            }

            // Get the queue item URL from the Location header
            var queueItemUrl = response.Headers.Location;
            if (queueItemUrl == null)
            {
                _logger.LogWarning("Jenkins build triggered, but no queue item URL was returned.");
                return new JenkinsBuildResult
                {
                    IsSuccessful = true,
                    BuildNumber = 0,
                    Timestamp = DateTime.UtcNow,
                    EstimatedDuration = 0,
                    BuildUrl = null
                };
            }

            // Wait for the build to start
            var buildInfo = await WaitForBuildToStartAsync(client, queueItemUrl, cancellationToken);

            return buildInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering Jenkins build for job {JobName}", jobName);
            return new JenkinsBuildResult
            {
                IsSuccessful = false,
                ErrorMessage = $"Error triggering Jenkins build: {ex.Message}"
            };
        }
    }

    private async Task AddCsrfCrumbIfNeededAsync(HttpClient client, CancellationToken cancellationToken)
    {
        try
        {
            // Execute the request with retry policy for getting the crumb
            var crumbResponse = await _retryPolicy.ExecuteAsync(() =>
                client.GetAsync("crumbIssuer/api/json", cancellationToken));

            if (crumbResponse.IsSuccessStatusCode)
            {
                var crumbData = await crumbResponse.Content.ReadFromJsonAsync<JenkinsCrumb>(cancellationToken);

                if (crumbData != null && !string.IsNullOrEmpty(crumbData.Crumb))
                {
                    // Remove any existing crumb header and add a new one
                    if (client.DefaultRequestHeaders.Contains(crumbData.CrumbRequestField))
                    {
                        client.DefaultRequestHeaders.Remove(crumbData.CrumbRequestField);
                    }

                    client.DefaultRequestHeaders.Add(crumbData.CrumbRequestField, crumbData.Crumb);
                    _logger.LogDebug("Added CSRF crumb to Jenkins request: {CrumbField}={Crumb}",
                        crumbData.CrumbRequestField, crumbData.Crumb);
                }
            }
            else
            {
                _logger.LogDebug("Failed to get CSRF crumb from Jenkins. Status code: {StatusCode}",
                    crumbResponse.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CSRF crumb from Jenkins. The server might not have CSRF protection enabled.");
        }
    }

    private async Task<JenkinsBuildResult> WaitForBuildToStartAsync(HttpClient client, Uri queueItemUrl, CancellationToken cancellationToken)
    {
        // Parse the queue item ID from the URL
        var queueItemPath = queueItemUrl.AbsolutePath;
        var queueItemId = queueItemPath.Split('/').Last(s => !string.IsNullOrEmpty(s));
        var queueItemApiUrl = $"queue/item/{queueItemId}/api/json";

        // Maximum number of attempts and delay between attempts
        const int maxAttempts = 5;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                // Execute the request with retry policy
                var queueItemResponse = await _retryPolicy.ExecuteAsync(() =>
                    client.GetAsync(queueItemApiUrl, cancellationToken));

                if (!queueItemResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to get queue item status. Status code: {StatusCode}. Attempt {Attempt}/{MaxAttempts}",
                        queueItemResponse.StatusCode, attempt + 1, maxAttempts);

                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5); // Increase delay by 50%
                    continue;
                }

                var queueItem = await queueItemResponse.Content.ReadFromJsonAsync<JenkinsQueueItem>(cancellationToken);

                if (queueItem == null)
                {
                    _logger.LogWarning("Failed to deserialize Jenkins queue item response. Attempt {Attempt}/{MaxAttempts}",
                        attempt + 1, maxAttempts);

                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5);
                    continue;
                }

                // Check if the build has started
                if (queueItem.Executable != null)
                {
                    // Get the build details
                    var buildUrl = queueItem.Executable.Url;
                    var buildApiUrl = $"{buildUrl.TrimEnd('/')}/api/json";

                    // Execute the request with retry policy
                    var buildResponse = await _retryPolicy.ExecuteAsync(() =>
                        client.GetAsync(buildApiUrl, cancellationToken));

                    if (buildResponse.IsSuccessStatusCode)
                    {
                        var buildData = await buildResponse.Content.ReadFromJsonAsync<JenkinsBuild>(
                            cancellationToken: cancellationToken,
                            options: new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        if (buildData != null)
                        {
                            _logger.LogInformation(
                                "Jenkins build started. Build number: {BuildNumber}, URL: {BuildUrl}",
                                buildData.Number, buildData.Url);

                            return new JenkinsBuildResult
                            {
                                IsSuccessful = true,
                                BuildNumber = buildData.Number,
                                BuildUrl = buildData.Url,
                                EstimatedDuration = buildData.EstimatedDuration,
                                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(buildData.Timestamp).DateTime
                            };
                        }
                    }

                    // If we couldn't get the build details, return a basic result
                    _logger.LogInformation(
                        "Jenkins build started but details not available. Build number: {BuildNumber}, URL: {BuildUrl}",
                        queueItem.Executable.Number, queueItem.Executable.Url);

                    return new JenkinsBuildResult
                    {
                        IsSuccessful = true,
                        BuildNumber = queueItem.Executable.Number,
                        BuildUrl = queueItem.Executable.Url,
                        Timestamp = DateTime.UtcNow,
                        EstimatedDuration = 0
                    };
                }

                _logger.LogDebug(
                    "Build not yet started. Queue item: {QueueItemId}, Waiting before checking again. Attempt {Attempt}/{MaxAttempts}",
                    queueItemId, attempt + 1, maxAttempts);

                // Wait before checking again
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for Jenkins build to start. Attempt {Attempt}/{MaxAttempts}",
                    attempt + 1, maxAttempts);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5);
            }
        }

        // If we couldn't get the build details after several attempts, return a basic successful result
        _logger.LogWarning(
            "Could not determine build details after {MaxAttempts} attempts. Assuming build was triggered successfully.",
            maxAttempts);

        return new JenkinsBuildResult
        {
            IsSuccessful = true,
            BuildNumber = 0,
            BuildUrl = queueItemUrl.ToString(),
            Timestamp = DateTime.UtcNow,
            EstimatedDuration = 0
        };
    }

    // Helper models for Jenkins API responses
    private class JenkinsCrumb
    {
        public string? Crumb { get; set; }
        public string CrumbRequestField { get; set; } = "Jenkins-Crumb";
    }

    private class JenkinsQueueItem
    {
        public JenkinsExecutable? Executable { get; set; }
    }

    private class JenkinsExecutable
    {
        public int Number { get; set; }
        public string? Url { get; set; }
    }

    private class JenkinsBuild
    {
        public int Number { get; set; }
        public string? Url { get; set; }
        public long Timestamp { get; set; }
        public long EstimatedDuration { get; set; }
    }
}