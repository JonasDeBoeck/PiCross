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
        public Vector2D start;
        public PicrossViewModel(Puzzle puzzle)
        {
            IsSolved = Cell.Create<bool>(false);
            var facade = new PiCrossFacade();
            playablePuzzle = facade.CreatePlayablePuzzle(puzzle);
            this.Grid = this.playablePuzzle.Grid.Map(square => new SquareViewModel(square));
            this.chronometer = new Chronometer();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += (o, s) => chronometer.Tick();
            timer.Start();
        }
    }

    public class SquareViewModel
    {
        private IPlayablePuzzleSquare square;
        public IPlayablePuzzleSquare Square { get; set; }
        public ICommand ChangeSquareContent { get; }
        public ICommand ContentRangeChanger { get; }
        public SquareViewModel(IPlayablePuzzleSquare square)
        {
            Square = square;
            ChangeSquareContent = new ChangeSquareContentsCommand();
            ContentRangeChanger = new ContentRangeChangerCommand();
        }
    }

    public class ContentRangeChangerCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            object[] parameters = (object[])parameter;
            PuzzleScreen screen = (PuzzleScreen)parameters[0];
            SquareViewModel square = (SquareViewModel)parameters[1];
            Vector2D start = screen.PicrossViewModel.start;
            if (start == null)
            {
                screen.PicrossViewModel.start = square.Square.Position;
            } else
            {
                int smallest;
                int biggest;
                if (start.X == square.Square.Position.X)
                {
                    if (square.Square.Position.Y < start.Y)
                    {
                        smallest = square.Square.Position.Y;
                        biggest = start.Y;
                    } else
                    {
                        smallest = start.Y;
                        biggest = square.Square.Position.Y;
                    }
                    while(smallest <= biggest)
                    {
                        screen.PicrossViewModel.Grid[new Vector2D(start.X, smallest)].Square.Contents.Value = Square.FILLED;
                        smallest++;
                    }
                } else if (start.Y == square.Square.Position.Y)
                {
                    if (square.Square.Position.X < start.X)
                    {
                        smallest = square.Square.Position.X;
                        biggest = start.X;
                    }
                    else
                    {
                        smallest = start.X;
                        biggest = square.Square.Position.X;
                    }
                    while (smallest <= biggest)
                    {
                        screen.PicrossViewModel.Grid[new Vector2D(smallest, start.Y)].Square.Contents.Value = Square.FILLED;
                        smallest++;
                    }
                }
                screen.PicrossViewModel.start = null;
            }
        }
    }

    public class ChangeSquareContentsCommand : ICommand
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

    public class Navigator : INotifyPropertyChanged
    {
        private Screen currentScreen;
        public SelectionScreen selectionScreen;
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
        public readonly Navigator navigator;

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
            GoToPuzzleEditor = new EasyCommand(() => SwitchTo(new PuzzleEditorScreen(navigator)));
        }
        public ICommand GoToSelection { get; }
        public ICommand GoToPuzzleEditor { get; }
    }

    public class PuzzleEditorScreen : Screen
    {
        public PuzzleEditorScreen(Navigator navigator) : base(navigator)
        {
            GoToMainMenu = new EasyCommand(() => SwitchTo(new MenuScreen(navigator)));
            CreateEmptyPuzzle = new CreateEmptyPuzzleCommand();
        }
        public ICommand GoToMainMenu { get; }
        public ICommand CreateEmptyPuzzle { get; }
    }

    public class CreateEmptyPuzzleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            object[] parameters = (object[])parameter;
            int rows = Int32.Parse((string)parameters[0]);
            int columns = Int32.Parse((string)parameters[1]);
            PuzzleEditorScreen screen = (PuzzleEditorScreen)parameters[2];
            Puzzle puzzle = Puzzle.CreateEmpty(new Size(columns, rows));
            var facade = new PiCrossFacade();
            screen.SwitchTo(new PuzzleEditScreen(screen.navigator, facade.CreatePuzzleEditor(puzzle)));
        }
    }

    public class PuzzleEditorSquareViewModel
    {
        public IPuzzleEditorSquare Square { get; }
        public PuzzleEditorSquareViewModel (IPuzzleEditorSquare square)
        {
            this.Square = square;
            ChangeContentsEditorSquare = new ChangeContentsEditorSquareCommand();
        }
        public ICommand ChangeContentsEditorSquare { get; }
    }

    public class ChangeContentsEditorSquareCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            PuzzleEditorSquareViewModel square = (PuzzleEditorSquareViewModel)parameter;
            square.Square.IsFilled.Value = !square.Square.IsFilled.Value;
        }
    }

    public class PuzzleEditScreen : Screen
    {
        public IPuzzleEditor Editor { get; }
        public IGrid<PuzzleEditorSquareViewModel> Grid { get; }
        public PuzzleEditScreen(Navigator navigator, IPuzzleEditor editor) : base(navigator)
        {
            this.Editor = editor;
            Grid = Editor.Grid.Map(square => new PuzzleEditorSquareViewModel(square));
            GoBack = new EasyCommand(() => SwitchTo(new PuzzleEditorScreen(navigator)));
            SavePuzzle = new SavePuzzleCommand();
        }

        public ICommand GoBack { get; }
        public ICommand SavePuzzle { get; }
    }

    public class SavePuzzleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            PuzzleEditScreen screen = (PuzzleEditScreen)parameter;
            screen.Editor.ResolveAmbiguity();
            var facade = new PiCrossFacade();
            bool am = false;
            foreach (IPuzzleEditorSquare square in screen.Editor.Grid.Items)
            {
                if (square.Ambiguity.Value == Ambiguity.Ambiguous)
                {
                    am = true;
                    break;
                }
            }
            if (!am)
            {
                IGameData gameData = facade.LoadGameData("../../../../python/picross.zip", true);
                Puzzle puzzle = screen.Editor.BuildPuzzle();
                IPuzzleLibraryEntry entry = gameData.PuzzleLibrary.Create(puzzle, "Jonas");
                screen.navigator.selectionScreen.cell.Value.Add(new PuzzleViewModel(entry));
                screen.SwitchTo(new MenuScreen(screen.navigator));
            } 
        }
    }

    public class SelectionScreen : Screen
    {
        public IList<PuzzleViewModel> backup;
        public IList<PuzzleViewModel> puzzles { get { return cell.Value; } set { cell.Value = value; } }
        public Cell<IList<PuzzleViewModel>> cell { get; }
        public IList<Size> puzzleSizes { get; }
        public SelectionScreen(Navigator navigator) : base(navigator)
        {
            var facade = new PiCrossFacade();
            cell = Cell.Create<IList<PuzzleViewModel>>(facade.LoadGameData("../../../../python/picross.zip").PuzzleLibrary.Entries.Select(entry => new PuzzleViewModel(entry)).ToList());
            backup = this.puzzles.Select(puzzle => puzzle).ToList();
            puzzleSizes = this.puzzles.Select(puzzle => puzzle.entry.Puzzle.Size).Distinct().ToList();
            GoToPuzzle = new GoToPuzzleCommand(navigator, this);
            GoToMenu = new EasyCommand(() => SwitchTo(new MenuScreen(navigator)));
            FilterUnsolved = new FilterUnsolvedCommand();
            FilterSize = new FilterSizeCommand();
            ClearFilters = new ClearFiltersCommand();
            OrderBySolved = new OrderBySolvedCommand();
            OrderByUnsolved = new OrderByUnsolvedCommand();
            OrderBySize = new OrderBySizeCommand();
        }
        public ICommand GoToPuzzle { get; }
        public ICommand GoToMenu { get; }
        public ICommand FilterUnsolved { get; }
        public ICommand FilterSize { get; }
        public ICommand ClearFilters { get; }
        public ICommand OrderBySolved { get; }
        public ICommand OrderByUnsolved { get; }
        public ICommand OrderBySize { get; }
    }

    public class OrderBySizeCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            object[] parameters = (object[])parameter;
            string type = (string)parameters[0];
            SelectionScreen screen = (SelectionScreen)parameters[1];
            IList<PuzzleViewModel> puzzles = screen.puzzles;
            if (type.Equals("ASC"))
            {
                screen.puzzles = puzzles.OrderBy(puzzle => puzzle.entry.Puzzle.Size.Width).ToList();
            }
            else
            {
                screen.puzzles = puzzles.OrderByDescending(puzzle => puzzle.entry.Puzzle.Size.Width).ToList();
            }
        }
    }

    public class OrderByUnsolvedCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            SelectionScreen screen = (SelectionScreen)parameter;
            IList<PuzzleViewModel> puzzles = screen.puzzles;
            screen.puzzles = puzzles.OrderBy(puzzle => puzzle.vm.IsSolved.Value).ToList();
        }
    }

    public class OrderBySolvedCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            SelectionScreen screen = (SelectionScreen)parameter;
            IList<PuzzleViewModel> puzzles = screen.puzzles;
            screen.puzzles = puzzles.OrderByDescending(puzzle => puzzle.vm.IsSolved.Value).ToList();
        }
    }

    public class ClearFiltersCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            SelectionScreen screen = (SelectionScreen)parameter;
            screen.puzzles = screen.backup;
        }
    }

    public class FilterSizeCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            object[] parameters = (object[])parameter;
            Size size = (Size)parameters[0];
            SelectionScreen screen = (SelectionScreen)parameters[1];
            IList<PuzzleViewModel> puzzles = screen.backup;
            screen.puzzles = puzzles.Where(puzzle => puzzle.entry.Puzzle.Size == size).ToList();
        }
    }

    public class FilterUnsolvedCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            SelectionScreen screen = (SelectionScreen)parameter;
            IList<PuzzleViewModel> puzzles = screen.puzzles;
            screen.puzzles = puzzles.Where(puzzle => puzzle.vm.IsSolved.Value == false).ToList();
        }
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
        public Navigator navigator;
        public PuzzleScreen(Navigator navigator, PicrossViewModel picrossViewModel) : base(navigator)
        {
            this.picrossViewModel = picrossViewModel;
            picrossViewModel.chronometer.Start();
            this.navigator = navigator;
            GoToSelection = new GoToSelectionCommand(this);
            this.CheckSolution = new CheckSolutionCommand(this.picrossViewModel.playablePuzzle);
            this.Help = new HelpCommand(this.picrossViewModel.playablePuzzle);
        }
        public PicrossViewModel PicrossViewModel { get {
                return picrossViewModel;
            }
        }
        public ICommand GoToSelection { get; }
    }

    public class GoToSelectionCommand : ICommand
    {
        private PuzzleScreen screen;
        public GoToSelectionCommand(PuzzleScreen screen)
        {
            this.screen = screen;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            screen.PicrossViewModel.chronometer.Pause();
            screen.SwitchTo(screen.navigator.selectionScreen);
        }
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