using Godot;

namespace Teoa.UI;

/// <summary>
/// Script tombol yang modular.
///
/// Anda cukup meng-attach script ini ke TextureButton, lalu mengisi properti di Inspector:
/// - Pindah scene (TargetScenePath)
/// - Keluar game (QuitGame)
/// - Munculkan popup dari node yang sudah ada (UiTargetPath)
/// - Munculkan popup dari scene lain (PopupScene)
///
/// Catatan: prioritas aksi saat tombol diklik:
/// 1) PopupScene (jika diisi) -> 2) UiTargetPath (jika diisi) -> 3) QuitGame -> 4) TargetScenePath
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

	public enum PopupHideMode
	{
		/// <summary>
		/// Hanya menyembunyikan popup (Visible=false). Instance popup tetap ada di scene tree.
		/// </summary>
		Hide = 0,
		/// <summary>
		/// Menghapus popup dari scene tree (QueueFree). Saat Show berikutnya, popup akan dibuat lagi.
		/// </summary>
		Destroy = 1,
	}

	/// <summary>
	/// Target UI (node yang SUDAH ADA di scene yang sama) yang akan di-Show/Hide/Toggle.
	/// Contoh: Panel, Control, child dari CanvasLayer, dll.
	///
	/// Jika diisi dan UiTargetAction != None, maka saat tombol diklik script akan menjalankan
	/// aksi tersebut pada node ini.
	/// </summary>
	[Export(PropertyHint.NodePathValidTypes, "CanvasItem")] public NodePath UiTargetPath { get; set; }

	/// <summary>
	/// Aksi untuk UiTargetPath saat tombol ditekan.
	/// - Show: Visible = true
	/// - Hide: Visible = false
	/// - Toggle: Visible dibalik (true/false)
	/// </summary>
	[Export] public UiAction UiTargetAction { get; set; } = UiAction.None;

	/// <summary>
	/// Jika true dan UI yang ditampilkan adalah Control, maka UI akan mengambil fokus (GrabFocus)
	/// ketika dimunculkan. Berguna untuk navigasi keyboard/gamepad.
	/// </summary>
	[Export] public bool GrabFocusOnShow { get; set; } = true;

	/// <summary>
	/// Scene popup UI (scene LAIN) yang akan dibuat (instantiate) ketika tombol ditekan.
	/// Root node dari scene popup ini sebaiknya bertipe Control.
	///
	/// Jika PopupScene diisi dan PopupAction != None, maka tombol akan menjalankan aksi popup
	/// (Show/Hide/Toggle) menggunakan scene ini.
	/// </summary>
	[Export] public PackedScene PopupScene { get; set; }

	/// <summary>
	/// Parent node tempat popup akan ditaruh (AddChild).
	/// Jika kosong, popup akan ditambahkan ke root scene yang sedang aktif.
	///
	/// Rekomendasi: arahkan ke CanvasLayer atau Control khusus UI.
	/// </summary>
	[Export(PropertyHint.NodePathValidTypes, "Node")] public NodePath PopupParentPath { get; set; }

	/// <summary>
	/// Aksi untuk PopupScene saat tombol ditekan.
	/// - Show: buat instance jika belum ada, lalu tampilkan
	/// - Hide: sembunyikan/hapus instance
	/// - Toggle: show kalau belum tampil, hide kalau sedang tampil
	/// </summary>
	[Export] public UiAction PopupAction { get; set; } = UiAction.None;

	/// <summary>
	/// Cara menutup popup ketika PopupAction = Hide (atau Toggle sedang mematikan popup).
	/// - Hide: Visible=false (instance tetap ada)
	/// - Destroy: QueueFree (instance dihapus dan akan dibuat ulang saat Show)
	/// </summary>
	[Export] public PopupHideMode PopupHideBehavior { get; set; } = PopupHideMode.Hide;

	/// <summary>
	/// Nama instance popup di bawah PopupParent.
	///
	/// Kalau Anda isi ini, script akan mencari popup yang sudah ada dengan nama ini
	/// supaya tidak membuat popup berkali-kali.
	///
	/// Disarankan DIISI, misalnya: "CreditsPopup".
	/// </summary>
	[Export] public string PopupInstanceName { get; set; } = "";

	/// <summary>
	/// Path scene tujuan untuk pindah scene.
	/// Contoh: res://scenes/ui/MainMenu.tscn
	///
	/// Harus menggunakan format res:// (bukan path Windows C:\\...).
	/// </summary>
	[Export(PropertyHint.File, "*.tscn,*.scn")] public string TargetScenePath { get; set; } = "";

	/// <summary>
	/// (Opsional) Node yang akan dijalankan animasinya / efek transisinya sebelum pindah scene.
	/// Contoh: node "PrisonBars" di main menu.
	///
	/// Jika diisi, script akan memanggil method (PreChangeMethod) pada node ini,
	/// lalu setelah itu baru pindah scene.
	/// </summary>
	[Export(PropertyHint.NodePathValidTypes, "Node")] public NodePath PreChangeNodePath { get; set; }

	/// <summary>
	/// Nama method yang akan dipanggil pada node PreChangeNodePath.
	/// Default: "PlayCloseAndWait".
	///
	/// Catatan:
	/// - Kalau method-nya mengembalikan Tween, script akan menunggu hingga Tween selesai.
	/// - Kalau method void, script akan lanjut pindah scene tanpa menunggu.
	/// </summary>
	[Export] public string PreChangeMethod { get; set; } = "PlayCloseAndWait";

	/// <summary>
	/// Jika true, tombol akan keluar dari game (GetTree().Quit()).
	/// Cocok untuk tombol Exit.
	/// </summary>
	[Export] public bool QuitGame { get; set; } = false;

	/// <summary>
	/// Jika true, tombol akan dinonaktifkan sementara saat diklik untuk mencegah double click.
	/// Jika aksinya hanya popup, tombol akan diaktifkan lagi setelah aksi selesai.
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

	private Node GetPopupParentOrDefault()
	{
		if (PopupParentPath != null && !PopupParentPath.IsEmpty)
		{
			var parent = GetNodeOrNull<Node>(PopupParentPath);
			if (parent != null)
				return parent;
			GD.PushWarning($"{Name}: PopupParentPath '{PopupParentPath}' not found. Falling back to current scene root.");
		}

		return GetTree().CurrentScene ?? GetTree().Root;
	}

	private Control FindPopupInstance(Node parent)
	{
		if (parent == null)
			return null;

		if (!string.IsNullOrWhiteSpace(PopupInstanceName))
			return parent.GetNodeOrNull<Control>(PopupInstanceName);

		// If no name is provided, we don't try to find an existing instance.
		return null;
	}

	private static UiAction ResolvePopupAction(UiAction configuredAction, Control existing)
	{
		if (configuredAction != UiAction.Toggle)
			return configuredAction;

		bool isVisible = existing != null && existing.Visible && existing.IsInsideTree();
		return isVisible ? UiAction.Hide : UiAction.Show;
	}

	private Control InstantiatePopupControl(Node parent)
	{
		var inst = PopupScene.Instantiate();
		var popup = inst as Control;
		if (popup == null)
		{
			GD.PushError($"{Name}: PopupScene root must be a Control to be used as UI popup.");
			inst.QueueFree();
			return null;
		}

		if (!string.IsNullOrWhiteSpace(PopupInstanceName))
			popup.Name = PopupInstanceName;

		parent.AddChild(popup);
		return popup;
	}

	private void ShowPopup(Control popup)
	{
		popup.Visible = true;
		if (GrabFocusOnShow)
			popup.GrabFocus();
	}

	private void HideOrDestroyPopup(Control popup)
	{
		if (PopupHideBehavior == PopupHideMode.Destroy)
			popup.QueueFree();
		else
			popup.Visible = false;
	}

	private bool TryRunPopupSceneAction()
	{
		if (PopupAction == UiAction.None || PopupScene == null)
			return false;

		var parent = GetPopupParentOrDefault();
		var existing = FindPopupInstance(parent);
		UiAction action = ResolvePopupAction(PopupAction, existing);

		if (action == UiAction.Show)
		{
			var popup = existing ?? InstantiatePopupControl(parent);
			if (popup != null)
				ShowPopup(popup);
			return true;
		}

		if (action == UiAction.Hide)
		{
			if (existing != null)
				HideOrDestroyPopup(existing);
			return true;
		}

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

		// Preferred path: strongly-typed call (avoids Variant return issues with node.Call()).
		if (node is Teoa.UI.MainMenu.PrisonBars bars && PreChangeMethod == "PlayCloseAndWait")
		{
			var tween = bars.PlayCloseAndWait();
			if (tween != null)
				await ToSignal(tween, Tween.SignalName.Finished);
			return;
		}

		if (!node.HasMethod(PreChangeMethod))
		{
			GD.PushWarning($"{Name}: PreChange node '{node.Name}' doesn't have method '{PreChangeMethod}'.");
			return;
		}

		// Fallback for generic nodes: call method, but don't rely on return type.
		// (Many Godot C# calls return Variant that isn't safely castable here.)
		node.Call(PreChangeMethod);
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

		// 0) Popup instantiated from another scene (highest priority, if configured).
		if (TryRunPopupSceneAction())
		{
			if (DisableOnPress)
				Disabled = false;
			return;
		}

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
