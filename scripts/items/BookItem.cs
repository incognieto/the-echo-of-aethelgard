using Godot;
using System;

// Book behavior untuk item
public class BookUsable : IUsableItem
{
	private string _bookTitle;

	public BookUsable(string title)
	{
		_bookTitle = title;
	}

	public void Use(Player player)
	{
		player.ShowBook(_bookTitle, "", "");
	}

	public string GetUseText()
	{
		return "Read";
	}
}

// PickableItem khusus untuk buku
public partial class BookItem : PickableItem
{
	[Export] public string BookTitle = "Mysterious Book";
	[Export(PropertyHint.MultilineText)] public string BookContent = "This book is empty...";

	public override void _Ready()
	{
		// Set sebagai usable item
		base._Ready();
		
		// Check if this is the ancient book (key item)
		bool isKeyItem = ItemId == "ancient_book";
		
		// Create ItemData dengan usable flag
		var bookData = new ItemData(ItemId, ItemName, 1, true, isKeyItem); // Max stack 1, usable = true, keyItem if ancient book
		bookData.Description = isKeyItem ? "An ancient mystical book - Contains secrets of alchemy" : "A book that can be read";
		
		// Set book behavior
		bookData.UsableBehavior = new BookUsable(BookTitle);
		
		// Override the default item data
		_itemData = bookData;
		
		GD.Print($"BookItem ready: {BookTitle} (Key Item: {isKeyItem})");
	}
}
