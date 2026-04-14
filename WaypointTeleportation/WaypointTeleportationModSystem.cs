using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace WaypointTeleportation;

public class WaypointTeleportationModSystem : ModSystem {
	public string HarmonyId => Mod.Info.ModID;
	public Harmony HarmonyInstance => field ??= new(HarmonyId);
	public override void StartClientSide(ICoreClientAPI api) { HarmonyInstance.PatchAll(); }
	public override void Dispose() { HarmonyInstance.UnpatchAll(); }
}