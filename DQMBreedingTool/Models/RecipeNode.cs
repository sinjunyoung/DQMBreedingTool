using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace DQMBreedingTool.Models
{
    public class RecipeNode : INotifyPropertyChanged
    {
        public int MonsterId { get; set; }
        public string DisplayName { get; set; } = "";
        public string Family { get; set; } = "";
        public string Rank { get; set; } = "";
        public string SourceLabel { get; set; } = "";
        public ImageSource? Icon { get; set; }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<RecipeNode> Children { get; set; } = [];

        public bool IsRecipeGroup { get; set; }

        public string Header => IsRecipeGroup ? $"◆ {SourceLabel}" : $"{DisplayName} ({Family} {Rank})";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}