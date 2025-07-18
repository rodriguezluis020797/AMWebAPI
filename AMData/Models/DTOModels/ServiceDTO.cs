using AMData.Models.CoreModels;
using MCCDotnetTools;

namespace AMData.Models.DTOModels;

public class ServiceDTO : BaseDTO
{
    public string ServiceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public decimal Price { get; set; } = decimal.Zero;
    public bool AllowClientScheduling { get; set; }

    public void Validate()
    {
        MCCValidationTool.ValidateName(Name, out var nOutput);
        Name = nOutput;
        if (string.IsNullOrEmpty(Name))
        {
            ErrorMessage = "Name is required";
            return;
        }

        MCCValidationTool.ValidateName(Description, out var dOutput);
        Description = !string.IsNullOrEmpty(dOutput) ? dOutput : null;

        if (Price < decimal.Zero)
            ErrorMessage = "Price cannot be negative";
    }

    public void AssignFromModel(ServiceModel model)
    {
        ServiceId = model.ServiceId.ToString();
        Name = model.Name;
        Description = model.Description;
        Price = model.Price;
        AllowClientScheduling = false; //model.AllowClientScheduling;
    }
}