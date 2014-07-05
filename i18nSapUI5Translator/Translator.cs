using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using i18nSapUI5Translator.Classes;
namespace i18nSapUI5Translator
{
    public partial class Translator : Form
    {
        private string[] mI18n;
        private string[] mI18nFiles;
        public List<Dictionary<string, I18n>> mI18nDictionary { get; private set; }
        public Translator()
        {
            InitializeComponent();
            this.dataGridView1.Visible = false;
            this.label2.Visible = false;
            this.dataGridView1.MouseDown += dataGridView1_MouseDown;
            this.contextMenuStrip1.Click += new System.EventHandler(this.contextMenuStrip1_Click);
        }

        private void contextMenuStrip1_Click(object sender, EventArgs e)
        {
            Int32 rowToDelete = this.dataGridView1.Rows.GetFirstRow(DataGridViewElementStates.Selected);
            this.dataGridView1.Rows.RemoveAt(rowToDelete);
            this.dataGridView1.ClearSelection();
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hti = this.dataGridView1.HitTest(e.X, e.Y);
                this.dataGridView1.ClearSelection();
                this.dataGridView1.Rows[hti.RowIndex].Selected = true;
            }
        }

        private void I18NPath_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.textBox1.Text = folderBrowserDialog1.SelectedPath;
                this.mI18nFiles = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.properties").Select(p => Path.GetFileNameWithoutExtension(p)).ToArray();
                this.mI18n = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.properties");
                if (this.mI18nFiles.Length <= 0)
                {
                    this.label2.Visible = true;
                    return;
                }
                this.CreateDGVColumns(this.mI18nFiles);
                this.label2.Visible = false;
                this.dataGridView1.Visible = true;
                this.ConstructDGVData();
            }
        }

        private void CreateDGVColumns(string[] iI18nFiles)
        {
            var stringCol = new DataGridViewTextBoxColumn();
            var commentCol = new DataGridViewTextBoxColumn();
            stringCol.HeaderText = "Tag";
            stringCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            stringCol.Name = "i18ntag";
            commentCol.HeaderText = "Comment";
            commentCol.Name = "comment";
            commentCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridView1.Columns.AddRange(new DataGridViewColumn[] { stringCol, commentCol });
            for (var i = 0; i < iI18nFiles.Length; i++)
            {
                var i18nCol = new DataGridViewTextBoxColumn();
                i18nCol.HeaderText = iI18nFiles[i];
                i18nCol.Name = iI18nFiles[i];
                i18nCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                this.dataGridView1.Columns.Add(i18nCol);
            }
        }

        private void ConstructDGVData()
        {
            this.mI18nDictionary = new List<Dictionary<string, I18n>>();
            foreach (var s in mI18n)
            {
                this.mI18nDictionary.Add(I18nParser.ReadFile(s));
            }
            //construct tags file based on primary i18n file
            foreach (KeyValuePair<string, I18n> p in this.mI18nDictionary[0])
            {
                DataGridViewRow row = (DataGridViewRow)this.dataGridView1.Rows[0].Clone();
                row.Cells[0].Value = p.Key;
                row.Cells[1].Value = p.Value.Comment;
                this.dataGridView1.Rows.Add(row);
            }
            //will dynamically generate i18n properties
            var i18nFileNo = 0;
            foreach (Dictionary<string, I18n> allI18n in this.mI18nDictionary)
            {
                foreach (KeyValuePair<string, I18n> property in allI18n)
                {
                    var propertyRow = (from row in this.dataGridView1.Rows.Cast<DataGridViewRow>() where row.Cells[0].Value.ToString() == property.Key select row);
                    if (propertyRow.Any())
                    {
                        propertyRow.First().Cells[i18nFileNo + 2].Value = property.Value.Value;
                    }
                }
                i18nFileNo++;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog2.ShowDialog();
            var tags = (from row in this.dataGridView1.Rows.Cast<DataGridViewRow>()  select row.Cells[0].Value).Cast<string>().ToList();
            var comments = (from row in this.dataGridView1.Rows.Cast<DataGridViewRow>() select row.Cells[1].Value).Cast<string>().ToList(); 
            var i18nFileNo = 0;
            if (result == DialogResult.OK)
            {
                var saveToFolder = folderBrowserDialog2.SelectedPath;
                foreach (var s in this.mI18nFiles)
                {
                    var i18nRow = (from row in this.dataGridView1.Rows.Cast<DataGridViewRow>() select row.Cells[i18nFileNo + 2].Value).Cast<string>().ToList();
                    var i18n = tags.Zip(comments.Zip(i18nRow, (b, c) => new { b, c }), (a, b) => new I18n {Key= a, Comment = b.b, Value = b.c });

                    foreach (var p in i18n)
                    {
                        using (StreamWriter writer = new StreamWriter(string.Format("{0}\\{1}.properties", saveToFolder, s), true))
                        {
                            if (!string.IsNullOrEmpty(p.Comment))
                            {
                                writer.WriteLine(string.Format("# {0}", p.Comment));
                            }
                            if (!string.IsNullOrEmpty(p.Key) && !string.IsNullOrEmpty(p.Key))
                            {
                                writer.WriteLine(string.Format("{0} = {1}", p.Key, p.Value));
                            }
                            else if (!string.IsNullOrEmpty(p.Key))
                            {
                                writer.WriteLine(string.Format("{0} = {1}", p.Key, p.Value));
                            }
                        }
                    }
                }
            }
        }
    }
}
