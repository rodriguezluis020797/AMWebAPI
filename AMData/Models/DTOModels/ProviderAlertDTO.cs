using AMData.Models.CoreModels;

namespace AMData.Models.DTOModels;

public class ProviderAlertDTO : BaseDTO
{
    public string ProviderAlertId { get; set; }
    public string Alert { get; set; }

    public void CreateRecordFromModel(ProviderAlertModel model)
    {
        ProviderAlertId = model.ProviderAlertId.ToString();
        Alert = model.Alert;
    }
}