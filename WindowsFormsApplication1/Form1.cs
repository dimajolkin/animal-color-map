using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    

    public partial class Form1 : Form
    {
        class ColorCount
        {
            private Color color;
            private int count = 0;

            public Color Color
            {
                get { return color; }
                set { color = value; }
            }

            public int Count
            {
                get { return count; }
                set { count = value; }
            }


            public void inc()
            {
                count++;
            }

            public int getRgb()
            {
                return color.R * color.G * color.B;
            }

            public ColorCount(Color color, int count = 0)
            {
                this.color = color;
                this.count = count;
            }
        }

        class ColorCountCollectin : List<ColorCount>
        {
            public int range = 80000;
            List<Color> defaultColors;

            public bool Contains(Color item)
            {
                foreach (ColorCount c in this)
                {
                    if (c.Color == item)
                    {
                        return true;
                    }
                }

                return false;
            }

            private void inc(ColorCount c)
            {
                c.inc();
            }

            private List<Color> GetColors()
            {
                if (defaultColors == null)
                {
                    defaultColors = new List<Color>();
                    string[] colorNames = Enum.GetNames(typeof(KnownColor));
                    foreach (string colorName in colorNames)
                    {
                        KnownColor knownColor = (KnownColor)Enum.Parse(typeof(KnownColor), colorName);
                        if (knownColor > KnownColor.Transparent)
                        {
                            defaultColors.Add(Color.FromName(colorName));
                        }
                    }
                }

                return defaultColors;
            }

            public void Add(Color item)
            {
                int rgb = item.R * item.G * item.B;
                bool f = true;
                this.ForEach(c => {
                    if ((c.Color.ToArgb() - range < item.ToArgb()) && (c.Color.ToArgb() + range > item.ToArgb()))
                    {
                        inc(c);
                        f = false;
                    }
                });

                if (f)
                {
                    this.Add(new ColorCount(item));
                }

            }

            public ColorCount GetByColor(Color color)
            {
                return this.Find(c => c.Color == color);
            }

            public void Remove(Color item)
            {
                this.RemoveAll(i => i.Color == item);
            }
        }

        class Brush
        {
            public int stepX { get; set; }
            public int stepY { get; set; }
            public Brush(int x, int y)
            {
                this.stepX = x;
                this.stepY = y;
            }


            public Color getMaxColor(Bitmap bmp, int x, int y)
            {
                ColorCountCollectin list = new ColorCountCollectin();
                
                for(int k = x; k < x + this.stepX; k++)
                {
                    for(int m = y; m < y + this.stepY; m++)
                    {
                        list.Add(bmp.GetPixel(k, m));
                    }
                }

                list.Sort(delegate (ColorCount colorX, ColorCount colorY)
                {
                    if (colorX != colorY) return 0;
                    if (colorX.Count == colorY.Count) return 0;
                    if (colorX.Count > colorY.Count) return 1;
                    if (colorX.Count < colorY.Count) return -1;
                    return 0;
                });

                return list[0].Color;
            }
        }

      

        public Form1()
        {
            InitializeComponent();
            dataGridView1.Columns.Add("N", "N");
            dataGridView1.Columns.Add("Color", "txt");
            dataGridView1.Columns.Add("%", "%");
        
            runButton.Enabled = false;
            statusStrip1.Text = "Готов к работе. Выбери файл";
        }

        private Bitmap bitmap;
        Brush brush = new Brush(5, 5);
        private int countPixels = 0;

        private ColorCountCollectin colorCounts = new ColorCountCollectin();
 
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap bitmap = (Bitmap)e.Argument;
            int lastProcent = 0;
            for (int x = 0; x < bitmap.Width - brush.stepX; x+= brush.stepX)
            {
                for (int y = 0; y < bitmap.Height - brush.stepY; y+= brush.stepY)
                {

                    colorCounts.Add(brush.getMaxColor(bitmap, x, y));
                }

                int p = Convert.ToInt32((x * 1.0 / bitmap.Width) * 100);
                if (lastProcent != p)
                {
                    backgroundWorker1.ReportProgress(p);
                    lastProcent = p;
                }
                
            }
        }
       
        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open Image";
                dlg.Filter = "Image Files (*.bmp;*.jpg;*.jpeg,*.png)|*.BMP;*.JPG;*.JPEG;*.PNG";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    colorCounts.Clear();
                    dataGridView1.Rows.Clear();
                    bitmap = new Bitmap(dlg.FileName);
                    countPixels = Convert.ToInt32(bitmap.Width / brush.stepX) * Convert.ToInt32(bitmap.Height / brush.stepY);

                    pictureBox1.Image = (Image)bitmap;
                         progressBar1.Value = progressBar1.Minimum;
                         if (backgroundWorker1.IsBusy)
                         {
                              backgroundWorker1.CancelAsync();
                         }
                    statusStrip1.Text = "Вы выбрали файл: " + dlg.FileName;
                    runButton.Enabled = true;
                    
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            label1.Text = progressBar1.Value.ToString() + "%  цветов(" + colorCounts.Count  + ")";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = progressBar1.Maximum;
            if (!e.Cancelled)
            {
                updateDataGrid(colorCounts, countPixels);
            }

            progressBar1.Value = progressBar1.Minimum;
            label1.Text = "0%";
        }

        private void updateDataGrid(List<ColorCount> list, int countPixels)
        {
            dataGridView1.Rows.Clear();
            foreach (ColorCount color in list)
            {
                double procent = (color.Count * 1.0 / (countPixels)) * 100;

                var two = new DataGridViewRow();

                var numberCell = new DataGridViewTextBoxCell();
                numberCell.Value = dataGridView1.Rows.Count.ToString();

                var colorCell = new DataGridViewTextBoxCell();
                colorCell.Value = color.Color;
                colorCell.Style.BackColor = color.Color;

                var procentCell = new DataGridViewTextBoxCell();
                procentCell.Value = Math.Round(procent, 3).ToString() + "%";

                two.Cells.Add(numberCell);
                two.Cells.Add(colorCell);
                two.Cells.Add(procentCell);

                dataGridView1.Rows.Add(two);
            }
        }

        private void runButton_Click(object sender, EventArgs e)
        {
            colorCounts.Clear();
            dataGridView1.Rows.Clear();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Step = 1;
            label1.Text = "0%";

            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.RunWorkerAsync(bitmap);
            statusStrip1.Text = "В работе.. ";
            runButton.Enabled = false;
        }

        private void dataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            
        }

        private void обновитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            update();
        }

        public void update()
        {
            colorCounts.range = Convert.ToInt32(textBox2.Text);
            brush = new Brush(Convert.ToInt32(textBox1.Text), Convert.ToInt32(textBox1.Text));
            countPixels = Convert.ToInt32(bitmap.Width / brush.stepX) * Convert.ToInt32(bitmap.Height / brush.stepY);

            List<Color> dataGridViewColors = new List<Color>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                dataGridViewColors.Add(row.Cells[1].Style.BackColor);
            }

            List<ColorCount> removes = colorCounts.FindAll(c => !dataGridViewColors.Contains(c.Color));
            colorCounts.RemoveAll(i => removes.Contains(i));
            removes.ForEach(i => { countPixels -= i.Count; });

            updateDataGrid(colorCounts, countPixels);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            
        }
    }
}
