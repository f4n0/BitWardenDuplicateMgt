using System.Text.Json.Serialization;

namespace BitWardenDuplicateMgt;

public class BitWardenExport
{
    public bool Encrypted { get; set; }
    public BitwardenFolder[] Folders { get; set; }
    public BitwardenItem[] Items { get; set; }
}

public class BitwardenFolder
{
  public string Id { get; set; }
  public string Name { get; set; }

  public void Deconstruct(out string id, out string name)
  {
    id = Id;
    name = Name;
  }
}

public class BitwardenItem
{
  public string Id { get; set; }
  public string? OrganizationId { get; set; }
  public string? FolderId { get; set; }
  public int Type { get; set; }
  public int Reprompt { get; set; }
  public string Name { get; set; }
  public string? Notes { get; set; }
  public bool Favorite { get; set; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public BitwardenLogin? Login { get; set; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public BitwardenCard? Card { get; set; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public BitwardenSecureNote? SecureNote { get; set; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public BitwardenField[]? Fields { get; set; }

  public string[]? CollectionIds { get; set; }
}

public class BitwardenLogin
{
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<BitwardenUri>? Uris { get; set; }

  public string Username { get; set; }
  public string Password { get; set; }
  public string? Totp { get; set; }
}

public record BitwardenSecureNote(int Type);

public record BitwardenUri(string Match, string Uri);

public record BitwardenCard(string CardholderName, string Brand, string Number, string ExpMonth, string ExpYear, string Code);

public record BitwardenField(string Name, string? Value, int Type, string? linkedId);