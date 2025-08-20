using System.Collections;
using MelonLoader;
using EnhancedCartel.Helpers;
using UnityEngine;
#if MONO
using ScheduleOne.Cartel;
using ScheduleOne.Product;
#else
using Il2CppScheduleOne.Cartel;
using Il2CppScheduleOne.Product;
#endif

using Object = UnityEngine.Object;

[assembly: MelonInfo(
    typeof(EnhancedCartel.EnhancedCartel),
    EnhancedCartel.BuildInfo.Name,
    EnhancedCartel.BuildInfo.Version,
    EnhancedCartel.BuildInfo.Author
)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace EnhancedCartel;

public static class BuildInfo
{
    public const string Name = "EnhancedCartel";
    public const string Description = "Allows cartel to request other products than default";
    public const string Author = "k073l";
    public const string Version = "1.0.0";
}

public class EnhancedCartel : MelonMod
{
    private static MelonLogger.Instance Logger;
    public static CartelDealManager CartelDealManagerInstance;
    
    public static MelonPreferences_Category Category;
    public static MelonPreferences_Entry<int> ProductQuantityMin;
    public static MelonPreferences_Entry<int> ProductQuantityMax;
    public static MelonPreferences_Entry<bool> UseListedProducts;
    public static MelonPreferences_Entry<bool> UseDiscoveredProducts;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Logger.Msg("EnhancedCartel initialized");
        
        // Initialize preferences
        Category = MelonPreferences.CreateCategory("EnhancedCartel", "Enhanced Cartel Settings");
        ProductQuantityMin = Category.CreateEntry("ProductQuantityMin", 10, description: "Minimum quantity of products in cartel requests");
        ProductQuantityMax = Category.CreateEntry("ProductQuantityMax", 40, description: "Maximum quantity of products in cartel requests");
        UseListedProducts = Category.CreateEntry("UseListedProducts", true, description: "Use products that are listed for sale in cartel requests");
        UseDiscoveredProducts = Category.CreateEntry("UseDiscoveredProducts", false, description: "Use products that have been discovered in cartel requests. Overrides UseListedProducts if true");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        Logger.Debug($"Scene loaded: {sceneName}");
        if (sceneName == "Main")
        {
            Logger.Debug("Main scene loaded, waiting for player");
            MelonCoroutines.Start(Utils.WaitForPlayer(CaptureCartelDealManager()));
        }
    }

    private IEnumerator CaptureCartelDealManager()
    {
        // I expect that network is ready by now
        var cartelDealManager = Object.FindObjectOfType<CartelDealManager>();
        if (cartelDealManager == null)
        {
            Logger.Error("CartelDealManager not found in the scene.");
            yield return null;
        }
        CartelDealManagerInstance = cartelDealManager;
        Logger.Debug("Captured CartelDealManager instance");
        yield return TweakCartelProducts();
    }

    /// <summary>
    /// Tweaks the cartel products to allow for other requests.
    /// I **think** there might be a race condition, but it's fine
    /// </summary>
    private IEnumerator TweakCartelProducts()
    {
        if (CartelDealManagerInstance == null)
        {
            Logger.Error("CartelDealManagerInstance is null, cannot tweak products.");
            yield break;
        }

        CartelDealManagerInstance.ProductQuantityMin = ProductQuantityMin.Value;
        CartelDealManagerInstance.ProductQuantityMax = ProductQuantityMax.Value;
        
        // Add custom products
        yield return WaitForPM();
        
        var defaultRequestable = CartelDealManagerInstance.RequestableProducts.AsEnumerable();
        var listedProducts = 
            UseListedProducts.Value && !UseDiscoveredProducts.Value // if UseDiscoveredProducts is true, listed products are ignored
                ? ProductManager.ListedProducts.AsEnumerable() 
                : Enumerable.Empty<ProductDefinition>();
        var discoveredProducts = 
            UseDiscoveredProducts.Value 
                ? ProductManager.DiscoveredProducts.AsEnumerable()
                : Enumerable.Empty<ProductDefinition>();
        var newRequestable = listedProducts
            .Union(defaultRequestable)
            .Union(discoveredProducts)
            .ToList();
        CartelDealManagerInstance.RequestableProducts = newRequestable.ToArray();
        foreach (var product in CartelDealManagerInstance.RequestableProducts)
        {
            Logger.Debug($"Requestable product: {product.Name} (ID: {product.ID})");
        }
        Logger.Debug($"CartelDealManager.RequestableProducts count: {CartelDealManagerInstance.RequestableProducts.Length}");
        Logger.Msg($"Cartel can now request following products: {string.Join(", ", CartelDealManagerInstance.RequestableProducts.Select(p => p.Name))}");
    }

    private IEnumerator WaitForPM()
    {
        while (!ProductManager.InstanceExists)
        {
            yield return new WaitForSeconds(1f);
        }
        // One more for good measure
        yield return new WaitForSeconds(1f);
    }
}