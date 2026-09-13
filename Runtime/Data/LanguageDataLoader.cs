using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Localization
{
    internal static class LanguageDataLoader
    {
        private static readonly Dictionary<string, AsyncOperationHandle<LanguageDataSO>> CachedHandles =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, LanguageDataSO> CachedResources =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> ResolvedAddresses =
            new(StringComparer.OrdinalIgnoreCase);

        public static bool TryLoadDataResource(string resourceName, out LanguageDataSO resource)
        {
            resource = null;

            if (!TryNormalizeResourceName(resourceName, out string requestedAddress))
                return false;

#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogError(
                $"[Localization] Cannot synchronously load Addressable language data '{requestedAddress}' on WebGL. " +
                $"Use {nameof(LoadDataResourceAsync)} instead.");
            return false;
#else
            if (!TryResolveAddress(requestedAddress, out string address))
                return false;

            if (TryGetCached(address, out resource))
                return true;

            var handle = GetOrCreateHandle(address);
            try
            {
                if (!handle.IsDone)
                    handle.WaitForCompletion();

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    resource = handle.Result;
                    CachedResources[address] = resource;
                    return true;
                }

                LogLoadFailure(address, handle);
                ReleaseHandle(address, handle);
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Localization] Failed to load Addressable language data '{address}': {exception}");
                ReleaseHandle(address, handle);
                return false;
            }
#endif
        }

        public static async Task<LanguageDataSO> LoadDataResourceAsync(string resourceName)
        {
            if (!TryNormalizeResourceName(resourceName, out string requestedAddress))
                return null;

            string address = await ResolveAddressAsync(requestedAddress);
            if (string.IsNullOrEmpty(address))
                return null;

            if (TryGetCached(address, out var resource))
                return resource;

            var handle = GetOrCreateHandle(address);
            try
            {
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    resource = handle.Result;
                    CachedResources[address] = resource;
                    return resource;
                }

                LogLoadFailure(address, handle);
                ReleaseHandle(address, handle);
                return null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Localization] Failed to load Addressable language data '{address}': {exception}");
                ReleaseHandle(address, handle);
                return null;
            }
        }

        public static void ReleaseCachedDataResources()
        {
            foreach (var handle in CachedHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            CachedHandles.Clear();
            CachedResources.Clear();
            ResolvedAddresses.Clear();
        }

        private static bool TryNormalizeResourceName(string resourceName, out string address)
        {
            address = string.IsNullOrWhiteSpace(resourceName) ? string.Empty : resourceName.Trim();
            if (!string.IsNullOrEmpty(address))
                return true;

            Debug.LogError("[Localization] Addressable language data address is empty.");
            return false;
        }

        private static bool TryResolveAddress(string requestedAddress, out string address)
        {
            if (ResolvedAddresses.TryGetValue(requestedAddress, out address))
                return true;

            string[] candidates = BuildAddressCandidates(requestedAddress);
            foreach (var candidate in candidates)
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(candidate, typeof(LanguageDataSO));
                try
                {
                    if (!locationsHandle.IsDone)
                        locationsHandle.WaitForCompletion();

                    if (locationsHandle is { Status: AsyncOperationStatus.Succeeded, Result: { Count: > 0 } })
                    {
                        address = candidate;
                        ResolvedAddresses[requestedAddress] = address;
                        return true;
                    }
                }
                finally
                {
                    if (locationsHandle.IsValid())
                        Addressables.Release(locationsHandle);
                }
            }

            Debug.LogError(
                $"[Localization] No Addressable language data found for '{requestedAddress}'. " +
                $"Tried: {string.Join(", ", candidates)}");
            address = string.Empty;
            return false;
        }

        private static async Task<string> ResolveAddressAsync(string requestedAddress)
        {
            if (ResolvedAddresses.TryGetValue(requestedAddress, out string address))
                return address;

            string[] candidates = BuildAddressCandidates(requestedAddress);
            foreach (string candidate in candidates)
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(candidate, typeof(LanguageDataSO));
                try
                {
                    await locationsHandle.Task;

                    if (locationsHandle is { Status: AsyncOperationStatus.Succeeded, Result: { Count: > 0 } })
                    {
                        ResolvedAddresses[requestedAddress] = candidate;
                        return candidate;
                    }
                }
                finally
                {
                    if (locationsHandle.IsValid())
                        Addressables.Release(locationsHandle);
                }
            }

            Debug.LogError(
                $"[Localization] No Addressable language data found for '{requestedAddress}'. " +
                $"Tried: {string.Join(", ", candidates)}");
            return null;
        }

        private static string[] BuildAddressCandidates(string requestedAddress)
        {
            if (requestedAddress.IndexOf('/') >= 0 || requestedAddress.IndexOf('\\') >= 0)
            {
                string normalized = requestedAddress.Replace('\\', '/');
                if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    return new[] { normalized };

                return new[] { normalized, $"Assets/Resources/{AppendAssetExtension(normalized)}" };
            }

            return new[]
            {
                requestedAddress,
                $"Localization/{requestedAddress}",
                $"Assets/Resources/Localization/{AppendAssetExtension(requestedAddress)}"
            };
        }

        private static string AppendAssetExtension(string address)
        {
            return address.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ? address : $"{address}.asset";
        }

        private static bool TryGetCached(string address, out LanguageDataSO resource)
        {
            if (CachedResources.TryGetValue(address, out resource) && resource != null)
                return true;

            CachedResources.Remove(address);
            resource = null;
            return false;
        }

        private static AsyncOperationHandle<LanguageDataSO> GetOrCreateHandle(string address)
        {
            if (CachedHandles.TryGetValue(address, out var handle) &&
                handle.IsValid())
            {
                return handle;
            }

            handle = Addressables.LoadAssetAsync<LanguageDataSO>(address);
            CachedHandles[address] = handle;
            return handle;
        }

        private static void ReleaseHandle(string address, AsyncOperationHandle<LanguageDataSO> handle)
        {
            if (handle.IsValid())
                Addressables.Release(handle);

            CachedHandles.Remove(address);
            CachedResources.Remove(address);
        }

        private static void LogLoadFailure(string address, AsyncOperationHandle<LanguageDataSO> handle)
        {
            string reason = handle.OperationException == null ? "unknown error" : handle.OperationException.Message;
            Debug.LogError($"[Localization] Failed to load Addressable language data '{address}': {reason}");
        }
    }
}
