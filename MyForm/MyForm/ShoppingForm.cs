using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MyForm
{
    public class ShoppingForm : Form
    {
        // ---------- Data ----------
        private class Product
        {
            public string Name;
            public string Category;
            public decimal Price;
        }

        private class CartItem
        {
            public Product Product;
            public int Qty;
        }

        private readonly List<Product> _products = new List<Product>
        {
            new Product { Name = "Wireless Headphones Pro", Category = "Headphones",     Price = 2500m },
            new Product { Name = "Gaming Headset",          Category = "Headphones",     Price = 1800m },
            new Product { Name = "Earbuds Mini",            Category = "Headphones",     Price = 950m  },
            new Product { Name = "Mechanical Keyboard",     Category = "Keyboards",      Price = 2200m },
            new Product { Name = "Wireless Keyboard",       Category = "Keyboards",      Price = 1100m },
            new Product { Name = "USB-C Cable 2m",          Category = "Cables",         Price = 150m  },
            new Product { Name = "HDMI Cable 4K",           Category = "Cables",         Price = 220m  },
            new Product { Name = "USB-C to HDMI Adapter",   Category = "Cables",         Price = 400m  },
            new Product { Name = "Laptop Sleeve 15\"",      Category = "Device Cases",   Price = 450m  },
            new Product { Name = "Phone Case Armor",        Category = "Device Cases",   Price = 300m  },
        };

        private readonly List<CartItem> _cart = new List<CartItem>();
        private const decimal ShippingFee = 50m;
        private const decimal FreeShippingOver = 2000m;

        // ---------- Controls ----------
        private ComboBox cmbCategory;
        private ListView lvProducts;
        private NumericUpDown numQty;
        private Button btnAdd;

        private ListView lvCart;
        private Button btnRemove, btnClear;
        private Label lblSubtotal, lblShipping, lblTotal;

        private TextBox txtName, txtPhone, txtEmail, txtAddress;
        private ComboBox cmbPayment;
        private Button btnPlaceOrder;

        // All sizes are derived from the font height, so the layout
        // stays correct at any Windows display scaling (100%, 125%, 150%...).
        private int fh;

        public ShoppingForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.5f);
            fh = Font.Height;

            Text = "TechHub - Online Shopping";
            StartPosition = FormStartPosition.CenterScreen;

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int w = Math.Min(fh * 62, wa.Width - 40);
            int h = Math.Min(fh * 40, wa.Height - 40);
            ClientSize = new Size(w, h);
            MinimumSize = new Size(Math.Min(fh * 50, wa.Width), Math.Min(fh * 34, wa.Height));

            BuildUi();
            LoadProducts();
            RefreshCart();

            Shown += (s, e) => FitAllColumns();
        }

        // ---------- UI ----------
        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(fh / 2)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, fh * 11));
            Controls.Add(root);

            // --- Products group ---
            var gbProducts = new GroupBox { Text = "Products", Dock = DockStyle.Fill, Padding = new Padding(fh / 2) };

            var pTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = fh * 2 + 8,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            pTop.Controls.Add(MakeFlowLabel("Category:", fh * 5));
            cmbCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = fh * 12,
                Margin = new Padding(0, 4, 0, 0)
            };
            cmbCategory.SelectedIndexChanged += (s, e) => LoadProducts();
            pTop.Controls.Add(cmbCategory);

            var pBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = fh * 2 + 14,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            pBottom.Controls.Add(MakeFlowLabel("Qty:", fh * 3));
            numQty = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 20,
                Value = 1,
                Width = fh * 4,
                Margin = new Padding(0, 6, 8, 0)
            };
            pBottom.Controls.Add(numQty);
            btnAdd = MakeButton("Add to Cart");
            btnAdd.Click += BtnAdd_Click;
            pBottom.Controls.Add(btnAdd);

            lvProducts = MakeListView();
            lvProducts.Columns.Add("Product", 100);
            lvProducts.Columns.Add("Category", 100);
            lvProducts.Columns.Add("Price (EGP)", 100, HorizontalAlignment.Right);
            lvProducts.DoubleClick += (s, e) => BtnAdd_Click(s, e);
            lvProducts.SizeChanged += (s, e) => FitColumns(lvProducts, 50, 25, 25);

            gbProducts.Controls.Add(lvProducts);
            gbProducts.Controls.Add(pBottom);
            gbProducts.Controls.Add(pTop);
            root.Controls.Add(gbProducts, 0, 0);

            // --- Cart group ---
            var gbCart = new GroupBox { Text = "Your Cart", Dock = DockStyle.Fill, Padding = new Padding(fh / 2) };

            var cartButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = fh * 2 + 14,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            btnRemove = MakeButton("Remove Selected");
            btnRemove.Click += BtnRemove_Click;
            btnClear = MakeButton("Clear Cart");
            btnClear.Click += (s, e) => { _cart.Clear(); RefreshCart(); };
            cartButtons.Controls.Add(btnRemove);
            cartButtons.Controls.Add(btnClear);

            lvCart = MakeListView();
            lvCart.Columns.Add("Item", 100);
            lvCart.Columns.Add("Qty", 50, HorizontalAlignment.Right);
            lvCart.Columns.Add("Price", 80, HorizontalAlignment.Right);
            lvCart.Columns.Add("Subtotal", 90, HorizontalAlignment.Right);
            lvCart.SizeChanged += (s, e) => FitColumns(lvCart, 42, 12, 22, 24);

            var totals = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = fh * 7,
                ColumnCount = 2,
                RowCount = 3
            };
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            totals.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            totals.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            totals.RowStyles.Add(new RowStyle(SizeType.Percent, 33.4f));
            var bold = new Font(Font, FontStyle.Bold);
            lblSubtotal = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            lblShipping = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            lblTotal = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = bold };
            totals.Controls.Add(new Label { Text = "Subtotal:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            totals.Controls.Add(lblSubtotal, 1, 0);
            totals.Controls.Add(new Label { Text = "Shipping:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            totals.Controls.Add(lblShipping, 1, 1);
            totals.Controls.Add(new Label { Text = "Total:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = bold }, 0, 2);
            totals.Controls.Add(lblTotal, 1, 2);

            gbCart.Controls.Add(lvCart);
            gbCart.Controls.Add(totals);
            gbCart.Controls.Add(cartButtons);
            root.Controls.Add(gbCart, 1, 0);

            // --- Customer details group ---
            var gbCustomer = new GroupBox
            {
                Text = "Customer and Delivery Details",
                Dock = DockStyle.Fill,
                Padding = new Padding(fh / 2)
            };

            int rowH = fh * 2 + 10;
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fh * 7));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fh * 7));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            txtName = new TextBox { Dock = DockStyle.Fill };
            txtPhone = new TextBox { Dock = DockStyle.Fill };
            txtEmail = new TextBox { Dock = DockStyle.Fill };
            cmbPayment = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPayment.Items.AddRange(new object[] { "Cash on Delivery", "Credit / Debit Card", "Vodafone Cash", "InstaPay" });
            cmbPayment.SelectedIndex = 0;
            txtAddress = new TextBox { Dock = DockStyle.Fill, Multiline = true };

            grid.Controls.Add(MakeLabel("Full name:"), 0, 0);
            grid.Controls.Add(txtName, 1, 0);
            grid.Controls.Add(MakeLabel("Phone:"), 2, 0);
            grid.Controls.Add(txtPhone, 3, 0);
            grid.Controls.Add(MakeLabel("Email:"), 0, 1);
            grid.Controls.Add(txtEmail, 1, 1);
            grid.Controls.Add(MakeLabel("Payment:"), 2, 1);
            grid.Controls.Add(cmbPayment, 3, 1);
            grid.Controls.Add(MakeLabel("Address:"), 0, 2);
            grid.Controls.Add(txtAddress, 1, 2);
            grid.SetColumnSpan(txtAddress, 3);

            btnPlaceOrder = new Button
            {
                Text = "Place Order",
                Dock = DockStyle.Bottom,
                Height = fh * 2 + 12,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnPlaceOrder.FlatAppearance.BorderSize = 0;
            btnPlaceOrder.Click += BtnPlaceOrder_Click;

            gbCustomer.Controls.Add(grid);
            gbCustomer.Controls.Add(btnPlaceOrder);
            root.Controls.Add(gbCustomer, 0, 1);
            root.SetColumnSpan(gbCustomer, 2);

            // Category list
            cmbCategory.Items.Add("All");
            foreach (var c in _products.Select(p => p.Category).Distinct())
                cmbCategory.Items.Add(c);
            cmbCategory.SelectedIndex = 0;
        }

        // ---------- UI helpers ----------
        private Label MakeLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        }

        private Label MakeFlowLabel(string text, int width)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Width = width,
                Height = fh * 2 + 4,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };
        }

        private Button MakeButton(string text)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(fh / 2, 2, fh / 2, 2),
                MinimumSize = new Size(0, fh * 2 + 6),
                Margin = new Padding(0, 4, 8, 0)
            };
        }

        private ListView MakeListView()
        {
            return new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false
            };
        }

        // Spread the columns across the full width of the list (percentages).
        private void FitColumns(ListView lv, params int[] percents)
        {
            int total = lv.ClientSize.Width;
            if (total <= 0) return;
            for (int i = 0; i < percents.Length && i < lv.Columns.Count; i++)
                lv.Columns[i].Width = total * percents[i] / 100;
        }

        private void FitAllColumns()
        {
            FitColumns(lvProducts, 50, 25, 25);
            FitColumns(lvCart, 42, 12, 22, 24);
        }

        // ---------- Logic ----------
        private void LoadProducts()
        {
            if (lvProducts == null || cmbCategory == null || cmbCategory.SelectedItem == null) return;
            string cat = cmbCategory.SelectedItem.ToString();
            lvProducts.Items.Clear();
            foreach (var p in _products.Where(x => cat == "All" || x.Category == cat))
            {
                var item = new ListViewItem(p.Name) { Tag = p };
                item.SubItems.Add(p.Category);
                item.SubItems.Add(p.Price.ToString("N2"));
                lvProducts.Items.Add(item);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (lvProducts.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a product first.", "TechHub", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var product = (Product)lvProducts.SelectedItems[0].Tag;
            int qty = (int)numQty.Value;

            var existing = _cart.FirstOrDefault(c => c.Product == product);
            if (existing != null) existing.Qty += qty;
            else _cart.Add(new CartItem { Product = product, Qty = qty });

            numQty.Value = 1;
            RefreshCart();
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (lvCart.SelectedItems.Count == 0) return;
            var item = (CartItem)lvCart.SelectedItems[0].Tag;
            _cart.Remove(item);
            RefreshCart();
        }

        private decimal Subtotal()
        {
            return _cart.Sum(c => c.Product.Price * c.Qty);
        }

        private decimal Shipping()
        {
            decimal sub = Subtotal();
            return (sub == 0 || sub >= FreeShippingOver) ? 0m : ShippingFee;
        }

        private void RefreshCart()
        {
            lvCart.Items.Clear();
            foreach (var c in _cart)
            {
                var item = new ListViewItem(c.Product.Name) { Tag = c };
                item.SubItems.Add(c.Qty.ToString());
                item.SubItems.Add(c.Product.Price.ToString("N2"));
                item.SubItems.Add((c.Product.Price * c.Qty).ToString("N2"));
                lvCart.Items.Add(item);
            }
            decimal sub = Subtotal(), ship = Shipping();
            lblSubtotal.Text = sub.ToString("N2") + " EGP";
            lblShipping.Text = (ship == 0 && sub > 0) ? "Free" : ship.ToString("N2") + " EGP";
            lblTotal.Text = (sub + ship).ToString("N2") + " EGP";
        }

        private bool ValidateInputs(out string error)
        {
            if (_cart.Count == 0) { error = "Your cart is empty."; return false; }
            if (string.IsNullOrWhiteSpace(txtName.Text)) { error = "Please enter your full name."; txtName.Focus(); return false; }
            if (!Regex.IsMatch(txtPhone.Text.Trim(), @"^(\+?20|0)?1[0125]\d{8}$"))
            { error = "Please enter a valid Egyptian mobile number (e.g. 01012345678)."; txtPhone.Focus(); return false; }
            if (!Regex.IsMatch(txtEmail.Text.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            { error = "Please enter a valid email address."; txtEmail.Focus(); return false; }
            if (string.IsNullOrWhiteSpace(txtAddress.Text)) { error = "Please enter your delivery address."; txtAddress.Focus(); return false; }
            error = null;
            return true;
        }

        private void BtnPlaceOrder_Click(object sender, EventArgs e)
        {
            string error;
            if (!ValidateInputs(out error))
            {
                MessageBox.Show(error, "Check your order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string orderNo = "TH-" + DateTime.Now.ToString("yyMMddHHmmss");
            string summary = string.Join(Environment.NewLine,
                _cart.Select(c => string.Format("{0} x{1}  =  {2:N2} EGP", c.Product.Name, c.Qty, c.Product.Price * c.Qty)));

            MessageBox.Show(
                "Thank you, " + txtName.Text.Trim() + "!" + Environment.NewLine + Environment.NewLine +
                "Order No: " + orderNo + Environment.NewLine +
                summary + Environment.NewLine + Environment.NewLine +
                "Shipping: " + lblShipping.Text + Environment.NewLine +
                "Total: " + lblTotal.Text + Environment.NewLine +
                "Payment: " + cmbPayment.SelectedItem + Environment.NewLine +
                "Deliver to: " + txtAddress.Text.Trim(),
                "Order placed", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _cart.Clear();
            RefreshCart();
            txtName.Clear(); txtPhone.Clear(); txtEmail.Clear(); txtAddress.Clear();
            cmbPayment.SelectedIndex = 0;
        }
    }
}
