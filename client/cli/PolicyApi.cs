using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mjcheetham.Git.Policy.Cli
{
    public interface IPolicyApi : IDisposable
    {
        Task<Profile?> GetProfileAsync();

        Task<Policy?> GetPolicyAsync(string policyId);
    }

    public class PolicyApi : IPolicyApi
    {
        private readonly Uri _baseUri;
        private readonly HttpClient _httpClient;

        public PolicyApi(string baseUrl) : this(new Uri(baseUrl)) { }

        public PolicyApi(Uri baseUri)
        {
            _baseUri = baseUri;
            _httpClient = new HttpClient();
        }

        public async Task<Profile?> GetProfileAsync()
        {
            var requestUri = CreateRequestUri("api/profile");
            var identity = ClientIdentity.Create();
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri).WithJsonContent(identity);

            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("Failed to get profile information", null, response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<Profile>();
        }

        public async Task<Policy?> GetPolicyAsync(string policyId)
        {
            var requestUri = CreateRequestUri($"api/policy/{policyId}");
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Policy>();
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw new HttpRequestException("Failed to get policy information", null, response.StatusCode);
        }

        private Uri CreateRequestUri(string api) => new(_baseUri, api);

        public void Dispose() => _httpClient.Dispose();
    }

    internal class ClientIdentity
    {
        [JsonPropertyName("hmac")]
        public string HashedMac { get; }

        private ClientIdentity(string hashedMac)
        {
            HashedMac = hashedMac;
        }

        public static ClientIdentity Create()
        {
            static bool IsInetType(NetworkInterface nic)
            {
                return nic.OperationalStatus == OperationalStatus.Up &&
                       (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
            }

            NetworkInterface? nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(IsInetType);
            if (nic is not null)
            {
                PhysicalAddress macAddress = nic.GetPhysicalAddress();
                byte[] hashedMacBytes = SHA256.HashData(macAddress.GetAddressBytes());
                string hashedMac = Convert.ToHexString(hashedMacBytes);
                return new ClientIdentity(hashedMac);
            }

            return new ClientIdentity(string.Empty);
        }
    }

    internal static class HttpRequestMessageExtension
    {
        public static HttpRequestMessage WithJsonContent(this HttpRequestMessage request,
            object? obj, MediaTypeHeaderValue? mediaType = null)
        {
            request.Content = JsonContent.Create(obj, mediaType);
            return request;
        }
    }
}
