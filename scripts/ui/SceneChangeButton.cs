using Godot;

namespace Teoa.UI;

/// <summary>
/// Modular button script: set the target scene in the Inspector, and clicking the button changes scene.
/// Attach this to any TextureButton.
/// </summary>
public partial class SceneChangeButton : TextureButton
{
	public enum UiAction
	{
		None = 0,
		Show = 1,
		Hide = 2,
		Toggle = 3,
	}

	/// <summary>
	/// Optional UI target to show/hide/toggle (Panel, Control, CanvasLayer child, etc.).
	/// If this is set and UiTargetAction != None, it will run that action on click.
	/// </summary>
	[Export(PropertyHint.NodePathValidTypes, "CanvasItem")] public NodePath UiTargetPath { get; set; }

	/// <summary>
	/// What to do to UiTargetPath when the button is pressed.
	/// </summary>
	[Export] public UiAction UiTargetAction { get; set; } = UiAction.None;

	/// <summary>
	/// Optional: if true and the UI is a Control, it will grab focus when shown.
	/// </summary>
	[Export] public bool GrabFocusOnShow { get; set; } = true;

	/// <summary>
	/// Scene file path to load, e.g. res://scenes/ui/MainMenu.tscn
	/// </summary>
	[Export(PropertyHint.File, "*.tscn,*.scn")] public string TargetScenePath { get; set; } = "";

	/// <summary>
	/// Optional: a node to animate before changing scenes (e.g. PrisonBars).
	/// If set, we'll call a method on it and await completion before switching scenes.
	/// </summary>
	[Export(PropertyHint.NodePathValidTypes, "Node")] public NodePath PreChangeNodePath { get; set; }

	/// <summary>
	/// Method name to call on PreChangeNodePath. It should return a Signal/Task,
	/// or it can be void (we'll proceed immediately).
	/// Default: "PlayCloseAndWait".
	/// </summary>
	[Export] public string PreChangeMethod { get; set; } = "PlayCloseAndWait";

	/// <summary>
	/// Optional: if true, quits the game instead of changing scene.
	/// Useful for an Exit button without a separate script.
	/// </summary>
	[Export] public bool QuitGame { get; set; } = false;

	/// <summary>
	/// Optional: disable the button after click to prevent double clicks.
	/// </summary>
	[Export] public bool DisableOnPress { get; set; } = true;

	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	public override void _ExitTree()
	{
		Pressed -= OnPressed;
	}

	private bool TryRunUiTargetAction()
	{
		if (UiTargetAction == UiAction.None || UiTargetPath == null || UiTargetPath.IsEmpty)
			return false;

		var ui = GetNodeOrNull<CanvasItem>(UiTargetPath);
		if (ui == null)
		{
			GD.PushWarning($"{Name}: UiTargetPath '{UiTargetPath}' not found.");
			return true;
		}

		switch (UiTargetAction)
		{
			case UiAction.Show:
				ui.Visible = true;
				break;
			case UiAction.Hide:
				ui.Visible = false;
				break;
			case UiAction.Toggle:
				ui.Visible = !ui.Visible;
				break;
		}

		if (GrabFocusOnShow && ui.Visible && ui is Control control)
			control.GrabFocus();

		return true;
	}

	private async System.Threading.Tasks.Task RunPreChangeAsync()
	{
		if (PreChangeNodePath == null || PreChangeNodePath.IsEmpty || string.IsNullOrWhiteSpace(PreChangeMethod))
			return;

		var node = GetNodeOrNull<Node>(PreChangeNodePath);
		if (node == null)
		{
			GD.PushWarning($"{Name}: PreChangeNodePath '{PreChangeNodePath}' not found.");
			return;
		}

		if (!node.HasMethod(PreChangeMethod))
		{
			GD.PushWarning($"{Name}: PreChange node '{node.Name}' doesn't have method '{PreChangeMethod}'.");
			return;
		}

		var ret = node.Call(PreChangeMethod);
		// Supported return types:
		// - Tween: wait for 'finished'
		// - AnimationPlayer: wait for 'animation_finished' (if returned)
		// - Task: await
		// - null/void: proceed immediately
		if (ret.VariantType == Variant.Type.Nil)
			return;

		// Godot returns Variant; for Objects we can extract as GodotObject.
		if (ret.VariantType == Variant.Type.Object)
		{
			var obj = ret.AsGodotObject();
			if (obj is Tween tween)
				await ToSignal(tween, Tween.SignalName.Finished);
			else if (obj is AnimationPlayer anim)
				await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
		}
	}

	private void OnPressed()
	{
		// Run async flow without blocking the signal.
		_ = OnPressedAsync();
	}

	private async System.Threading.Tasks.Task OnPressedAsync()
	{
		if (DisableOnPress)
			Disabled = true;

		// 1) UI action (popup) takes priority if configured.
		if (TryRunUiTargetAction())
		{
			if (DisableOnPress)
				Disabled = false;
			return;
		}

		if (QuitGame)
		{
			GetTree().Quit();
			return;
		}

		if (string.IsNullOrWhiteSpace(TargetScenePath))
		{
			GD.PushWarning($"{Name}: TargetScenePath is empty. Set it in the Inspector.");
			if (DisableOnPress)
				Disabled = false;
			return;
		}

		await RunPreChangeAsync();

		var err = GetTree().ChangeSceneToFile(TargetScenePath);
		if (err != Error.Ok)
		{
			GD.PushError($"{Name}: Failed to change scene to '{TargetScenePath}'. Error: {err}");
			if (DisableOnPress)
				Disabled = false;
		}
	}
}
