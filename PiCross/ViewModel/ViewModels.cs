using Cells;
using DataStructures;
using PiCross;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Utility;

namespace ViewModel
{
    public class PicrossViewModel
    {
        public IPlayablePuzzle playablePuzzle;
        public IGrid<SquareViewModel> Grid { get; }
        public PlayablePuzzleConstraintsViewModel RowConstraints { get; set; }
        public PlayablePuzzleConstraintsViewModel ColumnConstraints { get; set; }
        public Cell<bool> IsSolved { get; set; }
        private readonly DispatcherTimer timer;
        public Chronometer Chronometer { get; }
        public Vector2D start;
        public PicrossViewModel(Puzzle puzzle)
        {
            IsSolved = Cell.Create<bool>(false);
            var facade = new PiCrossFacade();
            playablePuzzle = facade.CreatePlayablePuzzle(puzzle);
            this.Grid = this.playablePuzzle.Grid.Map(square => new SquareViewModel(square));
            this.Chronometer = new Chronometer();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += (o, s) => Chronometer.Tick();
            timer.Start();

            RowConstraints = new PlayablePuzzleConstraintsViewModel(this.playablePuzzle.RowConstraints);
            ColumnConstraints = new PlayablePuzzleConstraintsViewModel(this.playablePuzzle.ColumnConstraints);
        }
    }

    public class PlayablePuzzleConstraintsViewModel
    {
        public IEnumerable<PlayablePuzzleConstraintsValueViewModel> Constraints { get; set; }

        public PlayablePuzzleConstraintsViewModel (ISequence<IPlayablePuzzleConstraints> constraints)
        {
            Constraints = constraints.Map(constraint => new PlayablePuzzleConstraintsValueViewModel(constraint));
        }
    }

    public class PlayablePuzzleConstraintsValueViewModel
    {
        public IPlayablePuzzleConstraints Values { get; set; }

        public PlayablePuzzleConstraintsValueViewModel (IPlayablePuzzleConstraints constraints)
        {
            this.Values = constraints;
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

    public class PuzzleViewModel
    {
        public PicrossViewModel Vm { get; }
        public IPuzzleLibraryEntry Entry { get; }
        public PuzzleViewModel(IPuzzleLibraryEntry entry)
        {
            this.Vm = new PicrossViewModel(entry.Puzzle);
            this.Entry = entry;
        }
    }
}