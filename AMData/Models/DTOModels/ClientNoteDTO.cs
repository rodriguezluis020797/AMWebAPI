using System.Globalization;
using AMData.Models.CoreModels;
using AMTools.Tools;

namespace AMData.Models.DTOModels;

public class ClientNoteDTO : BaseDTO
{
    public string ClientNoteId { get; set; }
    public string ClientId { get; set; }
    public string CreateDate { get; set; }
    public string? UpdateDate { get; set; }
    public string Note { get; set; }

    public void Validate()
    {
        ValidationTool.ValidateName(Note, out var nOutput);
        Note = nOutput;
        ErrorMessage = string.IsNullOrEmpty(Note) ? "Please note." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;
    }

    public void CreateRecordFromModel(ClientNoteModel model)
    {
        ClientNoteId = model.ClientNoteId.ToString();
        ClientId = model.ClientId.ToString();
        Note = model.Note;
        CreateDate = model.CreateDate.ToString("MM/dd/yyyy h:mm tt", CultureInfo.InvariantCulture);
        UpdateDate = model.UpdateDate?.ToString("MM/dd/yyyy h:mm tt", CultureInfo.InvariantCulture);
    }
}