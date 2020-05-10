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
using i18nSapUI5Translator.Interfaces;
using System.Text.RegularExpressions;

namespace i18nSapUI5Translator
{
    public partial class Translator : Form
    {
        private string[] mI18n;
        private string[] mI18nFiles;
        public List<List<KeyValuePair<string, I18n>>> mI18nDictionary { get; private set; }
        private List<ITranslationFileParser> _translators = new List<ITranslationFileParser> { new I18nParser(), new JsonParser()};
        private ITranslationFileParser _translationParser;
        private const string key_var = "TRANSLATOR_TEXT_SUBSCRIPTION_KEY";
        private static string subscriptionKey;
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com";
        public Translator()
        {
          List<string> listLines = new List<string>();
            foreach (var line in File.ReadLines("env.txt"))
            {
                var split = line.Split('=')[1];
                if(key_var == line.Split('=')[0])
                {
                    subscriptionKey = split;
                }
            }


            if (null == subscriptionKey)
            {
                throw new Exception("Please set/export the environment variable: " + key_var);
            }
            InitializeComponent();
            this.dataGridView1.Visible = false;
            this.label2.Visible = false;
            this.label4.Visible = false;
            this.button3.Visible = false;
            this.AutoTranslate.Visible = false;
            this.listBox1.Visible = false;
            this.dataGridView1.MouseDown += dataGridView1_MouseDown;
            this.contextMenuStrip1.Click += new System.EventHandler(this.contextMenuStrip1_Click);
            this.listBox1.SelectedIndexChanged += ListBox1_SelectedValueChanged;
        }

        private void ListBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            this.CreateDGVColumns(this.mI18nFiles[this.listBox1.SelectedIndex], this.mI18nFiles);
            this.label2.Visible = false;
             this.dataGridView1.Visible = true;
            this.ConstructDGVData(this.mI18nFiles[this.listBox1.SelectedIndex]);
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
                //search for properties files
                this.textBox1.Text = folderBrowserDialog1.SelectedPath;
                foreach (var translator in _translators)
                {
                    this.mI18nFiles = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*" + translator.FileExt).Select(p => Path.GetFileNameWithoutExtension(p)).ToArray();
                    this.mI18n = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*" + translator.FileExt);
                    if (this.mI18nFiles.Length > 0)
                    {
                        _translationParser = translator;
                        break;
                    }
                }

                if (this.mI18nFiles.Length <= 0 || _translationParser == null)
                {
                    this.label2.Visible = true;
                    return;
                }

                this.label4.Visible = true;
                this.listBox1.Visible = true;
                listBox1.Items.AddRange(this.mI18nFiles);

            }
        }

        private void CreateDGVColumns(string referenceFile, string[] iI18nFiles)
        {
            this.dataGridView1.Columns.Clear();
            var stringCol = new DataGridViewTextBoxColumn();
            var commentCol = new DataGridViewTextBoxColumn();
            stringCol.HeaderText = "Tag";
            stringCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            stringCol.Name = "i18ntag";
            commentCol.HeaderText = "ChildTag";
            commentCol.Name = "childtag";
            commentCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridView1.Columns.AddRange(new DataGridViewColumn[] { stringCol, commentCol });

            var i18nCol = new DataGridViewTextBoxColumn();
            i18nCol.HeaderText = referenceFile;
            i18nCol.Name = referenceFile;
            i18nCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridView1.Columns.Add(i18nCol);

            for (var i = 0; i < iI18nFiles.Length; i++)
            {
                if (referenceFile != iI18nFiles[i])
                {
                    i18nCol = new DataGridViewTextBoxColumn();
                    i18nCol.HeaderText = iI18nFiles[i];
                    i18nCol.Name = iI18nFiles[i];
                    i18nCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    this.dataGridView1.Columns.Add(i18nCol);
                }
            }
        }

        private void ConstructDGVData(string referenceFile)
        {
            this.mI18nDictionary = new List<List<KeyValuePair<string, I18n>>>();
            var reference = mI18n.Where(x => x.Contains(referenceFile)).First();
            this.mI18nDictionary.Add(_translationParser.ReadFile(reference)); //start with reference file
            foreach (var s in mI18n)
            {
                if (reference != s)
                {
                    this.mI18nDictionary.Add(_translationParser.ReadFile(s)); //do the rest
                }
            }
            //construct tags file based on primary i18n file
            foreach (KeyValuePair<string, I18n> p in this.mI18nDictionary[0])
            {
                DataGridViewRow row = (DataGridViewRow)this.dataGridView1.Rows[0].Clone();

                row.Cells[0].Value = p.Key;
                if (p.Key != p.Value.Key)
                {
                    row.Cells[1].Value = p.Value.Key;
                }
                this.dataGridView1.Rows.Add(row);
            }
            PopulateAndFillFields();
            this.button3.Visible = true;
            this.AutoTranslate.Visible = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var saveToFolder = folderBrowserDialog1.SelectedPath;
            var tags = (from row in this.dataGridView1.Rows.Cast<DataGridViewRow>() select row.Cells[0].Value).Cast<string>().ToList();
            var comments = (from row in this.dataGridView1.Rows.Cast<DataGridViewRow>() select row.Cells[1].Value).Cast<string>().ToList();
            var i18nFileNo = 0;
            foreach (var s in this.mI18nFiles)
            {
                var i18nRow = (from row in this.dataGridView1.Rows.Cast<DataGridViewRow>() select row.Cells[i18nFileNo + 2].Value).Cast<string>().ToList();
                var i18n = tags.Zip(comments.Zip(i18nRow, (b, c) => new { b, c }), (a, b) => new I18n { Key = a, Comment = b.b, Value = b.c });

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

        private Translation[] TranslateAsync(string text)
        {
            StringBuilder builder = new StringBuilder();
            var referenceI18n = this.mI18nFiles[this.listBox1.SelectedIndex];

            for (int i = 0; i < mI18n.Length; i++)
            {
                if (!mI18n[i].Contains(referenceI18n))
                {
                    var fileName = Path.GetFileName(mI18n[i]);
                    builder.Append($"to={fileName.Split('-')[0]}");
                    if (i != mI18n.Length - 1)
                    {
                        builder.Append("&");
                    }
                }
            }
            // This is our main function.
            // Output languages are defined in the route.
            // For a complete list of options, see API reference.
            // https://docs.microsoft.com/azure/cognitive-services/translator/reference/v3-0-translate
            string route = $"/translate?api-version=3.0&{builder.ToString()}";
            var results = MSTranslatorApi.TranslateTextRequest(subscriptionKey, endpoint, route, text).Result;
            return results.FirstOrDefault().Translations;
        }

        private void AutoTranslate_Click(object sender, EventArgs e)
        {
            PopulateAndFillFields(true);
        }

        private void PopulateAndFillFields(bool translate = false)
        {
            //will dynamically generate i18n properties
            var i18nFileNo = 0;


            foreach (List<KeyValuePair<string, I18n>> allI18n in this.mI18nDictionary)
            {
                var groupedList = allI18n.GroupBy(kvp => kvp.Key).ToList();
                foreach (var row in this.dataGridView1.Rows.Cast<DataGridViewRow>())
                {
                    var rootKey = (string)row.Cells[0].Value;
                    var childRootKey = (string)row.Cells[1].Value;
                    if (string.IsNullOrEmpty(rootKey))
                    {
                        //might need to highlight red here
                    }
                    if (!string.IsNullOrEmpty(rootKey) && string.IsNullOrEmpty(childRootKey))
                    {
                        var val = groupedList.Where(x => x.Key == rootKey).FirstOrDefault();
                        if (val != null)
                        {
                            row.Cells[i18nFileNo + 2].Value = val.First().Value.Value;
                        }
                        else
                        {
                            if (!translate)
                            {
                                row.Cells[i18nFileNo + 2].Value = ""; //highlight red it's missing 
                                row.Cells[i18nFileNo + 2].Style.BackColor = Color.Red;
                            }
                            else
                            {
                                var locale = val.FirstOrDefault().Value.Locale;
                                var defaultLangSelectText = row.Cells[2].Value;
                                var results = TranslateAsync((string)defaultLangSelectText);
                                row.Cells[i18nFileNo + 2].Value = results.Where(x => x.To == locale).First().Text;
                                row.Cells[i18nFileNo + 2].Style.BackColor = Color.Green;
                            }
                        }
                    }
                    else
                    {
                        //search for rootKey and childRootKey and prepopulate, if nothing leaving empty
                        var val = groupedList.Where(x => x.Key == rootKey).FirstOrDefault();
                        if (val != null)
                        {
                            var val2 = val.Where(x => x.Value.Key == childRootKey).FirstOrDefault();
                            if (val2.Key != null)
                            {
                                row.Cells[i18nFileNo + 2].Value = val2.Value.Value;
                            }
                            else
                            {
                                if (!translate)
                                {
                                    row.Cells[i18nFileNo + 2].Value = ""; //highlight red it's missing 
                                    row.Cells[i18nFileNo + 2].Style.BackColor = Color.Red;
                                }
                                else
                                {
                                    var locale = val.FirstOrDefault().Value.Locale;
                                    var defaultLangSelectText = row.Cells[2].Value;
                                    var results = TranslateAsync((string)defaultLangSelectText);
                                    row.Cells[i18nFileNo + 2].Value = results.Where(x => x.To == locale).First().Text;
                                    row.Cells[i18nFileNo + 2].Style.BackColor = Color.Green;
                                }

                            }
                        }
                    }

                }
                i18nFileNo++;
            }
        }
    }
}
