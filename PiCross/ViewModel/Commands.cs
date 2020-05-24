using DataStructures;
using PiCross;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace ViewModel
{
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
            }
            else
            {
                int smallest;
                int biggest;
                if (start.X == square.Square.Position.X)
                {
                    if (square.Square.Position.Y < start.Y)
                    {
                        smallest = square.Square.Position.Y;
                        biggest = start.Y;
                    }
                    else
                    {
                        smallest = start.Y;
                        biggest = square.Square.Position.Y;
                    }
                    while (smallest <= biggest)
                    {
                        screen.PicrossViewModel.Grid[new Vector2D(start.X, smallest)].Square.Contents.Value = Square.FILLED;
                        smallest++;
                    }
                }
                else if (start.Y == square.Square.Position.Y)
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
            }
            else if (data.Square.Contents.Value == Square.EMPTY)
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

        public CheckSolutionCommand(IPlayablePuzzle puzzle)
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
                vm.Chronometer.Pause();
            }
        }
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
            if (!parameters[0].Equals("") && !parameters[1].Equals(""))
            {
                int rows = Int32.Parse((string)parameters[0]);
                int columns = Int32.Parse((string)parameters[1]);
                PuzzleEditorScreen screen = (PuzzleEditorScreen)parameters[2];
                Puzzle puzzle = Puzzle.CreateEmpty(new Size(columns, rows));
                var facade = new PiCrossFacade();
                screen.SwitchTo(new PuzzleEditScreen(screen.navigator, facade.CreatePuzzleEditor(puzzle)));
            }
        }
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
                screen.navigator.selectionScreen.PuzzleSizes.Add(puzzle.Size);
                screen.SwitchTo(new MenuScreen(screen.navigator));
            }
        }
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
            IList<PuzzleViewModel> puzzles = screen.Puzzles;
            if (type.Equals("ASC"))
            {
                screen.Puzzles = puzzles.OrderBy(puzzle => puzzle.Entry.Puzzle.Size.Width).ToList();
            }
            else
            {
                screen.Puzzles = puzzles.OrderByDescending(puzzle => puzzle.Entry.Puzzle.Size.Width).ToList();
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
            IList<PuzzleViewModel> puzzles = screen.Puzzles;
            screen.Puzzles = puzzles.OrderBy(puzzle => puzzle.Vm.IsSolved.Value).ToList();
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
            IList<PuzzleViewModel> puzzles = screen.Puzzles;
            screen.Puzzles = puzzles.OrderByDescending(puzzle => puzzle.Vm.IsSolved.Value).ToList();
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
            screen.Puzzles = screen.backup;
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
            screen.Puzzles = puzzles.Where(puzzle => puzzle.Entry.Puzzle.Size == size).ToList();
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
            IList<PuzzleViewModel> puzzles = screen.Puzzles;
            screen.Puzzles = puzzles.Where(puzzle => puzzle.Vm.IsSolved.Value == false).ToList();
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
            screen.SwitchTo(new PuzzleScreen(navigator, puzzle.Vm));
        }
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
            screen.PicrossViewModel.Chronometer.Pause();
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
}
