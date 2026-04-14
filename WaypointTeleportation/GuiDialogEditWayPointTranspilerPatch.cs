using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace WaypointTeleportation;

[HarmonyPatch(typeof(GuiDialogEditWayPoint), "ComposeDialog")]
public static class GuiDialogEditWayPointTranspilerPatch {

	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
		var addTopElementsMethod = AccessTools.Method(typeof(GuiDialogEditWayPointTranspilerPatch), nameof(AddTopElements));
		var injected = false;

		foreach (var instruction in instructions) {
			if (instruction.opcode == OpCodes.Ldc_R8 && instruction.operand is 28.0) {
				instruction.operand = 100.0;
			}

			yield return instruction;
			if (!injected &&
				(instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
				instruction.operand is MethodInfo { Name: "BeginChildElements" }) {
				yield return new(OpCodes.Ldarg_0);
				yield return new(OpCodes.Call, addTopElementsMethod);

				injected = true;
			}
		}
	}

	public static GuiComposer AddTopElements(GuiComposer composer, GuiDialogEditWayPoint dialog) {
		var trv = Traverse.Create(dialog);
		var waypoint = trv.Field("waypoint").GetValue<Waypoint>() ?? trv.Property("waypoint").GetValue<Waypoint>();
		var capi = trv.Field("capi").GetValue<ICoreClientAPI>() ?? trv.Property("capi").GetValue<ICoreClientAPI>();
		var textBounds = ElementBounds.Fixed(0, 28, 320, 25).WithAlignment(EnumDialogArea.CenterFixed);
		var btnBounds = ElementBounds.Fixed(0, 60, 320, 25).WithAlignment(EnumDialogArea.CenterFixed);

		var pos = waypoint.Position;
		var pos2 = pos.Clone();
		var y = pos2.Y;
		pos2.Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
		pos2.Y = y;

		var coordsText = $"X: {pos2.XInt} Y: {pos2.YInt} Z: {pos2.ZInt}";

		return composer
			.AddStaticText(coordsText, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), textBounds)
			.AddSmallButton(Lang.Get("waypointteleportation:Teleport"),
				() => {
					capi.SendChatMessage(
						$"/tp ={pos.X} ={pos.Y} ={pos.Z}");
					dialog.TryClose();
					return true;
				},
				btnBounds,
				EnumButtonStyle.Normal,
				"tpBtn");
	}
}