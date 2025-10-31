using MelonLoader;
using EnhancedCartel.Helpers;
using HarmonyLib;
using MelonLoader.Preferences;
using UnityEngine;
#if MONO
using FishNet;
using ScheduleOne.GameTime;
using ScheduleOne.Cartel;
using ScheduleOne.Levelling;
using ScheduleOne.Product;
#else
using Il2CppFishNet;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Cartel;
using Il2CppScheduleOne.Levelling;
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

#if (MONO)
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#endif

namespace EnhancedCartel;

public static class BuildInfo
{
    public const string Name = "EnhancedCartel";
    public const string Description = "Allows cartel to request other products than default";
    public const string Author = "k073l";
    public const string Version = "1.1.0";
}

public class EnhancedCartel : MelonMod
{
    private static MelonLogger.Instance Logger;

    public static MelonPreferences_Category Category;
    public static MelonPreferences_Entry<int> ProductQuantityMin;
    public static MelonPreferences_Entry<int> ProductQuantityMax;
    public static MelonPreferences_Entry<bool> UseListedProducts;
    public static MelonPreferences_Entry<bool> UseDiscoveredProducts;
    public static MelonPreferences_Entry<int> RoundingMultiple;

    public static int CeilToNearest(int value, int multiple)
        => ((value + multiple - 1) / multiple) * multiple;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Logger.Msg("EnhancedCartel initialized");

        // Initialize preferences
        Category = MelonPreferences.CreateCategory("EnhancedCartel", "Enhanced Cartel Settings");
        ProductQuantityMin = Category.CreateEntry("ProductQuantityMin", 10,
            description: "Minimum quantity of products in cartel requests");
        ProductQuantityMax = Category.CreateEntry("ProductQuantityMax", 40,
            description: "Maximum quantity of products in cartel requests");
        UseListedProducts = Category.CreateEntry("UseListedProducts", true,
            description: "Use products that are listed for sale in cartel requests");
        UseDiscoveredProducts = Category.CreateEntry("UseDiscoveredProducts", false,
            description:
            "Use products that have been discovered in cartel requests. Overrides UseListedProducts if true");
        RoundingMultiple = Category.CreateEntry("RoundingMultiple", 1,
            description:
            "Round requested product quantities to nearest multiple of this value (1 = no rounding) [1-20]",
            validator: new ValueRange<int>(1, 20));
    }
}

[HarmonyPatch(typeof(CartelDealManager), nameof(CartelDealManager.StartDeal))]
class CartelDealManager_StartDeal_Patch
{
    public static bool Prefix(CartelDealManager __instance)
    {
        if (InstanceFinder.IsServer)
        {
            __instance.ProductQuantityMin = EnhancedCartel.ProductQuantityMin.Value;
            __instance.ProductQuantityMax = EnhancedCartel.ProductQuantityMax.Value;

            if (!EnhancedCartel.UseDiscoveredProducts.Value && !EnhancedCartel.UseListedProducts.Value)
            {
                // Mod off, use default behavior
                return true;
            }

            var fullRank = new FullRank(ERank.Kingpin, 0);
            var rankProgress = Mathf.Clamp01(LevelManager.Instance.GetFullRank().ToFloat() / fullRank.ToFloat());

            var listedProducts =
                EnhancedCartel.UseListedProducts.Value &&
                !EnhancedCartel.UseDiscoveredProducts
                    .Value // if UseDiscoveredProducts is true, listed products are ignored;
                    ? ProductManager.ListedProducts.AsEnumerable()
                    : Enumerable.Empty<ProductDefinition>();
            var discoveredProducts =
                EnhancedCartel.UseDiscoveredProducts.Value
                    ? ProductManager.DiscoveredProducts.AsEnumerable()
                    : Enumerable.Empty<ProductDefinition>();
            var newRequestable = listedProducts
                // .Union(__instance.RequestableWeed.AsEnumerable())
                .Union(discoveredProducts)
                .ToList();

            var productDef = newRequestable[UnityEngine.Random.Range(0, newRequestable.Count)];
            var quantity = EnhancedCartel.CeilToNearest(
                Mathf.RoundToInt(Mathf.Lerp(__instance.ProductQuantityMin, __instance.ProductQuantityMax,
                    rankProgress)), EnhancedCartel.RoundingMultiple.Value);

            var dateTime = TimeManager.Instance.GetDateTime();
            dateTime.elapsedDays += 3;
            dateTime.time = 401;

            var payment =
                Mathf.RoundToInt(productDef.MarketValue * quantity * 0.65f); // Cartel pays 65% of market value

            var cartelDealInfo =
                new CartelDealInfo(productDef.ID, quantity, payment, dateTime, CartelDealInfo.EStatus.Pending);
            __instance.InitializeDealQuest(null, cartelDealInfo);
            __instance.SendRequestMessage(cartelDealInfo);
            __instance.ActiveDeal = cartelDealInfo;
            return false;
        }

        return true;
    }
}