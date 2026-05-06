using System.Threading.Tasks;
namespace AudioStemPlayer.Core.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowErrorAsync(string title, string message); 
}