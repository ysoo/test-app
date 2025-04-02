using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace YarpK8sProxy.Configuration
{
    public class K8sProxyConfigProvider : IProxyConfigProvider
    {
        private ProxyConfig _config;
        private readonly ILogger<K8sProxyConfigProvider> _logger;

        public K8sProxyConfigProvider(ILogger<K8sProxyConfigProvider> logger)
        {
            _logger = logger;
            
            _logger.LogInformation("Initializing proxy configuration");

            var routes = new[]
            {
                new RouteConfig
                {
                    RouteId = "test-route",
                    ClusterId = "test-cluster",
                    Match = new RouteMatch
                    {
                        Path = "/test"
                    },
                    Transforms = new[]
                    {
                        new Dictionary<string, string>
                        {
                            { "PathRemovePrefix", "/test" }
                        }
                    }
                },
                new RouteConfig
                {
                    RouteId = "public-test-route",
                    ClusterId = "test-cluster",
                    Match = new RouteMatch
                    {
                        Path = "/public/test"
                    },
                    Transforms = new[]
                    {
                        new Dictionary<string, string>
                        {
                            { "PathRemovePrefix", "/public/test" }
                        }
                    }
                },
                // Root path route
                new RouteConfig
                {
                    RouteId = "root-route",
                    ClusterId = "test-cluster",
                    Match = new RouteMatch
                    {
                        Path = "/"
                    }
                }
            };

            var clusters = new[]
            {
                new ClusterConfig
                {
                    ClusterId = "test-cluster",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        { "test-app", new DestinationConfig 
                            { 
                                Address = "http://test-app:8080/"
                            } 
                        }
                    }
                }
            };

            _config = new ProxyConfig(routes, clusters);
            _logger.LogInformation("Proxy configuration initialized with routes: /, /test, and /public/test");
        }

        public IProxyConfig GetConfig() => _config;

        public Task UpdateAsync(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            _logger.LogInformation("Updating proxy configuration with {RouteCount} routes and {ClusterCount} clusters",
                routes.Count, clusters.Count);

            // Create new config with new values
            var newConfig = new ProxyConfig(routes, clusters);

            // Replace the existing config
            var oldConfig = _config;
            _config = newConfig;

            // Signal that the old config has changed to trigger reload
            oldConfig.SignalChange();

            return Task.CompletedTask;
        }
    }
}
