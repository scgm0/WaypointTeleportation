using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WaypointTeleportation;

[HarmonyPatch(typeof(GuiDialogAddWayPoint), "ComposeDialog")]
public static class GuiDialogAddWayPointTranspilerPatch {

	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
		var addTopElementsMethod = AccessTools.Method(typeof(GuiDialogAddWayPointTranspilerPatch), nameof(AddTopElements));
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

	public static GuiComposer AddTopElements(GuiComposer composer, GuiDialogAddWayPoint dialog) {
		var trv = Traverse.Create(dialog);
		var pos = trv.Field("WorldPos").GetValue<Vec3d>() ?? trv.Property("WorldPos").GetValue<Vec3d>();
		var capi = trv.Field("capi").GetValue<ICoreClientAPI>() ?? trv.Property("capi").GetValue<ICoreClientAPI>();
		var textBounds = ElementBounds.Fixed(0, 28, 320, 25).WithAlignment(EnumDialogArea.CenterFixed);
		var btnBounds = ElementBounds.Fixed(0, 60, 320, 25).WithAlignment(EnumDialogArea.CenterFixed);

		var pos2 = pos.Clone();
		var y = pos2.Y;
		pos2.Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
		pos2.Y = y;

		var mapChunk = capi.World.BlockAccessor.GetMapChunkAtBlockPos(pos.AsBlockPos);
		string coordsText;
		if (mapChunk == null || !capi.World.BlockAccessor.IsValidPos(pos.AsBlockPos)) {
			pos.Y = capi.World.MapSizeY / 2.0;
			coordsText = $"X: {pos2.XInt} Y: ?({pos.YInt}) Z: {pos2.ZInt}";
		} else {
			coordsText = $"X: {pos2.XInt} Y: {pos2.YInt} Z: {pos2.ZInt}";
		}

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