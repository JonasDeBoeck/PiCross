using Cells;
using DataStructures;
using PiCross;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace ViewModel
{
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
        public PicrossViewModel PicrossViewModel
        {
            get
            {
                return picrossViewModel;
            }
        }
        public ICommand GoToSelection { get; }
    }
}
