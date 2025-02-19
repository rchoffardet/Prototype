using Godot;
using System;
using System.Diagnostics;
using C7GameData;
using C7GameData.Save;
using System.Collections.Generic;
using Serilog;

// The popup for selecting which other civilization to contact.
public partial class DiplomacySelection : Popup {
	private Player player;
	private List<Player> allPlayers;

	public DiplomacySelection(Player player, List<Player> allPlayers) {
		alignment = BoxContainer.AlignmentMode.Center;
		margins = new Margins(top: 200);
		this.player = player;
		this.allPlayers = allPlayers;
	}

	public override void _Ready() {
		base._Ready();

		int width = 530;
		int height = 115 + 25 * player.playerRelationships.Keys.Count;
		AddTexture(width, height);
		AddBackground(width, height);
		AddHeader("Pick the civilization...", 10);

		int vOffset = 65;
		foreach (KeyValuePair<ID, PlayerRelationship> kvp in player.playerRelationships) {
			string status = kvp.Value.atWar ? "War" : "Peace";
			AddButton($"{allPlayers.Find(x => x.id == kvp.Key).civilization.noun} (at {status})", vOffset, () => { });
			vOffset += 25;
		}

		//Cancel/confirm buttons.  Note the X button is thinner than the O button.
		// TODO: Push this shared logic up into Popup.cs
		ImageTexture circleTexture= Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 1, 1, 19, 19);
		ImageTexture xTexture = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 21, 1, 15, 19);
		ImageTexture circleHover = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 37, 1, 19, 19);
		ImageTexture xHover = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 57, 1, 15, 19);
		ImageTexture circlePressed = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 73, 1, 19, 19);
		ImageTexture xPressed = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 93, 1, 15, 19);
		TextureButton confirmButton = new TextureButton();
		confirmButton.TextureNormal = circleTexture;
		confirmButton.TextureHover = circleHover;
		confirmButton.TexturePressed = circlePressed;
		confirmButton.SetPosition(new Vector2(width - 55, height - 47));
		AddChild(confirmButton);
		TextureButton cancelButton = new TextureButton();
		cancelButton.TextureNormal = xTexture;
		cancelButton.TextureHover = xHover;
		cancelButton.TexturePressed = xPressed;
		cancelButton.SetPosition(new Vector2(width - 30, height - 47));
		AddChild(cancelButton);

		// TODO: Do something when the confirm button is pressed.
		cancelButton.Pressed += GetParent<PopupOverlay>().OnHidePopup;
	}
}
