using System.ComponentModel.DataAnnotations;

namespace TFB.Mvc.Models
{
    public class PersonalityUpdateRequestModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string PersonalityDescription { get; set; }
    }
}
