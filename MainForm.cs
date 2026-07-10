using System.ComponentModel;
using System.Collections.Specialized;
using System.Reflection;

public sealed class MainForm : Form
{
    private const string ReleaseDate = "2026-07-10";
    private readonly TextBox _chromePathBox = new();
    private readonly NumericUpDown _portBox = new();
    private readonly TextBox _saveFolderBox = new();
    private readonly TextBox _productInputBox = new();
    private readonly Button _initButton = new();
    private readonly Button _startButton = new();
    private readonly Button _browseChromeButton = new();
    private readonly Button _autoChromeButton = new();
    private readonly Button _browseSaveButton = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _logBox = new();
    private readonly BindingList<TaskItem> _tasks = new();
    private readonly TaobaoExtractor _extractor = new();

    private int _dragRowIndex = -1;
    private Point _dragStartPoint;

    public MainForm()
    {
        Text = "淘宝商品长图提取";
        Width = 1100;
        Height = 760;
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;

        BuildLayout();
        LoadDefaults();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 5,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(BuildChromePanel(), 0, 0);
        root.Controls.Add(BuildInputPanel(), 0, 1);
        root.Controls.Add(BuildTaskGrid(), 0, 2);
        root.Controls.Add(BuildLogBox(), 0, 3);
        root.Controls.Add(BuildFooter(), 0, 4);
    }

    private Control BuildChromePanel()
    {
        var panel = new GroupBox
        {
            Text = "1. 初始化 Chrome 登录",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 7,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.Controls.Add(layout);

        layout.Controls.Add(new Label { Text = "Chrome 路径", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _chromePathBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        layout.Controls.Add(_chromePathBox, 1, 0);

        _autoChromeButton.Text = "自动查找";
        _autoChromeButton.Click += (_, _) => AutoFindChrome();
        layout.Controls.Add(_autoChromeButton, 2, 0);

        _browseChromeButton.Text = "浏览";
        _browseChromeButton.Click += (_, _) => BrowseChrome();
        layout.Controls.Add(_browseChromeButton, 3, 0);

        layout.Controls.Add(new Label { Text = "端口", AutoSize = true, Anchor = AnchorStyles.Left }, 4, 0);
        _portBox.Minimum = 1024;
        _portBox.Maximum = 65535;
        _portBox.Width = 90;
        layout.Controls.Add(_portBox, 5, 0);

        _initButton.Text = "初始化";
        _initButton.Width = 110;
        _initButton.Click += async (_, _) => await InitializeChromeAsync();
        layout.Controls.Add(_initButton, 6, 0);

        var hint = new Label
        {
            Text = "初始化会启动 Chrome 并打开淘宝登录页；请在 Chrome 窗口中扫码登录。",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Padding = new Padding(0, 6, 0, 0)
        };
        layout.SetColumnSpan(hint, 7);
        layout.Controls.Add(hint, 0, 1);

        return panel;
    }

    private Control BuildInputPanel()
    {
        var panel = new GroupBox
        {
            Text = "2. 输入商品ID或链接并获取长图",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 6,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.Controls.Add(layout);

        layout.Controls.Add(new Label { Text = "商品ID/链接", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _productInputBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        layout.Controls.Add(_productInputBox, 1, 0);

        _startButton.Text = "开始获取长图";
        _startButton.Width = 130;
        _startButton.Click += async (_, _) => await StartExtractAsync();
        layout.Controls.Add(_startButton, 2, 0);

        layout.Controls.Add(new Label { Text = "保存文件夹", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _saveFolderBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        layout.SetColumnSpan(_saveFolderBox, 2);
        layout.Controls.Add(_saveFolderBox, 1, 1);

        _browseSaveButton.Text = "浏览";
        _browseSaveButton.Click += (_, _) => BrowseSaveFolder();
        layout.Controls.Add(_browseSaveButton, 3, 1);

        var openFolder = new Button { Text = "打开文件夹", Width = 100 };
        openFolder.Click += (_, _) => OpenSaveFolder();
        layout.Controls.Add(openFolder, 4, 1);

        return panel;
    }

    private Control BuildTaskGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.DataSource = _tasks;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "状态", DataPropertyName = nameof(TaskItem.Status), Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品ID", DataPropertyName = nameof(TaskItem.ProductId), Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品名称", DataPropertyName = nameof(TaskItem.ProductName), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "图片文件", DataPropertyName = nameof(TaskItem.ImageFile), Width = 260 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "说明", DataPropertyName = nameof(TaskItem.Message), Width = 220 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "创建时间", DataPropertyName = nameof(TaskItem.CreatedAt), Width = 150 });
        _grid.MouseDown += GridMouseDown;
        _grid.MouseMove += GridMouseMove;
        _grid.CellDoubleClick += GridCellDoubleClick;
        return _grid;
    }

    private Control BuildLogBox()
    {
        _logBox.Dock = DockStyle.Fill;
        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        return _logBox;
    }

    private Control BuildFooter()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        panel.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = "完成任务可从列表拖拽到本地文件夹，系统会复制对应图片文件。"
        }, 0, 0);

        panel.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = $"版本：{GetVersion()}  日期：{ReleaseDate}"
        }, 1, 0);

        return panel;
    }

    private void LoadDefaults()
    {
        _chromePathBox.Text = ChromeHelper.FindChromePath() ?? "";
        _portBox.Value = ChromeHelper.FindAvailablePort();
        _saveFolderBox.Text = Path.Combine(AppContext.BaseDirectory, "downloads");
    }

    private void AutoFindChrome()
    {
        var chrome = ChromeHelper.FindChromePath();
        if (chrome is null)
        {
            MessageBox.Show("未找到 Chrome，请手动选择 chrome.exe。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _chromePathBox.Text = chrome;
    }

    private void BrowseChrome()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Chrome|chrome.exe|可执行文件|*.exe|所有文件|*.*",
            Title = "选择 chrome.exe"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _chromePathBox.Text = dialog.FileName;
        }
    }

    private void BrowseSaveFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "选择图片保存文件夹" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _saveFolderBox.Text = dialog.SelectedPath;
        }
    }

    private async Task InitializeChromeAsync()
    {
        try
        {
            _initButton.Enabled = false;
            if (string.IsNullOrWhiteSpace(_chromePathBox.Text) || !File.Exists(_chromePathBox.Text))
            {
                throw new FileNotFoundException("Chrome 路径无效，请先选择 chrome.exe。");
            }

            var port = (int)_portBox.Value;
            if (await ChromeHelper.CanConnectToDebugPortAsync(port))
            {
                AppendLog($"已连接现有 Chrome，端口：{port}");
                await ChromeHelper.OpenLoginPageInExistingChromeAsync(port);
                AppendLog($"已在现有 Chrome 中打开淘宝登录页：{ChromeHelper.LoginUrl}");
                return;
            }

            if (!ChromeHelper.IsPortAvailable(port))
            {
                port = ChromeHelper.FindAvailablePort(port + 1);
                _portBox.Value = port;
                AppendLog($"原端口不可用，已自动调整为：{port}");
            }

            ChromeHelper.StartChrome(_chromePathBox.Text, port);
            await ChromeHelper.WaitForDebugPortAsync(port, TimeSpan.FromSeconds(10));
            AppendLog($"Chrome 已启动，端口：{port}");
            AppendLog($"已打开淘宝登录页：{ChromeHelper.LoginUrl}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _initButton.Enabled = true;
        }
    }

    private async Task StartExtractAsync()
    {
        var input = _productInputBox.Text.Trim();
        if (input.Length == 0)
        {
            MessageBox.Show("请输入商品ID或商品链接。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = new TaskItem
        {
            Status = "进行中",
            ProductId = TaobaoExtractor.ExtractProductId(TaobaoExtractor.NormalizeInput(input)),
            ProductName = "",
            Message = "开始处理"
        };
        _tasks.Insert(0, item);
        _grid.ClearSelection();
        _grid.Rows[0].Selected = true;

        ToggleWorkControls(false);
        try
        {
            var progress = new Progress<string>(message =>
            {
                item.Message = message;
                AppendLog(message);
                _grid.Refresh();
            });

            var result = await _extractor.ExtractAsync(
                input,
                (int)_portBox.Value,
                _saveFolderBox.Text,
                progress);

            item.Status = "完成";
            item.ProductId = result.ProductId;
            item.ProductName = result.ProductName;
            item.ImageFile = result.OutputFile;
            item.Message = result.UsedFallback ? "完成：使用详情页主图兜底" : "完成：店铺搜索页命中";
            AppendLog(item.Message);
        }
        catch (Exception ex)
        {
            item.Status = "失败";
            item.Message = ex.Message;
            AppendLog($"失败：{ex.Message}");
        }
        finally
        {
            _grid.Refresh();
            ToggleWorkControls(true);
        }
    }

    private void ToggleWorkControls(bool enabled)
    {
        _startButton.Enabled = enabled;
        _initButton.Enabled = enabled;
        _browseChromeButton.Enabled = enabled;
        _autoChromeButton.Enabled = enabled;
        _browseSaveButton.Enabled = enabled;
    }

    private void OpenSaveFolder()
    {
        Directory.CreateDirectory(_saveFolderBox.Text);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _saveFolderBox.Text,
            UseShellExecute = true
        });
    }

    private void GridMouseDown(object? sender, MouseEventArgs e)
    {
        var hit = _grid.HitTest(e.X, e.Y);
        if (hit.RowIndex < 0)
        {
            _dragRowIndex = -1;
            return;
        }

        _dragRowIndex = hit.RowIndex;
        _dragStartPoint = e.Location;
        _grid.ClearSelection();
        _grid.Rows[hit.RowIndex].Selected = true;
        if (hit.ColumnIndex >= 0)
        {
            _grid.CurrentCell = _grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
        }
    }

    private void GridMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _dragRowIndex < 0) return;
        var dragSize = SystemInformation.DragSize;
        var dragRect = new Rectangle(
            _dragStartPoint.X - dragSize.Width / 2,
            _dragStartPoint.Y - dragSize.Height / 2,
            dragSize.Width,
            dragSize.Height);
        if (dragRect.Contains(e.Location)) return;

        if (_grid.Rows[_dragRowIndex].DataBoundItem is not TaskItem item) return;
        if (item.Status != "完成" || string.IsNullOrWhiteSpace(item.ImageFile) || !File.Exists(item.ImageFile)) return;

        var data = CreateFileDragData(item.ImageFile);
        _grid.DoDragDrop(data, DragDropEffects.Copy);
        _dragRowIndex = -1;
    }

    private void GridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (_grid.Rows[e.RowIndex].DataBoundItem is not TaskItem item) return;

        if (!TryCreateFileDataObject(item, out var data, out var error))
        {
            MessageBox.Show(error, "无法复制", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Clipboard.SetDataObject(data, true);
        AppendLog($"已复制图片文件到剪贴板：{item.ImageFile}");
        item.Message = "已复制到剪贴板，可到文件夹中粘贴";
        _grid.Refresh();
    }

    private void AppendLog(string message)
    {
        _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private static DataObject CreateFileDragData(string filePath)
    {
        var files = new StringCollection { filePath };
        var data = new DataObject();
        data.SetFileDropList(files);
        data.SetData(DataFormats.FileDrop, true, new[] { filePath });
        data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)DragDropEffects.Copy)));
        return data;
    }

    private static bool TryCreateFileDataObject(TaskItem item, out DataObject data, out string error)
    {
        data = new DataObject();
        error = "";

        if (item.Status != "完成")
        {
            error = "只有完成状态的任务才能复制图片。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.ImageFile))
        {
            error = "该任务没有图片文件路径。";
            return false;
        }

        if (!File.Exists(item.ImageFile))
        {
            error = "图片文件不存在，可能已被移动或删除。";
            return false;
        }

        data = CreateFileDragData(item.ImageFile);
        return true;
    }

    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0";
    }
}
