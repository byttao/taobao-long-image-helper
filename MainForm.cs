using System.ComponentModel;
using System.Collections.Specialized;
using System.Reflection;

public sealed class MainForm : Form
{
    private const string ReleaseDate = "2026-07-10";
    private const int AutoInputPanelHeight = 132;
    private const int ManualInputPanelHeight = 390;
    private readonly GroupBox _inputPanel = new();
    private readonly TabControl _modeTabs = new();
    private readonly TextBox _chromePathBox = new();
    private readonly NumericUpDown _portBox = new();
    private readonly TextBox _saveFolderBox = new();
    private readonly TextBox _productInputBox = new();
    private readonly TextBox _manualInputBox = new();
    private readonly TextBox _qianniuUrlBox = new();
    private readonly RichTextBox _productSourceBox = new();
    private readonly RichTextBox _shopSourceBox = new();
    private readonly TextBox _manualProductNameBox = new();
    private readonly TextBox _manualSellerIdBox = new();
    private readonly TextBox _manualShopIdBox = new();
    private readonly Button _initButton = new();
    private readonly Button _startButton = new();
    private readonly Button _convertQianniuUrlButton = new();
    private readonly Button _manualStartButton = new();
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
        _inputPanel.Text = "2. 选择获取方式";
        _inputPanel.Dock = DockStyle.Top;
        _inputPanel.Height = AutoInputPanelHeight;
        _inputPanel.Padding = new Padding(10);

        _modeTabs.Dock = DockStyle.Fill;
        _modeTabs.SelectedIndexChanged += (_, _) => AdjustInputPanelHeight();
        _inputPanel.Controls.Add(_modeTabs);

        var autoTab = new TabPage("自动获取");
        autoTab.Controls.Add(BuildAutoInputPanel());
        _modeTabs.TabPages.Add(autoTab);

        var manualTab = new TabPage("千牛源码辅助");
        manualTab.Controls.Add(BuildManualInputPanel());
        _modeTabs.TabPages.Add(manualTab);

        return _inputPanel;
    }

    private Control BuildAutoInputPanel()
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(0, 6, 0, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.Controls.Add(layout);

        layout.Controls.Add(new Label { Text = "商品ID/链接", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _productInputBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _productInputBox.Margin = new Padding(6, 3, 8, 3);
        layout.Controls.Add(_productInputBox, 1, 0);

        _startButton.Text = "开始获取长图";
        _startButton.Width = 130;
        _startButton.Click += async (_, _) => await StartExtractAsync();
        layout.Controls.Add(_startButton, 2, 0);

        layout.Controls.Add(new Label { Text = "保存文件夹", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _saveFolderBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _saveFolderBox.Margin = new Padding(6, 3, 8, 3);
        layout.Controls.Add(_saveFolderBox, 1, 1);

        _browseSaveButton.Text = "浏览";
        _browseSaveButton.Click += (_, _) => BrowseSaveFolder();
        layout.Controls.Add(_browseSaveButton, 2, 1);

        var openFolder = new Button { Text = "打开文件夹", Width = 100 };
        openFolder.Click += (_, _) => OpenSaveFolder();
        layout.Controls.Add(openFolder, 3, 1);

        return host;
    }

    private Control BuildManualInputPanel()
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 7,
            Padding = new Padding(0, 6, 0, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.Controls.Add(layout);

        layout.Controls.Add(new Label { Text = "商品ID/链接", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _manualInputBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _manualInputBox.Margin = new Padding(6, 3, 8, 3);
        layout.Controls.Add(_manualInputBox, 1, 0);

        _convertQianniuUrlButton.Text = "转换并复制";
        _convertQianniuUrlButton.Width = 110;
        _convertQianniuUrlButton.Click += (_, _) => ConvertQianniuUrl();
        layout.Controls.Add(_convertQianniuUrlButton, 2, 0);

        layout.Controls.Add(new Label { Text = "千牛链接", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _qianniuUrlBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _qianniuUrlBox.ReadOnly = true;
        _qianniuUrlBox.Margin = new Padding(6, 3, 0, 3);
        layout.SetColumnSpan(_qianniuUrlBox, 3);
        layout.Controls.Add(_qianniuUrlBox, 1, 1);

        layout.Controls.Add(new Label { Text = "商品页源码", AutoSize = true, Anchor = AnchorStyles.Left }, 1, 2);
        layout.Controls.Add(new Label { Text = "店铺页源码", AutoSize = true, Anchor = AnchorStyles.Left }, 3, 2);

        ConfigureSourceTextBox(_productSourceBox);
        _productSourceBox.Margin = new Padding(6, 3, 8, 3);
        layout.Controls.Add(_productSourceBox, 1, 3);

        ConfigureSourceTextBox(_shopSourceBox);
        _shopSourceBox.Margin = new Padding(6, 3, 0, 3);
        layout.Controls.Add(_shopSourceBox, 3, 3);

        layout.Controls.Add(new Label { Text = "商品名称", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
        ConfigureReadonlyResultBox(_manualProductNameBox);
        layout.Controls.Add(_manualProductNameBox, 1, 4);

        layout.Controls.Add(new Label { Text = "sellerId", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 4);
        ConfigureReadonlyResultBox(_manualSellerIdBox);
        layout.Controls.Add(_manualSellerIdBox, 3, 4);

        layout.Controls.Add(new Label { Text = "shopId", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 5);
        ConfigureReadonlyResultBox(_manualShopIdBox);
        layout.Controls.Add(_manualShopIdBox, 1, 5);

        _manualStartButton.Text = "一键提取并获取长图";
        _manualStartButton.Width = 150;
        _manualStartButton.Anchor = AnchorStyles.Right;
        _manualStartButton.Click += async (_, _) => await StartManualExtractAsync();
        layout.Controls.Add(_manualStartButton, 3, 6);

        return host;
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
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ScrollBars = ScrollBars.Vertical;
        _grid.ShowCellToolTips = true;
        _grid.DataSource = _tasks;
        _grid.Columns.Add(CreateGridColumn("状态", nameof(TaskItem.Status), 8, 60));
        _grid.Columns.Add(CreateGridColumn("商品ID", nameof(TaskItem.ProductId), 13, 100));
        _grid.Columns.Add(CreateGridColumn("商品名称", nameof(TaskItem.ProductName), 18, 110));
        _grid.Columns.Add(CreateGridColumn("sellerId", nameof(TaskItem.SellerId), 10, 80));
        _grid.Columns.Add(CreateGridColumn("shopId", nameof(TaskItem.ShopId), 10, 80));
        _grid.Columns.Add(CreateGridColumn("图片链接", nameof(TaskItem.ImageUrl), 24, 140));
        _grid.Columns.Add(CreateGridColumn("图片文件", nameof(TaskItem.ImageFile), 20, 120));
        _grid.Columns.Add(CreateGridColumn("说明", nameof(TaskItem.Message), 18, 120));
        _grid.Columns.Add(CreateGridColumn("创建时间", nameof(TaskItem.CreatedAt), 10, 85, "MM-dd HH:mm"));
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
            Text = "双击“图片链接”列复制网址；双击其他列复制图片文件。失败时列表会保留已获取的商品名、sellerId、shopId 和排查链接。"
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

    private void ConvertQianniuUrl()
    {
        var productId = TaobaoExtractor.ExtractProductId(TaobaoExtractor.NormalizeInput(_manualInputBox.Text.Trim()));
        if (string.IsNullOrWhiteSpace(productId))
        {
            MessageBox.Show("请输入商品ID或包含 id 参数的商品链接。", "无法转换", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var url = SourceParser.BuildQianniuProductUrl(productId);
        _qianniuUrlBox.Text = url;
        Clipboard.SetText(url);
        AppendLog($"已生成并复制千牛商品链接：{url}");
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
                if (port == ChromeHelper.DefaultPort && ChromeHelper.HasRunningChromeProcesses())
                {
                    AppendLog("检测到已有 Chrome 进程，保留默认端口 9222，不自动切换端口。");
                }
                else
                {
                    port = ChromeHelper.FindAvailablePort(port + 1);
                    _portBox.Value = port;
                    AppendLog($"原端口不可用，已自动调整为：{port}");
                }
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
            item.SellerId = result.SellerId;
            item.ShopId = result.ShopId;
            item.ImageUrl = result.ImageUrl;
            item.ImageFile = result.OutputFile;
            item.Message = "完成：店铺搜索页命中";
            AppendLog(item.Message);
        }
        catch (Exception ex)
        {
            item.Status = "失败";
            if (ex is ExtractDiagnosticException diagnosticException)
            {
                item.ImageUrl = diagnosticException.DiagnosticUrl;
                if (!string.IsNullOrWhiteSpace(diagnosticException.ProductName))
                {
                    item.ProductName = diagnosticException.ProductName;
                }
                item.SellerId = diagnosticException.SellerId;
                item.ShopId = diagnosticException.ShopId;
            }

            item.Message = ex.Message;
            AppendLog($"失败：{ex.Message}");
        }
        finally
        {
            _grid.Refresh();
            ToggleWorkControls(true);
        }
    }

    private async Task StartManualExtractAsync()
    {
        var productId = TaobaoExtractor.ExtractProductId(TaobaoExtractor.NormalizeInput(_manualInputBox.Text.Trim()));
        var productName = SourceParser.ExtractProductName(_productSourceBox.Text);
        var (sellerId, shopId) = SourceParser.ExtractSellerAndShopIds(_shopSourceBox.Text);

        _manualProductNameBox.Text = productName;
        _manualSellerIdBox.Text = sellerId;
        _manualShopIdBox.Text = shopId;

        if (string.IsNullOrWhiteSpace(productId))
        {
            MessageBox.Show("请输入商品ID或商品链接。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _qianniuUrlBox.Text = SourceParser.BuildQianniuProductUrl(productId);

        if (string.IsNullOrWhiteSpace(productName))
        {
            MessageBox.Show("未能从商品页源码中提取商品名称。", "提取失败", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(sellerId) || string.IsNullOrWhiteSpace(shopId))
        {
            MessageBox.Show($"未能完整提取 sellerId/shopId。sellerId={sellerId} shopId={shopId}", "提取失败", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        AppendLog($"源码解析完成：商品名称={productName} sellerId={sellerId} shopId={shopId}");

        var item = new TaskItem
        {
            Status = "进行中",
            ProductId = productId,
            ProductName = productName,
            SellerId = sellerId,
            ShopId = shopId,
            ImageUrl = BuildAuctionUrlForDisplay(sellerId, shopId, productName),
            Message = "按源码信息开始处理"
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

            var result = await _extractor.ExtractFromKnownInfoAsync(
                productId,
                productName,
                sellerId,
                shopId,
                (int)_portBox.Value,
                _saveFolderBox.Text,
                progress);

            item.Status = "完成";
            item.ProductId = result.ProductId;
            item.ProductName = result.ProductName;
            item.SellerId = result.SellerId;
            item.ShopId = result.ShopId;
            item.ImageUrl = result.ImageUrl;
            item.ImageFile = result.OutputFile;
            item.Message = "完成：源码信息店铺搜索页命中";
            AppendLog(item.Message);
        }
        catch (Exception ex)
        {
            item.Status = "失败";
            if (ex is ExtractDiagnosticException diagnosticException)
            {
                item.ImageUrl = diagnosticException.DiagnosticUrl;
                item.ProductName = FirstNonEmpty(diagnosticException.ProductName, item.ProductName);
                item.SellerId = FirstNonEmpty(diagnosticException.SellerId, item.SellerId);
                item.ShopId = FirstNonEmpty(diagnosticException.ShopId, item.ShopId);
            }

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
        _convertQianniuUrlButton.Enabled = enabled;
        _manualStartButton.Enabled = enabled;
    }

    private void AdjustInputPanelHeight()
    {
        _inputPanel.Height = _modeTabs.SelectedIndex == 0
            ? AutoInputPanelHeight
            : ManualInputPanelHeight;
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

        var propertyName = e.ColumnIndex >= 0
            ? _grid.Columns[e.ColumnIndex].DataPropertyName
            : "";
        if (propertyName == nameof(TaskItem.ImageUrl))
        {
            CopyImageUrl(item);
            return;
        }

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

    private void CopyImageUrl(TaskItem item)
    {
        if (string.IsNullOrWhiteSpace(item.ImageUrl))
        {
            MessageBox.Show("该任务没有图片链接。", "无法复制", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Clipboard.SetText(item.ImageUrl);
        AppendLog($"已复制图片链接到剪贴板：{item.ImageUrl}");
        item.Message = "已复制图片链接到剪贴板";
        _grid.Refresh();
    }

    private static DataGridViewTextBoxColumn CreateGridColumn(
        string header,
        string propertyName,
        float fillWeight,
        int minimumWidth,
        string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = propertyName,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = fillWeight,
            MinimumWidth = minimumWidth,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        if (!string.IsNullOrWhiteSpace(format))
        {
            column.DefaultCellStyle.Format = format;
        }

        return column;
    }

    private static void ConfigureSourceTextBox(RichTextBox textBox)
    {
        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
        textBox.DetectUrls = false;
        textBox.HideSelection = false;
        textBox.ScrollBars = RichTextBoxScrollBars.Vertical;
        textBox.WordWrap = false;
    }

    private static void ConfigureReadonlyResultBox(TextBox textBox)
    {
        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        textBox.Enabled = false;
        textBox.Margin = new Padding(6, 3, 8, 3);
    }

    private static string BuildAuctionUrlForDisplay(string sellerId, string shopId, string productName)
    {
        var builder = new UriBuilder("https://market.m.taobao.com/app/tb-source-app/shop-auction/pages/auction");
        builder.Query = $"wh_weex=true&sellerId={Uri.EscapeDataString(sellerId)}&shopId={Uri.EscapeDataString(shopId)}&searchText={Uri.EscapeDataString(productName)}";
        return builder.Uri.ToString();
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
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
