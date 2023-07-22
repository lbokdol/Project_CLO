using System.Collections.Concurrent;

using Project_CLO.Common;

namespace Project_CLO.Services
{
    public class StatisticsService
    {
        private readonly ConcurrentDictionary<string, APIInformation> _apiCount = new ConcurrentDictionary<string, APIInformation>();
        private readonly EndpointDataSource _endpointDataSource;

        public StatisticsService(EndpointDataSource endpointDataSource) 
        {
            _endpointDataSource = endpointDataSource;
        }

        public async Task UpsertApiInformation(string path, MethodType methodType)
        {
            var apiPath = path.ToLower();
            var apiList = GetAPIList();

            if (apiList.Any(api => api.Equals(apiPath)) == false)
                return;

            _apiCount.AddOrUpdate($"{apiPath}_{methodType}", new APIInformation()
            {
                Path = path,
                Count = 1,
                MethodType = methodType
            }, (path, existingValue) =>
            {
                existingValue.Count++;

                return existingValue;
            });
        }

        public async Task<APIInformation> GetAPIInformation(string path, MethodType methodType)
        {
            var apiPath = path.ToLower();

            var apiList = GetAPIList();
            
            if (apiList.Any(api => api.Equals(apiPath)) == false)
                return null;
            
            var apiInformation = _apiCount.GetOrAdd($"{apiPath}_{methodType}", new APIInformation()
            {
                Path = path,
                Count = 0,
                MethodType = methodType
            });
            
            return apiInformation;
        }

        private List<string> GetAPIList()
        {
            var routes = _endpointDataSource.Endpoints
                .Where(endpoint => endpoint is RouteEndpoint)
                .Select(endpoint => (RouteEndpoint)endpoint)
                .Select(routeEndpoint => $"/{routeEndpoint.RoutePattern.RawText.ToLower()}");

            return routes.ToList();
        }
    }
}
