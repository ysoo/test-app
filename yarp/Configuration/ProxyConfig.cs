using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace YarpK8sProxy.Configuration
{
    public class ProxyConfig : IProxyConfig
    {
        private readonly CancellationTokenSource _changeToken = new();

        public ProxyConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
        }

        public IReadOnlyList<RouteConfig> Routes { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken => new CancellationChangeToken(_changeToken.Token);

        public void SignalChange()
        {
            _changeToken.Cancel();
        }
    }
}
