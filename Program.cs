using CliWrap.Buffered;
using CliWrap;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string[] lines = { "echo Hello %USERNAME%!", "echo %TIME%", "dir" };
        string title = "Batch Split Run Example";
        try
        {
            (title, lines) = args switch
            {
                [] => (title, lines),
                [string a] => ($"Batch Split Run From {a}", File.ReadAllLines(a)),
                _ => ($"Batch Split Run From {args.Length} Files", args.SelectMany(File.ReadAllLines).ToArray()),
            };
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }

        static Control[] slot(string line)
        {
            var l = new Box<string> { Value = line };
            var r = new Box<BufferedCommandResult> { Value = new(default, default, default, "", "") };
            var d = new BoxBool { Value = false };
            var cmd = new TextBox
            {
                DataBindings =
                {
                    new Binding(nameof(TextBox.Text),l, nameof(l.Value), true, DataSourceUpdateMode.OnPropertyChanged),
                }
            };
            async void call(object sender, EventArgs e) => r.Value = await Cli.Wrap("cmd").WithArguments("/c " + l.Value).WithValidation(CommandResultValidation.None).ExecuteBufferedAsync();
            var timer = new Timer { Interval = 500, Enabled = false }.As(t => t.Tick += call);
            var result = new Panel
            {
                Height = 130,
                Controls =
                {
                    new TextBox
                    {
                        Multiline = true,
                        Dock = DockStyle.Fill,
                        ScrollBars = ScrollBars.Vertical,
                        BackColor = Color.AliceBlue,
                        DataBindings =
                        {
                            new Binding(nameof(TextBox.Text),r, "Value.StandardOutput", true, DataSourceUpdateMode.OnPropertyChanged),
                            new Binding(nameof(TextBox.Visible),d , nameof(d.Not), true, DataSourceUpdateMode.OnPropertyChanged),
                        }
                    },
                    new PropertyGrid
                    {
                        Dock = DockStyle.Fill,
                        HelpVisible = false,
                        ToolbarVisible = false,
                        PropertySort = PropertySort.NoSort,
                        DataBindings =
                        {
                            new Binding(nameof(PropertyGrid.SelectedObject),r, nameof(r.Value), true, DataSourceUpdateMode.OnPropertyChanged),
                            new Binding(nameof(PropertyGrid.Visible),d , nameof(d.Value), true, DataSourceUpdateMode.OnPropertyChanged),
                        }
                    }
                }
            };
            cmd.Controls.AddRange(new Control[]
            {
                new Button { Text = "Run",Dock = DockStyle.Right, AutoSize = true }.As(b => b.Click += call),
                new CheckBox{ Text = "Loop",Dock = DockStyle.Right, AutoSize = true, DataBindings =
                    {
                        new Binding(nameof(CheckBox.Checked),timer , nameof(timer.Enabled), true, DataSourceUpdateMode.OnPropertyChanged),
                    },
                },
                new NumericUpDown
                {
                    Name = "int",
                    Minimum = 0,
                    Maximum = int.MaxValue,
                    Dock = DockStyle.Right,
                    DataBindings =
                    {
                        new Binding(nameof(NumericUpDown .Enabled),timer, nameof(timer.Enabled), true, DataSourceUpdateMode.OnPropertyChanged),
                        new Binding(nameof(NumericUpDown .Value),timer, nameof(timer.Interval), true, DataSourceUpdateMode.OnPropertyChanged),
                    }
                },
                new CheckBox{ Text = "Detail",Dock = DockStyle.Right, AutoSize = true, DataBindings =
                    {
                        new Binding(nameof(CheckBox.Checked),d , nameof(d.Value), true, DataSourceUpdateMode.OnPropertyChanged),
                    },
                },
            });
            return new Control[] { cmd, result, new Splitter() };
        }
        Application.EnableVisualStyles();
        var arr = lines.SelectMany(slot).Reverse().ToArray();
        foreach (var c in arr) c.Dock = DockStyle.Top;
        Application.Run(new Form
        {
            Text = title,
            AutoScroll = true,
            Width = 640,
            Height = 640,
        }.As(f => f.Controls.AddRange(arr)));
    }
}

class Box<T> : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    T? v;
    public T? Value { get => v; set { v = value; PropertyChanged?.Invoke(this, new("Value")); } }
}
class BoxBool : Box<bool>
{
    public bool Not => !Value;
}

file static class Ex
{
    public static Out To<In, Out>(this In t, Func<In, Out> a) => a(t);
    public static T As<T>(this T t, Action<T> a) { a(t); return t; }
}