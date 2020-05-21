using Cells;
using DataStructures;
using PiCross;
using System;
using System.Collections.Generic;
using System.Windows.Input;
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
        public PicrossViewModel vm { get; }
        public IPuzzleLibraryEntry entry { get; }
        public PuzzleViewModel(IPuzzleLibraryEntry entry)
        {
            this.vm = new PicrossViewModel(entry.Puzzle);
            this.entry = entry;
        }
    }
}