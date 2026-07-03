using System.ComponentModel.DataAnnotations;

namespace AspiraHub.ViewModels
{
    public class OnboardingStep4VM
    {
        public List<string> Interests { get; set; } = new();
        // Technology / Business / Design / Medicine / Engineering / Arts

        public string? CustomInterest { get; set; }
    }
}