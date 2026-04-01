// ============================================================
//  SmartZip.cs - Smart Folder Zipper with Tree Exclusion
//  Single-file C# WinForms Application
//  Author: Built for WALL MAX FF / Harry
//  Usage: Compile with: csc SmartZip.cs /target:winexe /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll
//         Or add to a WinForms project and set as startup
// ============================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartZip
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // ─────────────────────────────────────────────────────────
    //  MAIN FORM
    // ─────────────────────────────────────────────────────────
    public class MainForm : Form
    {
        // ── Zip Tab Controls ──
        private Panel pnlHeader;
        private Label lblTitle, lblSubtitle;
        private TabControl tabMain;
        private TabPage tabZip, tabUnzip, tabAbout;

        // Zip tab
        private GroupBox grpSource, grpTree, grpOutput, grpQuickExclude;
        private TextBox txtSource;
        private DarkButton btnBrowseSource, btnRefreshTree;
        private TreeView tvFolders;
        private DarkButton btnSelectAll, btnDeselectAll, btnExpandAll, btnCollapseAll;
        private TextBox txtOutput;
        private DarkButton btnBrowseOutput;
        private DarkButton btnZip;
        private ProgressBar prgZip;
        private Label lblZipStatus, lblStats;
        private Panel pnlZipBottom;
        private CheckedListBox lstPresets;
        private DarkButton btnApplyPresets;
        private Label lblPresetsTitle;

        // Unzip tab
        private GroupBox grpZipFile, grpUnzipOut;
        private TextBox txtZipFile, txtUnzipDest;
        private DarkButton btnBrowseZip, btnBrowseUnzipDest;
        private DarkButton btnUnzip;
        private ProgressBar prgUnzip;
        private Label lblUnzipStatus;
        private TreeView tvZipContents;
        private GroupBox grpZipContents;

        // State
        private string _sourceFolder = "";
        private bool _suppressCheckEvents = false;
        private CancellationTokenSource _cts;

        // Colors
        private static readonly Color BG       = Color.FromArgb(22, 22, 28);
        private static readonly Color BG2      = Color.FromArgb(30, 30, 38);
        private static readonly Color BG3      = Color.FromArgb(40, 40, 52);
        private static readonly Color ACCENT   = Color.FromArgb(99, 102, 241);
        private static readonly Color ACCENT2  = Color.FromArgb(139, 92, 246);
        private static readonly Color SUCCESS  = Color.FromArgb(34, 197, 94);
        private static readonly Color DANGER   = Color.FromArgb(239, 68, 68);
        private static readonly Color WARNING  = Color.FromArgb(234, 179, 8);
        private static readonly Color FG       = Color.FromArgb(226, 232, 240);
        private static readonly Color FG2      = Color.FromArgb(148, 163, 184);
        private static readonly Color BORDER   = Color.FromArgb(55, 65, 81);

        // Quick exclude presets
        private static readonly string[] PRESETS = {
            "node_modules", ".git", ".vs", ".idea",
            "bin", "obj", "__pycache__", "dist",
            "build", ".next", ".nuxt", "coverage",
            "logs", "temp", "tmp", ".gradle", "vendor",
            "target", ".svn", ".hg", "Thumbs.db", ".DS_Store"
        };

        // ─────────────────────────────────────────────────────
        public MainForm()
        {
            this.Text = "SmartZip  •  Smart Folder Zipper";
            this.Size = new Size(1000, 760);
            this.MinimumSize = new Size(860, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BG;
            this.ForeColor = FG;
            this.Font = new Font("Segoe UI", 9f);
            this.Icon = CreateAppIcon();

            BuildUI();
            PopulatePresets();
        }

        // ─────────────────────────────────────────────────────
        //  BUILD UI
        // ─────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Header ──
            pnlHeader = new Panel {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = BG2,
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlHeader.Paint += (s, e) => {
                using var pen = new Pen(ACCENT, 2);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            lblTitle = new Label {
                Text = "⚡ SmartZip",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = FG,
                AutoSize = true,
                Location = new Point(20, 10)
            };
            lblSubtitle = new Label {
                Text = "Zip any folder, skip what you don't need — node_modules, .git, junk — safely.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = FG2,
                AutoSize = true,
                Location = new Point(22, 44)
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle });

            // ── Tab Control ──
            tabMain = new TabControl {
                Dock = DockStyle.Fill,
                Padding = new Point(16, 6),
                BackColor = BG,
                ForeColor = FG
            };
            tabMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabMain.DrawItem += DrawTab;
            tabMain.ItemSize = new Size(120, 36);

            tabZip    = new TabPage("📦  Zip Folder") { BackColor = BG, ForeColor = FG };
            tabUnzip  = new TabPage("📂  Unzip File") { BackColor = BG, ForeColor = FG };
            tabAbout  = new TabPage("ℹ️  About")       { BackColor = BG, ForeColor = FG };

            BuildZipTab();
            BuildUnzipTab();
            BuildAboutTab();

            tabMain.TabPages.Add(tabZip);
            tabMain.TabPages.Add(tabUnzip);
            tabMain.TabPages.Add(tabAbout);

            this.Controls.Add(tabMain);
            this.Controls.Add(pnlHeader);
        }

        // ─────────────────────────────────────────────────────
        //  ZIP TAB
        // ─────────────────────────────────────────────────────
        private void BuildZipTab()
        {
            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BG,
                Padding = new Padding(10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));

            // ── Left panel ──
            var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = BG };

            // Source folder group
            grpSource = MakeGroupBox("📁  Source Folder", new Rectangle(0, 0, 600, 60));
            grpSource.Dock = DockStyle.Top;
            grpSource.Height = 64;

            txtSource = MakeTextBox(new Rectangle(10, 22, 480, 28));
            txtSource.PlaceholderText = "Select a folder to zip...";
            txtSource.Dock = DockStyle.None;
            txtSource.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            txtSource.SetBounds(10, 25, 1, 28);  // width set in resize

            btnBrowseSource = new DarkButton("Browse") { Width = 90, Height = 28 };
            btnBrowseSource.Click += BrowseSource_Click;

            btnRefreshTree = new DarkButton("↺ Refresh") { Width = 90, Height = 28 };
            btnRefreshTree.Click += (s, e) => LoadTree(_sourceFolder);
            btnRefreshTree.BackColor = BG3;

            grpSource.Controls.AddRange(new Control[] { txtSource, btnBrowseSource, btnRefreshTree });
            grpSource.Resize += (s, e) => {
                int w = grpSource.Width - btnBrowseSource.Width - btnRefreshTree.Width - 30;
                txtSource.SetBounds(10, 28, w, 28);
                btnBrowseSource.SetBounds(w + 14, 28, 90, 28);
                btnRefreshTree.SetBounds(w + 108, 28, 90, 28);
            };

            // Tree group
            grpTree = MakeGroupBox("🌳  Folder Tree  —  Uncheck folders/files you want to EXCLUDE from the zip", new Rectangle(0, 70, 600, 400));
            grpTree.Dock = DockStyle.Fill;

            // Tree toolbar
            var treeBar = new Panel { Height = 36, Dock = DockStyle.Top, BackColor = BG2, Padding = new Padding(4, 4, 4, 4) };
            btnSelectAll   = new DarkButton("✔ Select All")   { Width = 100, Height = 26, Left = 4,   Top = 5 };
            btnDeselectAll = new DarkButton("✖ Deselect All") { Width = 110, Height = 26, Left = 108, Top = 5, BackColor = Color.FromArgb(60, 30, 30) };
            btnExpandAll   = new DarkButton("⊞ Expand All")   { Width = 100, Height = 26, Left = 222, Top = 5, BackColor = BG3 };
            btnCollapseAll = new DarkButton("⊟ Collapse")     { Width = 100, Height = 26, Left = 326, Top = 5, BackColor = BG3 };
            lblStats = new Label { Text = "No folder loaded", ForeColor = FG2, AutoSize = true, Top = 10, Left = 440 };

            btnSelectAll.Click   += (s, e) => SetAllNodes(tvFolders.Nodes, true);
            btnDeselectAll.Click += (s, e) => SetAllNodes(tvFolders.Nodes, false);
            btnExpandAll.Click   += (s, e) => tvFolders.ExpandAll();
            btnCollapseAll.Click += (s, e) => tvFolders.CollapseAll();

            treeBar.Controls.AddRange(new Control[] { btnSelectAll, btnDeselectAll, btnExpandAll, btnCollapseAll, lblStats });

            tvFolders = new TreeView {
                Dock = DockStyle.Fill,
                BackColor = BG3,
                ForeColor = FG,
                CheckBoxes = true,
                HideSelection = false,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                ImageList = MakeImageList(),
                ShowLines = true,
                LineColor = BORDER,
                ShowNodeToolTips = true
            };
            tvFolders.AfterCheck += TreeNode_AfterCheck;

            grpTree.Controls.Add(tvFolders);
            grpTree.Controls.Add(treeBar);

            // Output + Zip button
            var bottomPanel = new Panel { Height = 100, Dock = DockStyle.Bottom, BackColor = BG, Padding = new Padding(4) };

            grpOutput = MakeGroupBox("💾  Output Zip File", new Rectangle(0, 0, 400, 60));
            grpOutput.Dock = DockStyle.Fill;

            txtOutput = MakeTextBox(new Rectangle(10, 28, 1, 28));
            txtOutput.PlaceholderText = "Choose where to save the .zip...";

            btnBrowseOutput = new DarkButton("Browse") { Width = 90, Height = 28 };
            btnBrowseOutput.Click += BrowseOutput_Click;

            grpOutput.Controls.AddRange(new Control[] { txtOutput, btnBrowseOutput });
            grpOutput.Resize += (s, e) => {
                int w = grpOutput.Width - 110;
                txtOutput.SetBounds(10, 28, w, 28);
                btnBrowseOutput.SetBounds(w + 14, 28, 90, 28);
            };

            btnZip = new DarkButton("  ⚡  Create ZIP  ") {
                Width = 160, Height = 42, BackColor = ACCENT,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Right
            };
            btnZip.Click += BtnZip_Click;

            var outputRow = new TableLayoutPanel {
                Dock = DockStyle.Top,
                Height = 62,
                ColumnCount = 2,
                BackColor = BG,
                Padding = new Padding(0)
            };
            outputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            outputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
            outputRow.Controls.Add(grpOutput, 0, 0);
            outputRow.Controls.Add(btnZip, 1, 0);

            prgZip = new ProgressBar {
                Dock = DockStyle.Top,
                Height = 18,
                Style = ProgressBarStyle.Continuous,
                BackColor = BG3,
                ForeColor = ACCENT,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                MarqueeAnimationSpeed = 30
            };

            lblZipStatus = new Label {
                Dock = DockStyle.Bottom,
                Height = 22,
                Text = "Ready. Select a folder and choose what to include.",
                ForeColor = FG2,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            bottomPanel.Controls.Add(outputRow);
            bottomPanel.Controls.Add(prgZip);
            bottomPanel.Controls.Add(lblZipStatus);

            leftPanel.Controls.Add(grpTree);
            leftPanel.Controls.Add(grpSource);
            leftPanel.Controls.Add(bottomPanel);

            // ── Right panel: Quick Exclude Presets ──
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = BG, Padding = new Padding(6, 0, 0, 0) };

            grpQuickExclude = MakeGroupBox("🚫  Quick Exclude Presets", new Rectangle(0, 0, 240, 400));
            grpQuickExclude.Dock = DockStyle.Fill;

            var presetsNote = new Label {
                Text = "Select presets and click\n\"Apply Exclusions\" to auto-\nuncheck matching folders.",
                ForeColor = FG2,
                Font = new Font("Segoe UI", 8.5f),
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(6, 6, 6, 0)
            };

            lstPresets = new CheckedListBox {
                Dock = DockStyle.Fill,
                BackColor = BG3,
                ForeColor = FG,
                BorderStyle = BorderStyle.None,
                CheckOnClick = true,
                Font = new Font("Segoe UI", 9f)
            };

            var presetBtnPanel = new Panel { Height = 80, Dock = DockStyle.Bottom, BackColor = BG2 };
            btnApplyPresets = new DarkButton("🚫  Apply Exclusions") {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(100, 40, 40),
                ForeColor = Color.White
            };
            btnApplyPresets.Click += ApplyPresets_Click;

            var btnSelectAllPresets  = new DarkButton("Check All")   { Height = 28, Dock = DockStyle.Left,  Width = 90, BackColor = BG3 };
            var btnClearAllPresets   = new DarkButton("Clear All")    { Height = 28, Dock = DockStyle.Right, Width = 90, BackColor = BG3 };
            btnSelectAllPresets.Click += (s, e)  => { for (int i = 0; i < lstPresets.Items.Count; i++) lstPresets.SetItemChecked(i, true); };
            btnClearAllPresets.Click  += (s, e)  => { for (int i = 0; i < lstPresets.Items.Count; i++) lstPresets.SetItemChecked(i, false); };

            var presetSubBar = new Panel { Height = 30, Dock = DockStyle.Top, BackColor = BG2 };
            presetSubBar.Controls.Add(btnSelectAllPresets);
            presetSubBar.Controls.Add(btnClearAllPresets);

            presetBtnPanel.Controls.Add(btnApplyPresets);
            presetBtnPanel.Controls.Add(presetSubBar);

            grpQuickExclude.Controls.Add(lstPresets);
            grpQuickExclude.Controls.Add(presetsNote);
            grpQuickExclude.Controls.Add(presetBtnPanel);

            // Estimated size panel
            var grpSize = MakeGroupBox("📊  Estimate", new Rectangle(0, 0, 240, 110));
            grpSize.Dock = DockStyle.Bottom;
            grpSize.Height = 110;
            lblPresetsTitle = new Label {
                Dock = DockStyle.Fill,
                ForeColor = FG2,
                Font = new Font("Segoe UI", 8.5f),
                Text = "Load a folder to see\nincluded/excluded\nfile estimates.",
                Padding = new Padding(8),
                TextAlign = ContentAlignment.TopLeft
            };
            grpSize.Controls.Add(lblPresetsTitle);

            rightPanel.Controls.Add(grpQuickExclude);
            rightPanel.Controls.Add(grpSize);

            layout.Controls.Add(leftPanel, 0, 0);
            layout.Controls.Add(rightPanel, 1, 0);

            tabZip.Controls.Add(layout);
        }

        // ─────────────────────────────────────────────────────
        //  UNZIP TAB
        // ─────────────────────────────────────────────────────
        private void BuildUnzipTab()
        {
            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = BG,
                Padding = new Padding(10)
            };

            // Zip file selection
            grpZipFile = MakeGroupBox("📦  Select ZIP File", new Rectangle(0, 0, 700, 60));
            grpZipFile.Dock = DockStyle.Top;
            grpZipFile.Height = 64;

            txtZipFile = MakeTextBox(new Rectangle(10, 28, 1, 28));
            txtZipFile.PlaceholderText = "Browse for a .zip file to extract...";
            txtZipFile.TextChanged += (s, e) => PreviewZipContents(txtZipFile.Text.Trim());

            btnBrowseZip = new DarkButton("Browse ZIP") { Width = 110, Height = 28 };
            btnBrowseZip.Click += BrowseZip_Click;

            grpZipFile.Controls.AddRange(new Control[] { txtZipFile, btnBrowseZip });
            grpZipFile.Resize += (s, e) => {
                int w = grpZipFile.Width - 130;
                txtZipFile.SetBounds(10, 28, w, 28);
                btnBrowseZip.SetBounds(w + 14, 28, 110, 28);
            };

            // Contents preview
            grpZipContents = MakeGroupBox("🔍  ZIP Contents Preview", new Rectangle(0, 0, 700, 200));
            grpZipContents.Dock = DockStyle.Fill;

            tvZipContents = new TreeView {
                Dock = DockStyle.Fill,
                BackColor = BG3,
                ForeColor = FG,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 8.5f),
                LineColor = BORDER,
                ImageList = MakeImageList()
            };
            grpZipContents.Controls.Add(tvZipContents);

            // Output destination
            grpUnzipOut = MakeGroupBox("📂  Extract To Folder", new Rectangle(0, 0, 700, 60));
            grpUnzipOut.Dock = DockStyle.Bottom;
            grpUnzipOut.Height = 64;

            txtUnzipDest = MakeTextBox(new Rectangle(10, 28, 1, 28));
            txtUnzipDest.PlaceholderText = "Choose destination folder for extraction...";

            btnBrowseUnzipDest = new DarkButton("Browse") { Width = 90, Height = 28 };
            btnBrowseUnzipDest.Click += BrowseUnzipDest_Click;

            grpUnzipOut.Controls.AddRange(new Control[] { txtUnzipDest, btnBrowseUnzipDest });
            grpUnzipOut.Resize += (s, e) => {
                int w = grpUnzipOut.Width - 110;
                txtUnzipDest.SetBounds(10, 28, w, 28);
                btnBrowseUnzipDest.SetBounds(w + 14, 28, 90, 28);
            };

            // Bottom controls
            var unzipBottom = new Panel { Height = 80, Dock = DockStyle.Bottom, BackColor = BG };

            btnUnzip = new DarkButton("  📂  Extract Files  ") {
                Width = 180, Height = 44,
                BackColor = SUCCESS,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.Black,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnUnzip.Click += BtnUnzip_Click;

            prgUnzip = new ProgressBar {
                Dock = DockStyle.Top,
                Height = 18,
                Style = ProgressBarStyle.Continuous,
                ForeColor = SUCCESS,
                Minimum = 0,
                Maximum = 100
            };

            lblUnzipStatus = new Label {
                Dock = DockStyle.Bottom,
                Height = 22,
                Text = "Select a ZIP file and destination folder, then click Extract.",
                ForeColor = FG2,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            unzipBottom.Controls.Add(prgUnzip);
            unzipBottom.Controls.Add(lblUnzipStatus);

            unzipBottom.Resize += (s, e) => {
                btnUnzip.Location = new Point(unzipBottom.Width - 200, 6);
            };
            unzipBottom.Controls.Add(btnUnzip);

            tabUnzip.Controls.Add(grpZipContents);
            tabUnzip.Controls.Add(grpZipFile);
            tabUnzip.Controls.Add(grpUnzipOut);
            tabUnzip.Controls.Add(unzipBottom);
        }

        // ─────────────────────────────────────────────────────
        //  ABOUT TAB
        // ─────────────────────────────────────────────────────
        private void BuildAboutTab()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = BG, Padding = new Padding(40) };

            var lbl = new Label {
                Text = "⚡  SmartZip\n\n" +
                       "Version 1.0  •  Built for WALL MAX FF\n\n" +
                       "─────────────────────────────────────────\n\n" +
                       "HOW TO USE:\n\n" +
                       "  📦  ZIP FOLDER:\n" +
                       "    1. Click Browse to select your source folder\n" +
                       "    2. The full folder tree loads with all files/folders checked\n" +
                       "    3. Uncheck anything you DON'T want in the zip\n" +
                       "       (e.g. node_modules, .git, bin, obj, etc.)\n" +
                       "    4. OR use Quick Exclude Presets on the right side\n" +
                       "       Select presets → Click 'Apply Exclusions'\n" +
                       "    5. Choose output .zip file path\n" +
                       "    6. Click 'Create ZIP'\n\n" +
                       "  📂  UNZIP FILE:\n" +
                       "    1. Browse for your .zip file\n" +
                       "    2. Preview its contents in the tree\n" +
                       "    3. Choose destination folder\n" +
                       "    4. Click 'Extract Files'\n\n" +
                       "─────────────────────────────────────────\n\n" +
                       "✅  100% SAFE — your original files are NEVER modified.\n" +
                       "    ZIP is always created separately. Nothing is deleted.\n\n" +
                       "─────────────────────────────────────────\n\n" +
                       "YouTube: WALL MAX FF\n" +
                       "Product: Cybercode",
                Font = new Font("Segoe UI", 10f),
                ForeColor = FG,
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = BG2,
                Padding = new Padding(30)
            };

            pnl.Controls.Add(lbl);
            tabAbout.Controls.Add(pnl);
        }

        // ─────────────────────────────────────────────────────
        //  TREE LOADING
        // ─────────────────────────────────────────────────────
        private void LoadTree(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

            tvFolders.BeginUpdate();
            tvFolders.Nodes.Clear();
            _suppressCheckEvents = true;

            try
            {
                var root = new TreeNode(Path.GetFileName(folder)) {
                    Tag = folder,
                    ImageIndex = 0,
                    SelectedImageIndex = 0,
                    Checked = true,
                    ToolTipText = folder
                };
                PopulateNode(root, folder, 0);
                tvFolders.Nodes.Add(root);
                root.Expand();
                UpdateStats();
                SetStatus("✅ Folder loaded. Uncheck items to exclude from ZIP.", SUCCESS);
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Error loading folder: {ex.Message}", DANGER);
            }
            finally
            {
                _suppressCheckEvents = false;
                tvFolders.EndUpdate();
            }
        }

        private void PopulateNode(TreeNode parent, string path, int depth)
        {
            if (depth > 15) return; // safety limit

            try
            {
                // Directories first
                foreach (var dir in Directory.GetDirectories(path).OrderBy(d => d))
                {
                    var name = Path.GetFileName(dir);
                    var node = new TreeNode(name) {
                        Tag = dir,
                        ImageIndex = 0,
                        SelectedImageIndex = 0,
                        Checked = true,
                        ToolTipText = dir
                    };
                    parent.Nodes.Add(node);
                    PopulateNode(node, dir, depth + 1);
                }

                // Files
                foreach (var file in Directory.GetFiles(path).OrderBy(f => f))
                {
                    var name = Path.GetFileName(file);
                    var info = new FileInfo(file);
                    var node = new TreeNode($"{name}  ({FormatSize(info.Length)})") {
                        Tag = file,
                        ImageIndex = 1,
                        SelectedImageIndex = 1,
                        Checked = true,
                        ForeColor = FG2,
                        ToolTipText = $"{file}\n{FormatSize(info.Length)}"
                    };
                    parent.Nodes.Add(node);
                }
            }
            catch { /* skip inaccessible */ }
        }

        // ─────────────────────────────────────────────────────
        //  TREE CHECK EVENTS
        // ─────────────────────────────────────────────────────
        private void TreeNode_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_suppressCheckEvents) return;
            if (e.Action == TreeViewAction.Unknown) return;

            _suppressCheckEvents = true;
            try
            {
                // Propagate down to children
                SetChildrenChecked(e.Node, e.Node.Checked);
                // Update parent states
                UpdateParentCheck(e.Node.Parent);
                UpdateStats();
            }
            finally
            {
                _suppressCheckEvents = false;
            }
        }

        private void SetChildrenChecked(TreeNode node, bool check)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = check;
                SetChildrenChecked(child, check);
            }
        }

        private void UpdateParentCheck(TreeNode parent)
        {
            if (parent == null) return;
            bool anyChecked = parent.Nodes.Cast<TreeNode>().Any(n => n.Checked);
            parent.Checked = anyChecked;
            UpdateParentCheck(parent.Parent);
        }

        private void SetAllNodes(TreeNodeCollection nodes, bool check)
        {
            _suppressCheckEvents = true;
            try
            {
                SetAllNodesRecursive(nodes, check);
                UpdateStats();
            }
            finally
            {
                _suppressCheckEvents = false;
            }
        }

        private void SetAllNodesRecursive(TreeNodeCollection nodes, bool check)
        {
            foreach (TreeNode node in nodes)
            {
                node.Checked = check;
                SetAllNodesRecursive(node.Nodes, check);
            }
        }

        // ─────────────────────────────────────────────────────
        //  QUICK EXCLUDE PRESETS
        // ─────────────────────────────────────────────────────
        private void PopulatePresets()
        {
            lstPresets.Items.Clear();
            foreach (var p in PRESETS)
                lstPresets.Items.Add(p);
            // Default checked: common ones
            var defaults = new[] { "node_modules", ".git", ".vs", "bin", "obj", "__pycache__", "dist", "build" };
            for (int i = 0; i < lstPresets.Items.Count; i++)
                if (defaults.Contains(lstPresets.Items[i].ToString()))
                    lstPresets.SetItemChecked(i, true);
        }

        private void ApplyPresets_Click(object sender, EventArgs e)
        {
            if (tvFolders.Nodes.Count == 0)
            {
                SetStatus("⚠️ Load a folder first, then apply presets.", WARNING);
                return;
            }

            var selectedPresets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in lstPresets.CheckedItems)
                selectedPresets.Add(item.ToString());

            if (selectedPresets.Count == 0)
            {
                SetStatus("⚠️ No presets selected.", WARNING);
                return;
            }

            _suppressCheckEvents = true;
            int excluded = 0;
            try
            {
                excluded = ApplyPresetsToNodes(tvFolders.Nodes, selectedPresets);
                UpdateStats();
            }
            finally
            {
                _suppressCheckEvents = false;
            }

            SetStatus($"✅ Applied presets — {excluded} folder(s) excluded. Review the tree and click Create ZIP.", SUCCESS);
        }

        private int ApplyPresetsToNodes(TreeNodeCollection nodes, HashSet<string> presets)
        {
            int count = 0;
            foreach (TreeNode node in nodes)
            {
                string name = Path.GetFileName(node.Tag?.ToString() ?? node.Text);
                if (presets.Contains(name))
                {
                    node.Checked = false;
                    SetChildrenChecked(node, false);
                    count++;
                }
                else
                {
                    count += ApplyPresetsToNodes(node.Nodes, presets);
                }
            }
            return count;
        }

        // ─────────────────────────────────────────────────────
        //  STATS UPDATE
        // ─────────────────────────────────────────────────────
        private void UpdateStats()
        {
            if (tvFolders.Nodes.Count == 0)
            {
                lblStats.Text = "No folder loaded";
                lblPresetsTitle.Text = "Load a folder first.";
                return;
            }

            long inclSize = 0, exclSize = 0;
            int inclFiles = 0, exclFiles = 0;
            CountNodes(tvFolders.Nodes, ref inclFiles, ref inclSize, ref exclFiles, ref exclSize);

            lblStats.Text = $"✅ {inclFiles} files ({FormatSize(inclSize)})   🚫 {exclFiles} excluded";
            lblPresetsTitle.Text =
                $"✅ Included:\n   {inclFiles} files\n   {FormatSize(inclSize)}\n\n" +
                $"🚫 Excluded:\n   {exclFiles} files\n   {FormatSize(exclSize)}";
        }

        private void CountNodes(TreeNodeCollection nodes, ref int inclFiles, ref long inclSize, ref int exclFiles, ref long exclSize)
        {
            foreach (TreeNode node in nodes)
            {
                string path = node.Tag?.ToString() ?? "";
                if (File.Exists(path))
                {
                    var info = new FileInfo(path);
                    if (node.Checked) { inclFiles++; inclSize += info.Length; }
                    else              { exclFiles++; exclSize += info.Length; }
                }
                else
                {
                    CountNodes(node.Nodes, ref inclFiles, ref inclSize, ref exclFiles, ref exclSize);
                }
            }
        }

        // ─────────────────────────────────────────────────────
        //  ZIP LOGIC
        // ─────────────────────────────────────────────────────
        private async void BtnZip_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_sourceFolder) || !Directory.Exists(_sourceFolder))
            {
                SetStatus("❌ Please select a valid source folder.", DANGER);
                return;
            }
            if (string.IsNullOrEmpty(txtOutput.Text.Trim()))
            {
                SetStatus("❌ Please choose an output .zip file path.", DANGER);
                return;
            }

            // Collect checked files
            var includedFiles = new List<string>();
            CollectCheckedFiles(tvFolders.Nodes, includedFiles);

            if (includedFiles.Count == 0)
            {
                SetStatus("⚠️ Nothing is selected to include in the ZIP!", WARNING);
                return;
            }

            string outputPath = txtOutput.Text.Trim();
            if (!outputPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                outputPath += ".zip";

            // Warn if file exists
            if (File.Exists(outputPath))
            {
                var result = MessageBox.Show(
                    $"File already exists:\n{outputPath}\n\nOverwrite it?",
                    "File Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes) return;
            }

            btnZip.Enabled = false;
            prgZip.Style = ProgressBarStyle.Marquee;
            prgZip.Value = 0;

            _cts = new CancellationTokenSource();

            try
            {
                int total = includedFiles.Count;
                int done = 0;

                await Task.Run(() =>
                {
                    string tempPath = outputPath + ".tmp";
                    try
                    {
                        using (var zip = ZipFile.Open(tempPath, ZipArchiveMode.Create))
                        {
                            foreach (var file in includedFiles)
                            {
                                _cts.Token.ThrowIfCancellationRequested();

                                // Entry name = relative path from source folder
                                string relativePath = file.Substring(_sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar);
                                zip.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);

                                done++;
                                int pct = (int)((double)done / total * 100);
                                this.BeginInvoke((Action)(() => {
                                    prgZip.Style = ProgressBarStyle.Continuous;
                                    prgZip.Value = pct;
                                    SetStatus($"⚡ Zipping... {done}/{total} files ({pct}%)", ACCENT);
                                }));
                            }
                        }

                        // Move temp → final (atomic-ish)
                        if (File.Exists(outputPath)) File.Delete(outputPath);
                        File.Move(tempPath, outputPath);
                    }
                    catch
                    {
                        if (File.Exists(tempPath)) File.Delete(tempPath);
                        throw;
                    }
                }, _cts.Token);

                prgZip.Value = 100;
                var finalInfo = new FileInfo(outputPath);
                SetStatus($"✅ ZIP created successfully!  {total} files  →  {FormatSize(finalInfo.Length)}   📁 {outputPath}", SUCCESS);

                if (MessageBox.Show($"ZIP created!\n\n{outputPath}\nSize: {FormatSize(finalInfo.Length)}\nFiles: {total}\n\nOpen folder?",
                        "Done!", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
            }
            catch (OperationCanceledException)
            {
                SetStatus("⚠️ ZIP cancelled.", WARNING);
                prgZip.Value = 0;
            }
            catch (Exception ex)
            {
                SetStatus($"❌ ZIP failed: {ex.Message}", DANGER);
                prgZip.Value = 0;
                MessageBox.Show($"Error creating ZIP:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnZip.Enabled = true;
                prgZip.Style = ProgressBarStyle.Continuous;
            }
        }

        private void CollectCheckedFiles(TreeNodeCollection nodes, List<string> files)
        {
            foreach (TreeNode node in nodes)
            {
                string path = node.Tag?.ToString() ?? "";
                if (File.Exists(path))
                {
                    if (node.Checked) files.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    if (node.Checked) CollectCheckedFiles(node.Nodes, files);
                    // if folder unchecked, skip all children
                }
            }
        }

        // ─────────────────────────────────────────────────────
        //  UNZIP LOGIC
        // ─────────────────────────────────────────────────────
        private void PreviewZipContents(string zipPath)
        {
            tvZipContents.Nodes.Clear();
            if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath)) return;

            try
            {
                using var zip = ZipFile.OpenRead(zipPath);
                var root = new TreeNode(Path.GetFileName(zipPath)) { ImageIndex = 0, SelectedImageIndex = 0 };
                var folders = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
                folders[""] = root;

                foreach (var entry in zip.Entries.OrderBy(e => e.FullName))
                {
                    string[] parts = entry.FullName.Replace('\\', '/').Split('/');
                    string currentPath = "";
                    TreeNode currentNode = root;

                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (string.IsNullOrEmpty(parts[i])) continue;
                        string prevPath = currentPath;
                        currentPath = string.IsNullOrEmpty(currentPath) ? parts[i] : currentPath + "/" + parts[i];

                        if (!folders.TryGetValue(currentPath, out var child))
                        {
                            bool isFile = (i == parts.Length - 1 && !entry.FullName.EndsWith("/"));
                            child = new TreeNode(isFile ? $"{parts[i]}  ({FormatSize(entry.Length)})" : parts[i]) {
                                ImageIndex = isFile ? 1 : 0,
                                SelectedImageIndex = isFile ? 1 : 0,
                                ForeColor = isFile ? FG2 : FG
                            };
                            if (folders.TryGetValue(prevPath, out var parent))
                                parent.Nodes.Add(child);
                            else
                                root.Nodes.Add(child);
                            folders[currentPath] = child;
                        }
                        currentNode = child;
                    }
                }

                tvZipContents.Nodes.Add(root);
                root.Expand();
                lblUnzipStatus.Text = $"✅ ZIP loaded: {zip.Entries.Count} entries";
            }
            catch (Exception ex)
            {
                lblUnzipStatus.Text = $"❌ Cannot read ZIP: {ex.Message}";
            }
        }

        private async void BtnUnzip_Click(object sender, EventArgs e)
        {
            string zipPath  = txtZipFile.Text.Trim();
            string destPath = txtUnzipDest.Text.Trim();

            if (!File.Exists(zipPath))       { lblUnzipStatus.Text = "❌ ZIP file not found."; return; }
            if (string.IsNullOrEmpty(destPath)) { lblUnzipStatus.Text = "❌ Choose a destination folder."; return; }

            // Safety: if dest has files, warn
            if (Directory.Exists(destPath) && Directory.GetFileSystemEntries(destPath).Length > 0)
            {
                var r = MessageBox.Show(
                    $"Destination folder is not empty:\n{destPath}\n\nFiles with the same name will be overwritten.\nContinue?",
                    "Destination Not Empty", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
            }

            Directory.CreateDirectory(destPath);

            btnUnzip.Enabled = false;
            prgUnzip.Style = ProgressBarStyle.Marquee;

            try
            {
                await Task.Run(() =>
                {
                    using var zip = ZipFile.OpenRead(zipPath);
                    int total = zip.Entries.Count;
                    int done = 0;

                    foreach (var entry in zip.Entries)
                    {
                        string fullDest = Path.Combine(destPath, entry.FullName.Replace('/', Path.DirectorySeparatorChar));

                        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                        {
                            Directory.CreateDirectory(fullDest);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fullDest));
                            entry.ExtractToFile(fullDest, overwrite: true);
                        }

                        done++;
                        int pct = (int)((double)done / total * 100);
                        this.BeginInvoke((Action)(() => {
                            prgUnzip.Style = ProgressBarStyle.Continuous;
                            prgUnzip.Value = pct;
                            lblUnzipStatus.Text = $"⚡ Extracting... {done}/{total} ({pct}%)";
                        }));
                    }
                });

                prgUnzip.Value = 100;
                lblUnzipStatus.Text = $"✅ Extraction complete!  →  {destPath}";

                if (MessageBox.Show($"Files extracted successfully!\n\n{destPath}\n\nOpen folder?",
                        "Done!", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    System.Diagnostics.Process.Start("explorer.exe", destPath);
            }
            catch (Exception ex)
            {
                lblUnzipStatus.Text = $"❌ Extraction failed: {ex.Message}";
                MessageBox.Show($"Error extracting ZIP:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnUnzip.Enabled = true;
                prgUnzip.Style = ProgressBarStyle.Continuous;
            }
        }

        // ─────────────────────────────────────────────────────
        //  BROWSE HANDLERS
        // ─────────────────────────────────────────────────────
        private void BrowseSource_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog {
                Description = "Select the folder you want to ZIP",
                ShowNewFolderButton = false
            };
            if (!string.IsNullOrEmpty(_sourceFolder)) fbd.SelectedPath = _sourceFolder;

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                _sourceFolder = fbd.SelectedPath;
                txtSource.Text = _sourceFolder;

                // Auto-suggest output path
                if (string.IsNullOrEmpty(txtOutput.Text))
                {
                    string parent = Path.GetDirectoryName(_sourceFolder);
                    string name   = Path.GetFileName(_sourceFolder);
                    txtOutput.Text = Path.Combine(parent ?? _sourceFolder, $"{name}_backup.zip");
                }

                SetStatus("⏳ Loading folder tree...", FG2);
                Application.DoEvents();
                LoadTree(_sourceFolder);
                ApplyPresets_Click(null, null);
            }
        }

        private void BrowseOutput_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog {
                Title = "Save ZIP File As",
                Filter = "ZIP Archive (*.zip)|*.zip",
                FileName = string.IsNullOrEmpty(_sourceFolder) ? "backup.zip" : Path.GetFileName(_sourceFolder) + "_backup.zip"
            };
            if (!string.IsNullOrEmpty(txtOutput.Text)) sfd.InitialDirectory = Path.GetDirectoryName(txtOutput.Text);
            if (sfd.ShowDialog() == DialogResult.OK) txtOutput.Text = sfd.FileName;
        }

        private void BrowseZip_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog {
                Title = "Select a ZIP File to Extract",
                Filter = "ZIP Archive (*.zip)|*.zip|All Files (*.*)|*.*"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtZipFile.Text = ofd.FileName;
                // Auto-suggest dest
                if (string.IsNullOrEmpty(txtUnzipDest.Text))
                {
                    string dir  = Path.GetDirectoryName(ofd.FileName);
                    string name = Path.GetFileNameWithoutExtension(ofd.FileName);
                    txtUnzipDest.Text = Path.Combine(dir, name + "_extracted");
                }
            }
        }

        private void BrowseUnzipDest_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog {
                Description = "Choose folder to extract into",
                ShowNewFolderButton = true
            };
            if (fbd.ShowDialog() == DialogResult.OK) txtUnzipDest.Text = fbd.SelectedPath;
        }

        // ─────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────
        private void SetStatus(string msg, Color color)
        {
            if (InvokeRequired) { BeginInvoke((Action)(() => SetStatus(msg, color))); return; }
            lblZipStatus.Text = msg;
            lblZipStatus.ForeColor = color;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.0} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024:0.00} MB";
            return $"{bytes / 1024.0 / 1024 / 1024:0.00} GB";
        }

        // ─────────────────────────────────────────────────────
        //  UI FACTORY HELPERS
        // ─────────────────────────────────────────────────────
        private GroupBox MakeGroupBox(string title, Rectangle bounds)
        {
            return new GroupBox {
                Text = title,
                ForeColor = ACCENT,
                BackColor = BG2,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Bounds = bounds,
                Padding = new Padding(6)
            };
        }

        private TextBox MakeTextBox(Rectangle bounds)
        {
            return new TextBox {
                BackColor = BG3,
                ForeColor = FG,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                Bounds = bounds
            };
        }

        private ImageList MakeImageList()
        {
            var il = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
            il.Images.Add("folder", CreateFolderIcon());
            il.Images.Add("file",   CreateFileIcon());
            return il;
        }

        private Bitmap CreateFolderIcon()
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            using var b = new SolidBrush(Color.FromArgb(251, 191, 36));
            g.FillRectangle(b, 0, 4, 16, 10);
            g.FillRectangle(b, 0, 2, 8, 4);
            return bmp;
        }

        private Bitmap CreateFileIcon()
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            using var b = new SolidBrush(Color.FromArgb(148, 163, 184));
            g.FillRectangle(b, 2, 0, 10, 14);
            using var w = new SolidBrush(Color.FromArgb(30, 30, 38));
            g.FillRectangle(w, 4, 3, 6, 1);
            g.FillRectangle(w, 4, 5, 6, 1);
            g.FillRectangle(w, 4, 7, 4, 1);
            return bmp;
        }

        private Icon CreateAppIcon()
        {
            try
            {
                var bmp = new Bitmap(32, 32);
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.FromArgb(99, 102, 241));
                using var f = new Font("Segoe UI", 16f, FontStyle.Bold);
                g.DrawString("⚡", f, Brushes.White, 2, 4);
                return Icon.FromHandle(bmp.GetHicon());
            }
            catch { return SystemIcons.Application; }
        }

        private void DrawTab(object sender, DrawItemEventArgs e)
        {
            var tab = (TabControl)sender;
            var page = tab.TabPages[e.Index];
            bool selected = (tab.SelectedIndex == e.Index);

            using var bgBrush = new SolidBrush(selected ? BG2 : BG);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            if (selected)
            {
                using var pen = new Pen(ACCENT, 2);
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            using var brush = new SolidBrush(selected ? FG : FG2);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(page.Text, new Font("Segoe UI", 9f, selected ? FontStyle.Bold : FontStyle.Regular), brush, e.Bounds, sf);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  DARK STYLED BUTTON
    // ─────────────────────────────────────────────────────────
    public class DarkButton : Button
    {
        private bool _hover = false;
        private static readonly Color DefaultBg = Color.FromArgb(60, 60, 78);

        public DarkButton(string text)
        {
            Text = text;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
            FlatAppearance.BorderSize = 1;
            BackColor = DefaultBg;
            ForeColor = Color.FromArgb(226, 232, 240);
            Font = new Font("Segoe UI", 9f);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            FlatAppearance.BorderColor = Color.FromArgb(99, 102, 241);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_hover)
            {
                using var b = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
                e.Graphics.FillRectangle(b, ClientRectangle);
            }
        }
    }
}