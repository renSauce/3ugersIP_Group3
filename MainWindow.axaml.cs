using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using SystemLogin.Core;
using SystemLogin.Robotics;
using SystemLogin.RobotScripts;
using SystemLogin.Security;

namespace SystemLogin;

public partial class MainWindow : Window
{
    private Authentication _authentication;
    private AppDbContext _appDbContext;
    private CustomerOperation _customerOperation;
    private OrderOperation _orderOperation;
    private User? _currentUser;

    private readonly ObservableCollection<Customer> _customers = new();
    private readonly ObservableCollection<OrderViewModel> _orders = new();
    private ComboBox? _orderCustomerComboBox;
    private TextBox? _redQuantityTextBox;
    private TextBox? _greenQuantityTextBox;
    private TextBox? _blueQuantityTextBox;
    private TextBox? _yellowQuantityTextBox;
    private TextBox? _customerNameTextBox;
    private TextBox? _customerAddressTextBox;
    private TextBox? _loginUsernameTextBox;
    private TextBox? _loginPasswordTextBox;
    private Button? _createCustomerButton;
    private DataGrid? _customersGrid;
    private StackPanel? _customerCreatePanel;
    private DataGrid? _ordersGrid;
    private Button? _createOrderButton;
    private Button? _sortOrderButton;
    private Button? _loginButton;
    private OrderViewModel? _selectedOrder;

    private Robot? _robotConnection;
    private TextBox? _robotIpTextBox;
    private TextBox? _dashboardPortTextBox;
    private TextBox? _urscriptPortTextBox;
    private Button? _connectRobotButton;
    private Button? _disconnectRobotButton;
    private TextBlock? _robotConnectionStatusText;

    private readonly LoginSecurityService _loginSecurity;
    private readonly InactivityLogoutTimer _inactivityLogoutTimer;

    public MainWindow()
    {
        InitializeComponent();

        _robotIpTextBox = this.FindControl<TextBox>("RobotIpTextBox");
        _dashboardPortTextBox = this.FindControl<TextBox>("DashboardPortTextBox");
        _urscriptPortTextBox = this.FindControl<TextBox>("UrscriptPortTextBox");
        _connectRobotButton = this.FindControl<Button>("ConnectRobotButton");
        _disconnectRobotButton = this.FindControl<Button>("DisconnectRobotButton");
        _robotConnectionStatusText = this.FindControl<TextBlock>("RobotConnectionStatusText");
        _customerNameTextBox = this.FindControl<TextBox>("CustomerNameTextBox");
        _customerAddressTextBox = this.FindControl<TextBox>("CustomerAddressTextBox");
        _loginUsernameTextBox = this.FindControl<TextBox>("LoginUsername");
        _loginPasswordTextBox = this.FindControl<TextBox>("LoginPassword");
        _createCustomerButton = this.FindControl<Button>("CreateCustomerButton");
        _customersGrid = this.FindControl<DataGrid>("CustomersGrid");
        _customerCreatePanel = this.FindControl<StackPanel>("CustomerCreatePanel");
        _ordersGrid = this.FindControl<DataGrid>("OrdersGrid");
        _createOrderButton = this.FindControl<Button>("CreateOrderButton");
        _sortOrderButton = this.FindControl<Button>("SortOrderButton");
        _loginButton = this.FindControl<Button>("LoginButton");
        _orderCustomerComboBox = this.FindControl<ComboBox>("OrderCustomerComboBox");
        _redQuantityTextBox = this.FindControl<TextBox>("RedQuantityTextBox");
        _greenQuantityTextBox = this.FindControl<TextBox>("GreenQuantityTextBox");
        _blueQuantityTextBox = this.FindControl<TextBox>("BlueQuantityTextBox");
        _yellowQuantityTextBox = this.FindControl<TextBox>("YellowQuantityTextBox");

        _loginSecurity = new LoginSecurityService(TimeSpan.FromMinutes(5), _log);
        _loginSecurity.LockStateChanged += UpdateLoginButtonState;
        
        _inactivityLogoutTimer = new InactivityLogoutTimer(TimeSpan.FromMinutes(5), HandleInactivityTimeout);

        if (_connectRobotButton != null) _connectRobotButton.Click += async (_, __) => await ConnectRobotAsync();
        if (_disconnectRobotButton != null) _disconnectRobotButton.Click += (_, __) => DisconnectRobot();
        if (_createCustomerButton != null) _createCustomerButton.Click += async (_, __) => await CreateCustomerAsync();
        if (_createOrderButton != null) _createOrderButton.Click += async (_, __) => await CreateOrderAsync();
        if (_sortOrderButton != null) _sortOrderButton.Click += async (_, __) => await SortSelectedOrderAsync();
        if (_ordersGrid != null) _ordersGrid.SelectionChanged += (_, __) => UpdateOrderSelection();
        if (_loginUsernameTextBox != null) _loginUsernameTextBox.TextChanged += (_, __) => UpdateLoginButtonState();
        if (_loginPasswordTextBox != null) _loginPasswordTextBox.TextChanged += (_, __) => UpdateLoginButtonState();

        DataContext = this;

        foreach (TabItem item in TabControl.Items)
            item.IsVisible = false;
        LoginTab.IsVisible = true;

        InitializeServices();
        Loaded += OnLoaded;
        InitializeRobotControls();
        InitializeCustomerControls();
        UpdateLoginButtonState();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (await EnsureDatabaseCreatedWithExampleDataAsync())
            _log("Customer data did not exist. So I created default customers.");

        if (_appDbContext != null)
        {
            await _appDbContext.EnsureSchemaAsync();
            await LoadCustomersAsync();
            await LoadOrdersAsync();
        }
    }

    private void _log(string s)
    {
        var now = DateTime.Now.ToString("yy-MM-dd HH:mm:ss");
        LogOutput.Text += $"{now} | {s}\n";
    }

    private void UiLog(string message)
    {
        Dispatcher.UIThread.Post(() => _log(message));
    }

    private void InitializeServices()
    {
        _appDbContext?.Dispose();
        _appDbContext = new AppDbContext();
        _authentication = new Authentication(_appDbContext, new PasswordHasher());
        _customerOperation = new CustomerOperation(_appDbContext);
        _orderOperation = new OrderOperation(_appDbContext);
    }

    public async Task<bool> EnsureDatabaseCreatedWithExampleDataAsync()
    {
        var databaseIsCreated = await _appDbContext.Database.EnsureCreatedAsync();
        await _appDbContext.EnsureSchemaAsync();

        InitializeServices();
        if (!await _appDbContext.Users.AnyAsync())
        {
            await _authentication.CreateUserAsync("admin", "admin1234", true);
        }

        if (!await _appDbContext.Products.AnyAsync())
        {
            _appDbContext.Products.AddRange(
                new Product { Name = "Red Block", Color = BlockColor.Red },
                new Product { Name = "Green Block", Color = BlockColor.Green },
                new Product { Name = "Blue Block", Color = BlockColor.Blue },
                new Product { Name = "Yellow Block", Color = BlockColor.Yellow }
            );
            await _appDbContext.SaveChangesAsync();
        }

        return databaseIsCreated;
    }

    private void InitializeRobotControls()
    {
        _robotConnection = new Robot();

        if (_robotIpTextBox != null) _robotIpTextBox.Text = _robotConnection.IpAddress;
        if (_dashboardPortTextBox != null) _dashboardPortTextBox.Text = _robotConnection.DashboardPort.ToString();
        if (_urscriptPortTextBox != null) _urscriptPortTextBox.Text = _robotConnection.UrscriptPort.ToString();

        UpdateRobotConnectionUi(false);
    }

    private void UpdateRobotConnectionUi(bool connected)
    {
        if (_robotConnectionStatusText != null) _robotConnectionStatusText.Text = connected ? "Connection: Connected" : "Connection: Disconnected";
        if (_connectRobotButton != null) _connectRobotButton.IsEnabled = !connected;
        if (_disconnectRobotButton != null) _disconnectRobotButton.IsEnabled = connected;
    }

    private bool TryReadRobotSettings(out string ipAddress, out int dashboardPort, out int urscriptPort)
    {
        ipAddress = _robotIpTextBox?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            UiLog("[Robot] IP address is required.");
            dashboardPort = 0;
            urscriptPort = 0;
            return false;
        }

        if (!int.TryParse(_dashboardPortTextBox?.Text, out dashboardPort))
        {
            UiLog("[Robot] Invalid dashboard port.");
            urscriptPort = 0;
            return false;
        }

        if (!int.TryParse(_urscriptPortTextBox?.Text, out urscriptPort))
        {
            UiLog("[Robot] Invalid URScript port.");
            return false;
        }

        return true;
    }

    private async Task ConnectRobotAsync()
    {
        if (_robotConnection == null)
            _robotConnection = new Robot();

        if (!TryReadRobotSettings(out var ipAddress, out var dashboardPort, out var urscriptPort))
            return;

        _robotConnection.IpAddress = ipAddress;
        _robotConnection.DashboardPort = dashboardPort;
        _robotConnection.UrscriptPort = urscriptPort;

        try
        {
            await Task.Run(() => _robotConnection.Connect());
            UiLog($"[Robot] Connected to {ipAddress}:{dashboardPort}/{urscriptPort}.");
            UpdateRobotConnectionUi(true);
        }
        catch (Exception ex)
        {
            UiLog("[Robot] Connect error: " + ex.Message);
            UpdateRobotConnectionUi(false);
        }
    }

    private void DisconnectRobot()
    {
        if (_robotConnection == null)
            return;

        _robotConnection.Disconnect();
        UiLog("[Robot] Disconnected.");
        UpdateRobotConnectionUi(false);
    }

    private async void CreateUserButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var (username, password, isAdmin) =
            (CreateUserUsername.Text, CreateUserPassword.Text, CreateUserIsAdmin.IsChecked);
        ResetInactivityTimer();

        if (!LoginSecurityService.IsCredentialValid(username) || !LoginSecurityService.IsPasswordValid(password))
        {
            _log("Username must be alphanumeric and password must be alphanumeric with at least 9 characters.");
            return;
        }

        if (await _authentication.UsernameExistsAsync(username))
        {
            _log($"Username {username} exists.");
            return;
        }

        await _authentication.CreateUserAsync(username, password, (bool)isAdmin);
        _log($"Created user: {username}.");
    }

    private async void LoginButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var username = _loginUsernameTextBox?.Text ?? string.Empty;
        var password = _loginPasswordTextBox?.Text ?? string.Empty;

        if (_loginSecurity.IsLoginLocked)
        {
            _log("Login is temporarily disabled due to repeated failures. Please wait and try again.");
            return;
        }

        if (!LoginSecurityService.IsCredentialValid(username) || !LoginSecurityService.IsPasswordValid(password))
        {
            _log("Username must be alphanumeric and password must be alphanumeric with at least 9 characters.");
            _loginSecurity.RecordFailedAttempt();
            UpdateLoginButtonState();
            return;
        }

        if (!await _authentication.UsernameExistsAsync(username))
        {
            _log("Username does not exist.");
            _loginSecurity.RecordFailedAttempt();
            return;
        }

        if (!await _authentication.CredentialsCorrectAsync(username, password))
        {
            _log("Password wrong.");
            _loginSecurity.RecordFailedAttempt();
            return;
        }

        var user = await _authentication.GetUserAsync(username);
        _currentUser = user;
        _loginSecurity.ResetFailures();
        LogoutButton.IsVisible = true;

        LoginTab.IsVisible = false;
        RobotTab.IsVisible = true;
        OrdersTab.IsVisible = true;
        CustomersTab.IsVisible = true;
        UsersTab.IsVisible = user.IsAdmin;

        if (_customerCreatePanel != null)
            _customerCreatePanel.IsVisible = user.IsAdmin;

        RobotTab.IsSelected = true;

        _log($"{user.Username} logged in.");
        if (_loginUsernameTextBox != null) _loginUsernameTextBox.Text = string.Empty;
        if (_loginPasswordTextBox != null) _loginPasswordTextBox.Text = string.Empty;
        UpdateLoginButtonState();
        ResetInactivityTimer();

        await LoadCustomersAsync();
        await LoadOrdersAsync();
    }

    private async void LogoutButton_OnClick(object? sender, RoutedEventArgs e)
    {
        StopInactivityTimer();
        foreach (TabItem item in TabControl.Items)
            item.IsVisible = false;
        LoginTab.IsVisible = true;
        LoginTab.IsSelected = true;
        LogoutButton.IsVisible = false;
        _currentUser = null;
        if (_customerCreatePanel != null) _customerCreatePanel.IsVisible = false;
        _log("Logged out.");
    }

    private void ClearLogButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LogOutput.Text = "";
    }

    private void InitializeCustomerControls()
    {
        if (_customersGrid != null)
            _customersGrid.ItemsSource = _customers;

        if (_ordersGrid != null)
            _ordersGrid.ItemsSource = _orders;

        if (_orderCustomerComboBox != null)
            _orderCustomerComboBox.ItemsSource = _customers;

        if (_customerCreatePanel != null)
            _customerCreatePanel.IsVisible = false;
    }

    private async Task LoadCustomersAsync()
    {
        if (_customerOperation == null)
            return;

        var customers = await _customerOperation.GetCustomersAsync();
        _customers.Clear();
        foreach (var customer in customers)
            _customers.Add(customer);

        if (_orderCustomerComboBox != null && _orderCustomerComboBox.SelectedItem == null && _customers.Count > 0)
            _orderCustomerComboBox.SelectedIndex = 0;
    }

    private async Task LoadOrdersAsync()
    {
        if (_orderOperation == null)
            return;

        var selectedId = _selectedOrder?.Id;
        var orders = await _orderOperation.GetOrdersAsync();
        _orders.Clear();

        foreach (var order in orders)
        {
            var items = order.Lines
                .Select(line => new SortingOrderItem(
                    line.ProductId,
                    line.Product.Name,
                    line.Product.Color,
                    line.Quantity))
                .ToList();

            _orders.Add(new OrderViewModel
            {
                Id = order.Id,
                CustomerName = order.Customer.Name,
                Status = order.Status,
                ItemsSummary = string.Join(", ", items.Select(i => $"{i.Quantity}x {i.ProductName}")),
                SortingOrder = new SortingOrder(order.Id, order.Customer.Name, items)
            });
        }

        if (_ordersGrid != null && selectedId.HasValue)
        {
            var match = _orders.FirstOrDefault(o => o.Id == selectedId.Value);
            if (match != null)
                _ordersGrid.SelectedItem = match;
        }

        UpdateOrderSelection();
    }

    private void UpdateOrderSelection()
    {
        _selectedOrder = _ordersGrid?.SelectedItem as OrderViewModel;
        var enable = _selectedOrder?.Status == OrderStatus.Pending;
        if (_sortOrderButton != null)
            _sortOrderButton.IsEnabled = enable == true;
    }

    private async Task SortSelectedOrderAsync()
    {
        if (_selectedOrder == null)
        {
            UiLog("[Orders] Select an order to sort.");
            return;
        }

        await SendOrderToRobotAsync(_selectedOrder.Id);
    }

    private async Task CreateOrderAsync()
    {
        if (_orderOperation == null)
            return;
        ResetInactivityTimer();

        if (_orderCustomerComboBox?.SelectedItem is not Customer customer)
        {
            UiLog("[Orders] Select a customer for the order.");
            return;
        }

        var quantities = new Dictionary<BlockColor, int>
        {
            [BlockColor.Red] = ParseQuantity(_redQuantityTextBox),
            [BlockColor.Green] = ParseQuantity(_greenQuantityTextBox),
            [BlockColor.Blue] = ParseQuantity(_blueQuantityTextBox),
            [BlockColor.Yellow] = ParseQuantity(_yellowQuantityTextBox)
        };

        if (quantities.Values.All(q => q <= 0))
        {
            UiLog("[Orders] Specify at least one block to sort.");
            return;
        }

        try
        {
            var orderId = await _orderOperation.CreateOrderAsync(customer.Id, quantities);
            UiLog($"[Orders] Created order #{orderId} for {customer.Name}.");
            ResetOrderForm();
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            UiLog("[Orders] Create error: " + ex.Message);
        }
    }

    private async Task SendOrderToRobotAsync(int orderId)
    {
        if (_orderOperation == null)
            return;

        if (_robotConnection == null || !_robotConnection.Connected)
        {
            UiLog("[Orders] Connect to the robot before sorting.");
            return;
        }

        try
        {
            await _orderOperation.UpdateStatusAsync(orderId, OrderStatus.Processing);
            var orderEntity = await _orderOperation.GetOrderDetailsAsync(orderId);
            var sortingOrder = BuildSortingOrder(orderEntity);
            var options = SorterColorOptions.FromOrder(sortingOrder) with
            {
                OrderDropPoseOverride = "p[-0.206331968965, -0.497838673522, -0.064026522761, -2.613660637500, 1.335120535459, -0.025721254140]",
                ResortDropPoseOverride = "p[-0.206331968965, -0.197838673522, -0.064026522761, -2.613660637500, 1.335120535459, -0.025721254140]"
            };
            var script = SorterColorScriptBuilder.Build(options);
            await Task.Run(() => _robotConnection.SendUrscript(script));
            UiLog($"[Orders] Script sent for order #{orderId}.");
            await _orderOperation.UpdateStatusAsync(orderId, OrderStatus.Completed);
        }
        catch (Exception ex)
        {
            UiLog("[Orders] Sorting error: " + ex.Message);
            await _orderOperation.UpdateStatusAsync(orderId, OrderStatus.Pending);
        }
        finally
        {
            await LoadOrdersAsync();
        }
    }

    private static SortingOrder BuildSortingOrder(Order orderEntity)
    {
        var items = orderEntity.Lines
            .Select(line => new SortingOrderItem(
                line.ProductId,
                line.Product.Name,
                line.Product.Color,
                line.Quantity))
            .ToList();

        return new SortingOrder(orderEntity.Id, orderEntity.Customer.Name, items);
    }

    private static int ParseQuantity(TextBox? textBox)
    {
        if (textBox == null)
            return 0;

        return int.TryParse(textBox.Text, out var value) && value > 0 ? value : 0;
    }

    private void ResetOrderForm()
    {
        if (_redQuantityTextBox != null) _redQuantityTextBox.Text = "0";
        if (_greenQuantityTextBox != null) _greenQuantityTextBox.Text = "0";
        if (_blueQuantityTextBox != null) _blueQuantityTextBox.Text = "0";
        if (_yellowQuantityTextBox != null) _yellowQuantityTextBox.Text = "0";
    }

    private async Task CreateCustomerAsync()
    {
        if (_customerOperation == null)
            return;

        if (_currentUser == null)
        {
            UiLog("[Customers] You need to login before creating customers.");
            return;
        }
        ResetInactivityTimer();

        var name = _customerNameTextBox?.Text?.Trim() ?? string.Empty;
        var address = _customerAddressTextBox?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            UiLog("[Customers] Name is required.");
            return;
        }

        if (name.Length > 20)
        {
            UiLog("[Customers] Name must be 20 characters or fewer.");
            return;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            UiLog("[Customers] Address is required.");
            return;
        }

        if (address.Length > 30)
        {
            UiLog("[Customers] Address must be 30 characters or fewer.");
            return;
        }
        try
        {
            await _customerOperation.CreateCustomerAsync(_currentUser.Id, name, address);
            UiLog($"[Customers] Created '{name}'.");
            await LoadCustomersAsync();

            if (_customerNameTextBox != null) _customerNameTextBox.Text = "";
            if (_customerAddressTextBox != null) _customerAddressTextBox.Text = "";
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            UiLog("[Customers] Create error: " + detail);
        }
    }

    public sealed class OrderViewModel
    {
        public int Id { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public OrderStatus Status { get; set; }
        public string ItemsSummary { get; init; } = string.Empty;
        public SortingOrder SortingOrder { get; init; } =
            new SortingOrder(0, string.Empty, Array.Empty<SortingOrderItem>());
    }

    private void ResetInactivityTimer()
    {
        if (_currentUser == null)
            return;

        _inactivityLogoutTimer.Reset();
    }

    private void StopInactivityTimer()
    {
        _inactivityLogoutTimer.Stop();
    }

    private void HandleInactivityTimeout()
    {
        if (_currentUser == null)
            return;

        _log("No activity detected. Logging out.");
        LogoutButton_OnClick(this, new RoutedEventArgs());
    }

    private void UpdateLoginButtonState()
    {
        if (_loginButton == null || _loginUsernameTextBox == null || _loginPasswordTextBox == null)
            return;

        var username = _loginUsernameTextBox.Text;
        var password = _loginPasswordTextBox.Text;
        var credentialsValid =
            LoginSecurityService.IsCredentialValid(username) && LoginSecurityService.IsPasswordValid(password);
        _loginButton.IsEnabled = credentialsValid && !_loginSecurity.IsLoginLocked;
    }
}
