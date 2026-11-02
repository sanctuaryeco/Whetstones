// Whestone.cs version 0.14.1.1+

namespace Eco.Mods.TechTree
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Eco.Core.Items;
    using Eco.Gameplay.Components;
    using Eco.Gameplay.Economy;
    using Eco.Gameplay.Items;
    using Eco.Gameplay.Objects;
    using Eco.Gameplay.Players;
    using Eco.Gameplay.Skills;
    using Eco.Shared.Localization;
    using Eco.Shared.Serialization;
    using Eco.Shared.Utils;
    using Eco.Core.Controller;
    using Eco.Core.Utils;
    using Eco.Gameplay.Items.Recipes;
	using Eco.Core.Plugins.Interfaces;
	using Eco.Core.Plugins;


// *************************************************************************************
// **********  Repair and crafting settings from Configs/WhetstonesConfig.eco.  ********
// *************************************************************************************
    [Localized]
    public class WhetstonesSettings
    {
        [Category("Repair")][LocDescription("Fixed percentage points of maximum integrity removed on every whetstone repair")]
        public float IntegrityLossPoints { get; set; } = 15f;

        [Category("Crafting - Wood")][LocDescription("Skill class used to craft Wood Whetstones")]
        public string WoodCraftSkill { get; set; } = nameof(CarpentrySkill);
        [Category("Crafting - Wood")][LocDescription("Required skill level for Wood Whetstones (cached vanilla range: 1 to 6)")]
        public int WoodCraftLevel { get; set; } = 1;

        [Category("Crafting - Stone")][LocDescription("Skill class used to craft Stone Whetstones")]
        public string StoneCraftSkill { get; set; } = nameof(BasicEngineeringSkill);
        [Category("Crafting - Stone")][LocDescription("Required skill level for Stone Whetstones (cached vanilla range: 1 to 5)")]
        public int StoneCraftLevel { get; set; } = 1;

        [Category("Crafting - Iron")][LocDescription("Skill class used to craft Iron Whetstones")]
        public string IronCraftSkill { get; set; } = nameof(BlacksmithSkill);
        [Category("Crafting - Iron")][LocDescription("Required skill level for Iron Whetstones (cached vanilla range: 1 to 6)")]
        public int IronCraftLevel { get; set; } = 2;

        [Category("Crafting - Steel")][LocDescription("Skill class used to craft Steel Whetstones")]
        public string SteelCraftSkill { get; set; } = nameof(BlacksmithSkill);
        [Category("Crafting - Steel")][LocDescription("Required skill level for Steel Whetstones (cached vanilla range: 1 to 6)")]
        public int SteelCraftLevel { get; set; } = 4;

        [Category("Crafting - Tractor")][LocDescription("Skill class used to craft Tractor Whetstones")]
        public string TractorCraftSkill { get; set; } = nameof(MechanicsSkill);
        [Category("Crafting - Tractor")][LocDescription("Required skill level for Tractor Whetstones (cached vanilla range: 1 to 5)")]
        public int TractorCraftLevel { get; set; } = 3;

        [Category("Crafting - Gasoline")][LocDescription("Skill class used to craft Modern Gas Whetstones")]
        public string GasolineCraftSkill { get; set; } = nameof(OilDrillingSkill);
        [Category("Crafting - Gasoline")][LocDescription("Required skill level for Modern Gas Whetstones (cached vanilla range: 1 to 5)")]
        public int GasolineCraftLevel { get; set; } = 5;
    }
    public class WhetstonesConfig : IModKitPlugin, IInitializablePlugin, IConfigurablePlugin, IDisplayablePlugin
    {
        public static PluginConfig<WhetstonesSettings>? config;
        private string status = "";
        public IPluginConfig? PluginConfig => config;
        public ThreadSafeAction<object, string> ParamChanged { get; set; } = new ThreadSafeAction<object, string>();

        public static WhetstonesSettings Settings
        {
            get
            {
                config ??= new PluginConfig<WhetstonesSettings>("WhetstonesConfig");
                return config.Config;
            }
        }

        public static Type SkillType(string configuredName, Type fallback)
        {
            if (string.IsNullOrWhiteSpace(configuredName)) return fallback;
            var fullName = configuredName.Contains('.') ? configuredName : $"Eco.Mods.TechTree.{configuredName}";
            var configuredType = typeof(BasicEngineeringSkill).Assembly.GetType(fullName, throwOnError: false, ignoreCase: true);
            var supported = configuredType == typeof(CarpentrySkill)
                || configuredType == typeof(BasicEngineeringSkill)
                || configuredType == typeof(BlacksmithSkill)
                || configuredType == typeof(MechanicsSkill)
                || configuredType == typeof(OilDrillingSkill);
            return supported ? configuredType! : fallback;
        }

        public static RequiresSkillAttribute[] CachedRequirement(Type skillType, int configuredLevel)
        {
            Type[] sources;
            int maxLevel;

            if (skillType == typeof(CarpentrySkill)) { sources = CarpentrySources; maxLevel = 6; }
            else if (skillType == typeof(BasicEngineeringSkill)) { sources = BasicEngineeringSources; maxLevel = 5; }
            else if (skillType == typeof(BlacksmithSkill)) { sources = BlacksmithSources; maxLevel = 6; }
            else if (skillType == typeof(MechanicsSkill)) { sources = MechanicsSources; maxLevel = 5; }
            else if (skillType == typeof(OilDrillingSkill)) { sources = OilDrillingSources; maxLevel = 5; }
            else { sources = BasicEngineeringSources; maxLevel = 5; }

            var level = Math.Max(1, Math.Min(maxLevel, configuredLevel));
            return RequiresSkillAttribute.Cache.Get(sources[level]);
        }

        private static readonly Type[] CarpentrySources =
        {
            typeof(FarEastLumberRecipe), typeof(FarEastLumberRecipe),
            typeof(CarpentryBasicUpgradeRecipe), typeof(CoffeeTableRecipe),
            typeof(BookshelfRecipe), typeof(CarpenterSignRecipe),
            typeof(HardwoodLumberTableRecipe), typeof(HardwoodLumberTableRecipe),
        };

        private static readonly Type[] BasicEngineeringSources =
        {
            typeof(ArrastraRecipe), typeof(ArrastraRecipe),
            typeof(BasicEngineeringUpgradeRecipe), typeof(IronRoadToolRecipe),
            typeof(WoodenLiquidTankRecipe), typeof(PoweredCartRecipe),
        };

        private static readonly Type[] BlacksmithSources =
        {
            typeof(CookingUtensilsRecipe), typeof(CookingUtensilsRecipe),
            typeof(IronAnchorRecipe), typeof(NorseAxeRecipe),
            typeof(SteelSawBladeRecipe), typeof(ChainsawRecipe),
            typeof(ModernPaintSprayerRecipe), typeof(ModernPaintSprayerRecipe),
        };

        private static readonly Type[] MechanicsSources =
        {
            typeof(BoilerRecipe), typeof(BoilerRecipe),
            typeof(HeatSinkRecipe), typeof(CombustionEngineRecipe),
            typeof(GoldWiringRecipe), typeof(CraneRecipe),
        };

        private static readonly Type[] OilDrillingSources =
        {
            typeof(PetroleumRecipe), typeof(PetroleumRecipe),
            typeof(GasolineRecipe), typeof(SyntheticPhosphateFertilizerRecipe),
            typeof(PlasticOutdoorTableRecipe), typeof(BiodieselRecipe),
        };

        public void Initialize(TimedTask timer)
        {
            this.status = "Reading config";
            _ = Settings;
            this.status = "";
        }
        public override string ToString() => Localizer.DoStr("Whetstones Repair");
        public string GetDisplayText() => this.status;
        public string GetStatus() => this.status;
        public string GetCategory() => "GadgetPaPa";
        public object? GetEditObject() => config?.Config;
        public void OnEditObjectChanged(object o, string param)
        {
        }
    }

// *************************************************************************************
// ********** Tool Breaker Breaks tools                                       **********
// *************************************************************************************
	[Serialized]
    [LocDisplayName("Tool Breaker")]
    [Category("Hidden")]
    [Weight(1)]
    [MaxStackSize(1)]
    [LocDescription("Break selected tool immediately")]
    public class ToolBreakerItem : Item
    {
        public LocString DisplayDescription => Localizer.DoStr("Break selected tool immediately");

        public override string OnUsed(Player player, ItemStack itemStack)
        {
            var selectedItem = player.User.Inventory.Toolbar.SelectedItem;
            if (selectedItem is PartItem Part) Part.Durability = 0;               // return base.OnUsed(player, itemStack);
            if (selectedItem is VehicleToolItem VehicleTool) VehicleTool.Durability = 0;
            if (selectedItem is not ToolItem tool) return string.Empty;
            tool.Durability = 0;
            return base.OnUsed(player, itemStack);
        }
    }

// *************************************************************************************
// ********** Universal Repair Tool, Repairs Tools, Vehicles Tools & Parts    **********
// *************************************************************************************
	[Serialized]
    [RequireComponent(typeof(RepairComponent))]
    [LocDisplayName("Universal Repair Tool")]
    [Category("Hidden")]
    [Weight(1)]
    [MaxStackSize(1)]
    [LocDescription("Repair selected tool immediately")]
    public class RepairToolItem : Item
    {
        public LocString DisplayDescription => Localizer.DoStr("Repair selected tool immediately");

        public override string OnUsed(Player player, ItemStack itemStack)
        {
            var selectedItem = player.User.Inventory.Toolbar.SelectedItem;
            /*if (selectedItem is FoodItem Food)
            {
                //Food.Spoilage = Food.OriginalSpoilageItem;
                //Food.SpoilageBaseShelfLife = Food.BaseShelfLife;
                Food.shelfLifeMultiplier = 100f;
				player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} repaired to " + Food.OriginalMaxDurability + "% durability!"));
            }*/
            // Repairs Parts to Original Durability
			if (selectedItem is PartItem Part)
			{
				Part.Durability = Part.OriginalMaxDurability;
				Part.IntegrityAmount = Part.OriginalMaxDurability;
				player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} repaired to "+Part.OriginalMaxDurability +"% durability!"));									  
			}
            // Repairs Vehicle Tools to Original Durability, Tractor parts etc.
            if (selectedItem is VehicleToolItem VehicleTool) 
			{
				VehicleTool.Durability = VehicleTool.OriginalMaxDurability;
				VehicleTool.IntegrityAmount = VehicleTool.OriginalMaxDurability;
				player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} repaired to "+VehicleTool.Durability +" durability!"));									  
			}
			// If not a tool Return and do nothing
            if (selectedItem is not ToolItem tool) return string.Empty;
			// Below repairs tools 
			tool.Durability = tool.OriginalMaxDurability;
			tool.IntegrityAmount = tool.OriginalMaxDurability;
			player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} repaired to "+tool.Durability +" durability!"));									  
            return base.OnUsed(player, itemStack);
        }
    }


// *************************************************************************************
// ********** Repairs tools with Whetstones                                   **********
// *************************************************************************************
    public abstract class Whetstones : Item
    {
        public  const float CraftTime = 0.5f;
		private float       ItemDurability = 0f;
		private float       ItemMaxDurability = 0f;
		private float       ItemIntegrity = 0f;
		private bool        RepairTool = false;
        protected virtual Tag? RepairTag => null;
        protected virtual Type? RepairItem => null;

        public override string OnUsed(Player player, ItemStack itemStack)
        {
            var selectedItem = player.User.Inventory.Toolbar.SelectedItem;
            var selectedStack = itemStack.StackObject;
		//	var isBlacksmith = player.User.Skillset.GetSkill(typeof(BlacksmithSkill));
        //  var BlacksmithLevel = player.User.Skillset.GetSkill(typeof(BlacksmithSkill)).Level;
//                 Thse lines are from the AxeItem.cs file
//                    var user = player.User;
//                    var isHunter = user.Skillset.HasSkill(typeof(HuntingSkill));
//                    var huntingLevel = user.Skillset.GetSkill(typeof(HuntingSkill)).Level;
//                    animal.Damage(1.5f + (huntingLevel == 0 ? 0 : huntingLevel / 2));
			if (selectedItem == null)
            {
                player.Error(Localizer.DoStr($"Missing repair target. Please select a tool on the toolbar"));
                return string.Empty;
            }
			// Check to see if item is repairable by the whetstone selected 
			// string selectedName = selectedItem.DisplayName;  // Item being repaired  Testing 
			string selectedName = selectedItem.Name;  // Item being repaired
            string selectedStackName = selectedStack.DisplayName;    // Item used to repair tool
			RepairTool = false;
            switch (selectedItem)
			{
				case ToolItem toolItem:
				{
					ItemDurability    = toolItem.Durability;
					ItemMaxDurability = toolItem.OriginalMaxDurability;
					ItemIntegrity     = toolItem.IntegrityAmount;
					switch (selectedStackName)
					{
						case "Modern Gas Whetstones":   // Used to repair tractor attachments
							if (selectedName.Contains("Chainsaw"))
							{
								player.InfoBox(Localizer.DoStr($"Using {selectedStack.DisplayName} to repair {selectedItem.DisplayName}"));
								RepairTool = true;
							}
							else if (selectedName.Contains("Modern Rock Drill"))  
							{
								player.InfoBox(Localizer.DoStr($"Using {selectedStack.DisplayName} to repair {selectedItem.DisplayName}"));
								RepairTool = true;
							}
							break;
						case "Wood Whetstones":  
							if (selectedName.Contains("Wood"))  //  Used to repair Wood Tools 
							{
								player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} & {selectedStack.DisplayName} is made of wood"));
								RepairTool = true;
							}
							break;
						case "Stone Whetstones":
							if (selectedName.Contains("Stone"))
							{
								player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} & {selectedStack.DisplayName} is made of Stone"));
								RepairTool = true;
							}
							break;
						case "Iron Whetstones":
							if (selectedName.Contains("Iron"))
							{
								player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} & {selectedStack.DisplayName} is made of Iron"));
								RepairTool = true;
							}
							else if (selectedName.Contains("Recurve"))
							{
								player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} & {selectedStack.DisplayName} is made of Iron"));
								RepairTool = true;
							}
							break;
						case "Steel Whetstones":
							if (selectedName.Contains("Steel"))
							{
								player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} & {selectedStack.DisplayName} made of Steal"));
								RepairTool = true;
							}
							else if (selectedName.Contains("Modern") && !selectedName.Contains("Modern Rock Drill"))
							{
								player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} & {selectedStack.DisplayName} made of Steal"));
								RepairTool = true;
							}
							else if (selectedName.Contains("Norse"))
							{
								player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} & {selectedStack.DisplayName} made of Steal"));
								RepairTool = true;
							}
							else if (selectedName.Contains("Composite"))
							{
								player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} & {selectedStack.DisplayName} made of Steal"));
								RepairTool = true;
							}
							break;
					}
					break;
				}
				case VehicleToolItem vehicleToolItem:
				{
					ItemDurability    = vehicleToolItem.Durability;
					ItemMaxDurability = vehicleToolItem.OriginalMaxDurability;
					ItemIntegrity     = vehicleToolItem.IntegrityAmount;
					switch (selectedStackName)
					{
						case "Tractor Whetstones":
							if (selectedName.Contains("Harvester"))       {RepairTool = true;}
							else if (selectedName.Contains("Sower"))      {RepairTool = true;} 
							else if (selectedName.Contains("Plow"))       {RepairTool = true;}
							else if (selectedName.Contains("Scoop"))      {RepairTool = true;}
							else if (selectedName.Contains("Tree Cutter")) {RepairTool = true;}
						player.InfoBox(Localizer.DoStr($"Repair {selectedName} using {selectedStackName}"));
						break;
					}
					break;
				}
				default:
				{
					player.Error(Localizer.DoStr($"{selectedItem.DisplayName} cannot be repaired with {selectedStack.DisplayName} #1"));
					return base.OnUsed(player, itemStack);
				}
			}
			// validate to make sure tool can be repaired by whetstone if not Return
			if (RepairTool == false)
			{
				player.Error(Localizer.DoStr($"{selectedItem.DisplayName} cannot be repaired with {selectedStack.DisplayName} #2"));
				return string.Empty;
			}
			var integrityLossPoints = Math.Max(0f, Math.Min(100f, WhetstonesConfig.Settings.IntegrityLossPoints));
			var originalMaximum = Math.Max(0f, ItemMaxDurability);
			var currentIntegrity = Math.Max(0f, Math.Min(originalMaximum, ItemIntegrity));
			var integrityLossAmount = originalMaximum * (integrityLossPoints / 100f);
			var remainingIntegrity = Math.Max(0f, currentIntegrity - integrityLossAmount);
			var repairedDurability = remainingIntegrity;
			var remainingIntegrityPercent = originalMaximum > 0f
				? remainingIntegrity / originalMaximum * 100f
				: 0f;
            if (selectedItem is ToolItem fixTool)
			{
				fixTool.IntegrityAmount = remainingIntegrity;
				fixTool.Durability = repairedDurability;  
			}
            if (selectedItem is VehicleToolItem fixVehicle)
			{
				fixVehicle.IntegrityAmount = remainingIntegrity;
				fixVehicle.Durability = repairedDurability;
			}
			itemStack.TryModifyStack(player.User, increment: -1, test: null, () =>
			player.InfoBox(Localizer.DoStr($"The {selectedStack.DisplayName} has been used and repaired your {selectedItem.DisplayName}")));
			player.InfoBox(Localizer.DoStr($"{selectedItem.DisplayName} repaired to full durability. Integrity reduced by {integrityLossPoints} points; {remainingIntegrityPercent:0.#}% integrity remains."));
            return base.OnUsed(player, itemStack);
        }
    }


// *************************************************************************************
    [Serialized]
    [LocDisplayName("Wood Whetstones")]
    [Weight(500)]
    [MaxStackSize(50)]
    [Currency]
    [Category("Tool")]
    [Tag("Tool")]
    [Ecopedia("Items", "Tools", createAsSubPage: true)]
    [LocDescription("Right Click to repair Wooden hand tools")]
    public class WoodWhetstoneItem : Whetstones
    {
        protected override Tag RepairTag => TagManager.Tag("Wood");
        public LocString DisplayDescription => Localizer.DoStr("Right Click to repair wooden tools");
    }

// *************************************************************************************
   [Serialized]
    [LocDisplayName("Stone Whetstones")]
    [Weight(500)]
    [MaxStackSize(50)]
    [Currency]
    [Category("Tool")]
    [Tag("Tool")]
    [Ecopedia("Items", "Tools", createAsSubPage: true)]
    [LocDescription("Right Click to repair Stone hand tools")]
    public class StoneWhetstoneItem : Whetstones
    {
        protected override Tag RepairTag => TagManager.Tag("Rock");
        public LocString DisplayDescription => Localizer.DoStr("Right Click to repair stone tools");
    }

// *************************************************************************************
    [Serialized]
    [LocDisplayName("Iron Whetstones")]
    [Weight(500)]
    [MaxStackSize(50)]
    [Currency]
    [Category("Tool")]
    [Tag("Tool")]
    [Ecopedia("Items", "Tools", createAsSubPage: true)]
    [LocDescription("Right Click to repair Iron hand tools")]
    public class IronWhetstoneItem : Whetstones
    {
        protected override Type RepairItem => typeof(IronBarItem);
        public LocString DisplayDescription => Localizer.DoStr("Right Click to repair iron tools");
    }

// *************************************************************************************
    [Serialized]
    [LocDisplayName("Steel Whetstones")]
    [Weight(500)]
    [MaxStackSize(50)]
    [Currency]
    [Category("Tool")]
    [Tag("Tool")]
    [Ecopedia("Items", "Tools", createAsSubPage: true)]
    [LocDescription("Right Click to repair Modern or Steel hand tools")]
    public class SteelWhetstoneItem : Whetstones
    {
        protected override Type RepairItem => typeof(SteelBarItem);
        public LocString DisplayDescription => Localizer.DoStr("Right Click to repair Modern/Steel tools");
    }

// *************************************************************************************
    [Serialized]
    [LocDisplayName("Tractor Whetstones")]
    [Weight(500)]
    [MaxStackSize(50)]
    [Currency]
    [Category("Tool")]
    [Tag("Tool")]
    [Ecopedia("Items", "Tools", createAsSubPage: true)]
    [LocDescription("Right Click to repair Tractor Attachements")]
    public class TractorWhetstoneItem : Whetstones
    {
        protected override Type RepairItem => typeof(IronPlateItem);
        public LocString DisplayDescription => Localizer.DoStr("Right Click to repair tractor attachment");
    }

// *************************************************************************************
    [Serialized]
    [LocDisplayName("Modern Gas Whetstones")]
    [Weight(500)]
    [MaxStackSize(50)]
    [Currency]
    [Category("Tool")]
    [Tag("Tool")]
    [Ecopedia("Items", "Tools", createAsSubPage: true)]
    [LocDescription("Right Click to repair modern gasoline powered tools, Chainsaw & Modern Rockdrill")]
    public class GasolineWhetstoneItem : Whetstones
    {
        protected override Type RepairItem => typeof(GasolineItem);
        public LocString DisplayDescription => Localizer.DoStr("Right Click to repair modern gasoline powered tools");
    }


// *************************************************************************************
// **********  Crafting Recipes for Wood Whetstones ************************************
// *************************************************************************************
    [Ecopedia("Items", "Tools", subPageName: "Wood Whetstones Item")]
    public partial class WoodWhetstonesRecipe : RecipeFamily
    {
        public static int InferRepairCostBase(RepairableItem item)
        {
            // Change this number for the amount needed to craft all whetstones 0.25 = 1/4, 0.5 = 1/2, 0.75 = 3/4, 1 = same as repair bench
			return (int)Math.Ceiling(item.FullRepairAmount * 0.25);
        }
        public WoodWhetstonesRecipe()
        {
            var settings = WhetstonesConfig.Settings;
            var skillType = WhetstonesConfig.SkillType(settings.WoodCraftSkill, typeof(CarpentrySkill));
            var recipe = new Recipe();
            recipe.Init(
                name: "Wood Whetstones",
                displayName: Localizer.DoStr("Wood Whetstones"),
                ingredients: new List<IngredientElement>
                {
                    new("Wood",
                        InferRepairCostBase(new WoodenShovelItem()),
                        skillType),
                        // typeof(BasicEngineeringLavishResourcesTalent)),
                },
                new List<CraftingElement>
                {
                    new CraftingElement<WoodWhetstoneItem>(),
                });
            Recipes = new List<Recipe> { recipe };
            ExperienceOnCraft = 1;
            LaborInCalories = CreateLaborInCaloriesValue(100, skillType);
            CraftMinutes = CreateCraftTimeValue(
                typeof(WoodWhetstonesRecipe),
                Whetstones.CraftTime,
                skillType);
            // typeof(BasicEngineeringFocusedSpeedTalent),
            // typeof(BasicEngineeringParallelSpeedTalent));
            Initialize(Localizer.DoStr("Wood Whetstones"), typeof(WoodWhetstonesRecipe));
            RequiredSkills = WhetstonesConfig.CachedRequirement(skillType, settings.WoodCraftLevel);
            CraftingComponent.AddRecipe(typeof(ToolBenchObject), this);
        }
    }

// *************************************************************************************
// **********  Crafting Recipes for Stone Whetstones  **********************************
// *************************************************************************************
    [Ecopedia("Items", "Tools", subPageName: "Stone Whetstones Item")]
    public partial class StoneWhetstonesRecipe : RecipeFamily
    {
        public StoneWhetstonesRecipe()
        {
            var settings = WhetstonesConfig.Settings;
            var skillType = WhetstonesConfig.SkillType(settings.StoneCraftSkill, typeof(BasicEngineeringSkill));
            var recipe = new Recipe();
            recipe.Init(
                name: "Stone Whetstones",  //noloc
                displayName: Localizer.DoStr("Stone Whetstones"),
                ingredients: new List<IngredientElement>
                {
                    new("Rock",
                        WoodWhetstonesRecipe.InferRepairCostBase(new StonePickaxeItem()),
                        skillType),
                        // typeof(BasicEngineeringLavishResourcesTalent)),
                },
                new List<CraftingElement>
                {
                    new CraftingElement<StoneWhetstoneItem>()
                });
            Recipes = new List<Recipe> { recipe };
            ExperienceOnCraft = 1;
            LaborInCalories = CreateLaborInCaloriesValue(100, skillType);
            CraftMinutes = CreateCraftTimeValue(
                typeof(StoneWhetstonesRecipe),
                Whetstones.CraftTime,
                skillType);
                // typeof(BasicEngineeringFocusedSpeedTalent),
                // typeof(BasicEngineeringParallelSpeedTalent));
            Initialize(Localizer.DoStr("Stone Whetstones"), typeof(StoneWhetstonesRecipe));
            RequiredSkills = WhetstonesConfig.CachedRequirement(skillType, settings.StoneCraftLevel);
            CraftingComponent.AddRecipe(typeof(ToolBenchObject), this);
        }
    }

// *************************************************************************************
// **********  Crafting Recipes for Iron Whetstones ************************************
// *************************************************************************************
    [Ecopedia("Items", "Tools", subPageName: "Iron Whetstones Item")]
    public partial class IronWhetstonesRecipe : RecipeFamily
    {
        public IronWhetstonesRecipe()
        {
            var settings = WhetstonesConfig.Settings;
            var skillType = WhetstonesConfig.SkillType(settings.IronCraftSkill, typeof(BlacksmithSkill));
            var recipe = new Recipe();
            recipe.Init(
                name: "Iron Whetstones",
                displayName: Localizer.DoStr("Iron Whetstones"),
                ingredients: new List<IngredientElement>
                {
                    new(typeof(IronBarItem),
                        WoodWhetstonesRecipe.InferRepairCostBase(new IronPickaxeItem()),
                        skillType),
                        // typeof(BlacksmithLavishResourcesTalent)),
                },
                new List<CraftingElement>
                {
                    new CraftingElement<IronWhetstoneItem>()
                });
            Recipes = new List<Recipe> { recipe };
            ExperienceOnCraft = 1;
            LaborInCalories = CreateLaborInCaloriesValue(100, skillType);
            CraftMinutes = CreateCraftTimeValue(
                typeof(IronWhetstonesRecipe),
                Whetstones.CraftTime,
                skillType);
                // typeof(BlacksmithFocusedSpeedTalent),
                // typeof(BlacksmithParallelSpeedTalent));
            Initialize(Localizer.DoStr("Iron Whetstones"), typeof(IronWhetstonesRecipe));
            RequiredSkills = WhetstonesConfig.CachedRequirement(skillType, settings.IronCraftLevel);
            CraftingComponent.AddRecipe(typeof(GrindstoneObject), this);
        }
    }

// *************************************************************************************
// **********  Crafting Recipes for Steel Whetstones ***********************************
// *************************************************************************************
    [Ecopedia("Items", "Tools", subPageName: "Steel Whetstones Item")]
    public partial class SteelWhetstonesRecipe : RecipeFamily
    {
        public SteelWhetstonesRecipe()
        {
            var settings = WhetstonesConfig.Settings;
            var skillType = WhetstonesConfig.SkillType(settings.SteelCraftSkill, typeof(BlacksmithSkill));
            var recipe = new Recipe();
            recipe.Init(
				name: "Steel Whetstones",
                displayName: Localizer.DoStr("Steel Whetstones"),
                ingredients: new List<IngredientElement>
                {
                    new(typeof(SteelBarItem),
                        WoodWhetstonesRecipe.InferRepairCostBase(new ModernPickaxeItem()),
                        skillType),
                        // typeof(BlacksmithLavishResourcesTalent)),
                },
                items: new List<CraftingElement>
                {
                    new CraftingElement<SteelWhetstoneItem>()
                });
            Recipes = new List<Recipe> { recipe };
            ExperienceOnCraft = 1;
            LaborInCalories = CreateLaborInCaloriesValue(200, skillType);
            CraftMinutes = CreateCraftTimeValue(
                typeof(SteelWhetstonesRecipe),
                Whetstones.CraftTime,
                skillType);
                // typeof(BlacksmithFocusedSpeedTalent),
                // typeof(BlacksmithParallelSpeedTalent));
            Initialize(Localizer.DoStr("Steel Whetstones"), typeof(SteelWhetstonesRecipe));
            RequiredSkills = WhetstonesConfig.CachedRequirement(skillType, settings.SteelCraftLevel);
            CraftingComponent.AddRecipe(typeof(PowerHammerObject), this);
        }
	}

// *************************************************************************************
// **********  Crafting Recipes for Tractor Whetstones ******************************
// *************************************************************************************
    [Ecopedia("Items", "Tools", subPageName: "Tractor Whetstones Item")]
    public class TractorWhetstonesRecipe : RecipeFamily
    {
        public TractorWhetstonesRecipe()
        {
            var settings = WhetstonesConfig.Settings;
            var skillType = WhetstonesConfig.SkillType(settings.TractorCraftSkill, typeof(MechanicsSkill));
            var recipe = new Recipe();
            recipe.Init(
                name: "Tractor Whetstones",
                displayName: Localizer.DoStr("Tractor Whetstones"),
                ingredients: new List<IngredientElement>
                {
                    new(typeof(IronPlateItem),
                        WoodWhetstonesRecipe.InferRepairCostBase(new SteamTractorHarvesterItem()),
                        skillType),
                        // typeof(MechanicsLavishResourcesTalent)),
                },
                new List<CraftingElement>
                {
                    new CraftingElement<TractorWhetstoneItem>()
                });
            Recipes = new List<Recipe> { recipe };
            ExperienceOnCraft = 5;
            LaborInCalories = CreateLaborInCaloriesValue(200, skillType);
            CraftMinutes = CreateCraftTimeValue(
                typeof(TractorWhetstonesRecipe),
                Whetstones.CraftTime,
                skillType);
                //typeof(MechanicsFocusedSpeedTalent),
                // typeof(MechanicsParallelSpeedTalent));
            Initialize(Localizer.DoStr("Tractor Whetstones"), typeof(TractorWhetstonesRecipe));
            RequiredSkills = WhetstonesConfig.CachedRequirement(skillType, settings.TractorCraftLevel);
            CraftingComponent.AddRecipe(typeof(AssemblyLineObject), this);
        }
    }

// *************************************************************************************
// **********  Crafting Recipes for Modern Gas Whetstones ******************************
// *************************************************************************************
    [Ecopedia("Items", "Tools", subPageName: "Modern Gasoline Whetstones Item")]
    public class GasolineWhetstonesRecipe : RecipeFamily
    {
        public GasolineWhetstonesRecipe()
        {
            var settings = WhetstonesConfig.Settings;
            var skillType = WhetstonesConfig.SkillType(settings.GasolineCraftSkill, typeof(OilDrillingSkill));
            var recipe = new Recipe();
            recipe.Init(
                name: "Modern Gasoline Whetstones",
                displayName: Localizer.DoStr("Modern Gasoline Whetstones"),
                ingredients: new List<IngredientElement>
                {
                    // new(typeof(SteelBarItem),
                    new(typeof(GasolineItem),
                        WoodWhetstonesRecipe.InferRepairCostBase(new ChainsawItem()),
                        skillType),
                        // typeof(BlacksmithLavishResourcesTalent)),
                },
                new List<CraftingElement>
                {
                    new CraftingElement<GasolineWhetstoneItem>()
                });
            Recipes = new List<Recipe> { recipe };
            ExperienceOnCraft = 5;
            LaborInCalories = CreateLaborInCaloriesValue(200, skillType);
            CraftMinutes = CreateCraftTimeValue(
                typeof(GasolineWhetstonesRecipe),
                Whetstones.CraftTime,
                skillType);
				// typeof(BlacksmithFocusedSpeedTalent),
				// typeof(BlacksmithParallelSpeedTalent));
            Initialize(Localizer.DoStr("Modern Gasoline Whetstones"), typeof(GasolineWhetstonesRecipe));
            RequiredSkills = WhetstonesConfig.CachedRequirement(skillType, settings.GasolineCraftLevel);
            CraftingComponent.AddRecipe(typeof(PowerHammerObject), this);
        }
    }

// ************************************************************************
// **********  Mod Registration.                             **************
// **********  Please don't make changes to this section.    **************
// ************************************************************************
	public class WhetstonesMod : IModInit
	{
		public static ModRegistration Register() => new() 
		{ 
			ModName = "Whetstones",
			ModDescription = "Mod includes 6 Whetstones to repair your tools and tractor attachments.",
			ModDisplayName = "Whetstone Repair",
		};
	}
}
