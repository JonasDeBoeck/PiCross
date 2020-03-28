using DataStructures;
using PiCross;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Grid = DataStructures.Grid;
using Size = DataStructures.Size;

namespace View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var puzzle = Puzzle.FromRowStrings(
                    "xxxxx",
                    "x...x",
                    "x...x",
                    "x...x",
                    "xxxxx"
                );

            var facade = new PiCrossFacade();
            var playablePuzzle = facade.CreatePlayablePuzzle(puzzle);

            playablePuzzle.Grid[new Vector2D(0, 0)].Contents.Value = Square.FILLED;
            playablePuzzle.Grid[new Vector2D(1, 0)].Contents.Value = Square.EMPTY;

            picrossControl.Grid = playablePuzzle.Grid;
            picrossControl.RowConstraints = Sequence.FromItems(
                new int[] {1, 2},
                new int[] {4},
                new int[] {1, 1},
                new int[] {1, 1},
                new int[] {1, 1, 1}
                );
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
}
