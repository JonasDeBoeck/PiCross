using Cells;
using DataStructures;
using PiCross;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using Utility;

namespace ViewModel
{
    public class PicrossViewModel
    {
        public IPlayablePuzzle playablePuzzle;
        public IGrid<SquareViewModel> Grid { get; }
        public IEnumerable<IPlayablePuzzleConstraints> RowConstraints => this.playablePuzzle.RowConstraints;
        public IEnumerable<IPlayablePuzzleConstraints> ColumnConstraints => this.playablePuzzle.ColumnConstraints;
        public Cell<bool> IsSolved { get; set; }
        private readonly DispatcherTimer timer;
        public Chronometer chronometer { get; }
        public PicrossViewModel(Puzzle puzzle)
        {
            IsSolved = Cell.Create<bool>(false);
            var facade = new PiCrossFacade();
            playablePuzzle = facade.CreatePlayablePuzzle(puzzle);
            this.Grid = this.playablePuzzle.Grid.Map(square => new SquareViewModel(square));
            this.chronometer = new Chronometer();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            chronometer.Start();
            timer.Tick += (o, s) => chronometer.Tick();
            timer.Start();
        }
    }

    public class SquareViewModel
    {
        private IPlayablePuzzleSquare square;
        public IPlayablePuzzleSquare Square { get; set; }
        public ICommand OnClick { get; }
        public SquareViewModel(IPlayablePuzzleSquare square)
        {
            Square = square;
            OnClick = new OnClickCommand();
        }
    }

    public class OnClickCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var data = parameter as SquareViewModel;
            if (data.Square.Contents.Value == Square.UNKNOWN)
            {
                data.Square.Contents.Value = Square.FILLED;
            }
            else if (data.Square.Contents.Value == Square.FILLED)
            {
                data.Square.Contents.Value = Square.EMPTY;
            } else if (data.Square.Contents.Value == Square.EMPTY)
            {
                data.Square.Contents.Value = Square.UNKNOWN;
            }
        }
    }

    public class HelpCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private IPlayablePuzzle puzzle;
        private IStepwisePuzzleSolver solvedPuzzle;
        public HelpCommand(IPlayablePuzzle puzzle)
        {
            this.puzzle = puzzle;
            var facade = new PiCrossFacade();
            solvedPuzzle = facade.CreateStepwisePuzzleSolver(PlayablePuzzleConstraintsToConstraints(puzzle.RowConstraints), PlayablePuzzleConstraintsToConstraints(puzzle.ColumnConstraints));
            while (!solvedPuzzle.IsSolved)
            {
                solvedPuzzle.Step();
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            for (int i = 0; i < puzzle.Grid.Items.Count(); i++)
            {
                if ((puzzle.Grid.Items.ElementAt(i).Contents.Value != solvedPuzzle.Grid.Items.ElementAt(i)))
                {
                    puzzle.Grid.Items.ElementAt(i).Contents.Value = Square.UNKNOWN;
                }
            }
        }

        private ISequence<Constraints> PlayablePuzzleConstraintsToConstraints(ISequence<IPlayablePuzzleConstraints> playableConstraints)
        {
            IEnumerable<IEnumerable<int>> constraints = playableConstraints.Select(i => (i.Values).Select(ix => ix.Value));
            return constraints.Select(i => Constraints.FromValues(i)).ToSequence();
        }
    }

    public class CheckSolutionCommand : ICommand
    {
        private IPlayablePuzzle puzzle;

        public CheckSolutionCommand (IPlayablePuzzle puzzle)
        {
            this.puzzle = puzzle;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            PuzzleScreen screen = (PuzzleScreen)parameter;
            PicrossViewModel vm = screen.PicrossViewModel;
            vm.IsSolved.Value = this.puzzle.IsSolved.Value;
            if (this.puzzle.IsSolved.Value)
            {
                vm.chronometer.Pause();
            }
        }
    }

    public class SquareConverter : IValueConverter
    {
        public object Filled { get; set; }
        public object Empty { get; set; }
        public object Unknown { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var square = (Square)value;
            if (square == Square.EMPTY)
            {
                return Empty;
            }
            else if (square == Square.FILLED)
            {
                return Filled;
            }
            else
            {
                return Unknown;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Navigator : INotifyPropertyChanged
    {
        private Screen currentScreen;
        public Screen selectionScreen;
        public Navigator()
        {
            this.currentScreen = new MenuScreen(this);
            this.selectionScreen = new SelectionScreen(this);
        }

        public Screen CurrentScreen
        {
            get
            {
                return currentScreen;
            }
            set
            {
                this.currentScreen = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentScreen)));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public abstract class Screen
    {
        protected readonly Navigator navigator;

        protected Screen(Navigator navigator)
        {
            this.navigator = navigator;
        }

        public void SwitchTo(Screen screen)
        {
            this.navigator.CurrentScreen = screen;
        }
    }

    public class MenuScreen : Screen
    {
        public MenuScreen(Navigator navigator) : base(navigator)
        {
            GoToSelection = new EasyCommand(() => SwitchTo(navigator.selectionScreen));
        }
        public ICommand GoToSelection { get; }
    }

    public class SelectionScreen : Screen
    {
        public IList<PuzzleViewModel> puzzles { get; }
        public SelectionScreen(Navigator navigator) : base(navigator)
        {
            var facade = new PiCrossFacade();
            puzzles = facade.LoadGameData("../../../../python/picross.zip").PuzzleLibrary.Entries.Select(entry => new PuzzleViewModel(entry)).ToList();
            GoToPuzzle = new GoToPuzzleCommand(navigator, this);
            GoToMenu = new EasyCommand(() => SwitchTo(new MenuScreen(navigator)));
        }
        public ICommand GoToPuzzle { get; }
        public ICommand GoToMenu { get; }
    }

    public class GoToPuzzleCommand : ICommand
    {
        private Navigator navigator;
        private SelectionScreen screen;
        public GoToPuzzleCommand(Navigator navigator, SelectionScreen screen)
        {
            this.navigator = navigator;
            this.screen = screen;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            PuzzleViewModel puzzle = (PuzzleViewModel)parameter;
            screen.SwitchTo(new PuzzleScreen(navigator, puzzle.vm));
        }
    }

    public class PuzzleScreen : Screen
    {
        private PicrossViewModel picrossViewModel;
        public ICommand CheckSolution { get; }
        public ICommand Help { get; }
        public PuzzleScreen(Navigator navigator, PicrossViewModel picrossViewModel) : base(navigator)
        {
            this.picrossViewModel = picrossViewModel;
            GoToSelection = new EasyCommand(() => SwitchTo(navigator.selectionScreen));
            this.CheckSolution = new CheckSolutionCommand(this.picrossViewModel.playablePuzzle);
            this.Help = new HelpCommand(this.picrossViewModel.playablePuzzle);
        }
        public PicrossViewModel PicrossViewModel { get {
                return picrossViewModel;
            }
        }
        public ICommand GoToSelection { get; }
    }

    public class EasyCommand : ICommand
    {
        private readonly Action action;

        public EasyCommand(Action action)
        {
            this.action = action;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            action();
        }
    }

    public class PuzzleViewModel
    {
        public PicrossViewModel vm { get; }
        public IPuzzleLibraryEntry entry { get; }
        public PuzzleViewModel(IPuzzleLibraryEntry entry)
        {
            this.vm = new PicrossViewModel(entry.Puzzle);
            this.entry = entry;
        }
    }
}