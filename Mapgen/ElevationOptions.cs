using System;

namespace Mapgen {
    public class ElevationOptions : ViewModel {
        private MainWindowViewModel _mainWindowViewModel;
        private int _seed = 42;
        private double _freq = 0.008;
        public int _waterLevel;

        private Action triggerRender;
        private int _octaveCount = 4;

        public ElevationOptions(MainWindowViewModel mainWindowViewModel, Action triggerRender)
        {
            _mainWindowViewModel = mainWindowViewModel;
            this.triggerRender = triggerRender;
        }

        public int Seed
        {
            get { return _seed; }
            set
            {
                _seed = value;
                OnPropertyChanged();
            }
        }

        public double Freq
        {
            get { return _freq; }
            set { _freq = value;
                OnPropertyChanged(); }
        }

        public int WaterLevel
        {
            get { return _waterLevel; }
            set
            {
                _waterLevel = value;
                OnPropertyChanged();
                _mainWindowViewModel.SetDirty(EDirty.WaterLevel);
                triggerRender();
            }
        }

        public int OctaveCount
        {
            get { return _octaveCount; }
            set
            {
                _octaveCount = value;
                OnPropertyChanged();
            }
        }
    }
}