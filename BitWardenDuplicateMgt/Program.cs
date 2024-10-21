using System.Text.Json;
using BitWardenDuplicateMgt;

#if DEBUG
args = new string[1] { "Add Path" };
#endif

if (args.Length != 1)
{
  Console.WriteLine("Usage: dotnet run <path to bitwarden export json>");
  return;
}

var filePath = args[0];
var newFilePath = filePath.Insert(filePath.LastIndexOf('.'), "_dedup");

await using var fileStream = new FileStream(filePath, FileMode.Open);
var json = await JsonSerializer.DeserializeAsync<BitWardenExport>(fileStream, new JsonSerializerOptions
{
  PropertyNameCaseInsensitive = true
});
if (json is null)
{
  Console.WriteLine("Could not deserialize json");
  return;
}

var visitedGroupNames = new HashSet<string>();
var groupNameToId = new Dictionary<string, string>();
var idToNewId = new Dictionary<string, string>();
var idToFolder = new Dictionary<string, BitwardenFolder>();

foreach (var (id, name) in json.Folders)
{
  if (visitedGroupNames.Contains(name))
  {
    idToNewId.Add(id, groupNameToId[name]);
  }
  else
  {
    visitedGroupNames.Add(name);
    groupNameToId.Add(name, id);
    idToNewId.Add(id, id);
    idToFolder.Add(id, new BitwardenFolder
    {
      Id = id,
      Name = name
    });
  }
}

var itemHashToNewItem = new Dictionary<string, BitwardenItem>();

foreach (var item in json.Items)
{
  var itemHash = ItemHash(item);
  if (itemHashToNewItem.TryGetValue(itemHash, out var newItem))
  {
    Merge(newItem, item);
    continue;
  }

  newItem = new BitwardenItem
  {
    Id = Guid.NewGuid().ToString(),
    OrganizationId = item.OrganizationId,
    FolderId = item.FolderId is null ? null : idToNewId[item.FolderId],
    Type = item.Type,
    Reprompt = item.Reprompt,
    Name = item.Name,
    Notes = item.Notes,
    Favorite = item.Favorite,
    Login = item.Login,
    Card = item.Card,
    SecureNote = item.SecureNote,
    CollectionIds = item.CollectionIds
  };

  itemHashToNewItem.Add(itemHash, newItem);
}


void Merge(BitwardenItem source, BitwardenItem target)
{
  if (target.Login?.Uris is not null && source.Type == 1)
  {
    source.Login ??= new BitwardenLogin();
    source.Login.Uris ??= new List<BitwardenUri>();
    foreach (var uri in target.Login.Uris.Where(uri => !source.Login.Uris.Contains(uri)))
    {
      source.Login.Uris.Add(uri);
    }
  }

  if (target.Favorite)
  {
    source.Favorite = true;
  }

  if (!string.IsNullOrEmpty(target.Notes))
  {
    if (!string.IsNullOrEmpty(source.Notes) && !source.Notes.Contains(target.Notes))
    {
      source.Notes += $"\n\n{target.Notes}";
    }
    else
    {
      source.Notes = target.Notes;
    }
  }
  var srcFields = source.Fields?.ToList() ?? new List<BitwardenField>() ;
  var trgFields = target.Fields?.ToList() ?? new List<BitwardenField>();
  srcFields.AddRange(trgFields);
  srcFields.Add(new BitwardenField("OldPWD", target.Login?.Password, 1, null));

  source.Fields = srcFields.ToArray();
}

var newItems = itemHashToNewItem.Values.ToArray();
var options = new JsonSerializerOptions()
{
  Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
  WriteIndented = true,
  PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
await using var newFileStream = new FileStream(newFilePath, FileMode.Create);
var dedup = new BitWardenExport
{
  Folders = idToFolder.Values.ToArray(),
  Items = newItems
};
await JsonSerializer.SerializeAsync(newFileStream, dedup, options);

Console.WriteLine("Report:");
Console.WriteLine($"---------------------------------");
Console.WriteLine($"Folder Before: {json.Folders.Length}\t-\t Folder After: {dedup.Folders.Length}");
Console.WriteLine($"---------------------------------");
Console.WriteLine($"Item Before: {json.Items.Length}\t-\t Item After: {dedup.Items.Length}");
Console.WriteLine($"---------------------------------");

Console.ReadKey();

// Hashes the item, ignoring the id and folder id
string ItemHash(BitwardenItem item)
{
  Uri? uri = null;
  Uri.TryCreate(item.Login?.Uris.FirstOrDefault()?.Uri, new UriCreationOptions(), out uri);
  var domain = uri?.GetLeftPart(UriPartial.Authority) ?? null;
  return
      $"{item.Name}-{item.Login?.Username ?? "x"}-{domain ?? "x"}-{item.SecureNote?.Type ?? 0}-{item.Card?.GetHashCode().ToString() ?? "x"}-{item.Type}-{item.Reprompt}";
}