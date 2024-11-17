﻿using ProductsAzyavchikava.Views.Intefraces;
using ProductsAzyavchikava.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProductsAzyavchikava.Views
{
    public partial class CompositionRequestView : Form, ICompositionRequestView
    {
        private string? _message;
        private bool _isSuccessful;
        private bool _isEdit;

        public Guid Id
        {
            get => Guid.Parse(IdTxt.Text);
            set => IdTxt.Text = value.ToString();
        }
        public RequestViewModel RequestId
        {
            get => (RequestViewModel)RequestCmb.SelectedItem;
            set => RequestCmb.SelectedItem = value;
        }
        public ProductViewModel ProductId
        {
            get => (ProductViewModel)ProductCmb.SelectedItem;
            set => ProductCmb.SelectedItem = value;
        }
        public int Count
        {
            get
            {
                if (!int.TryParse(CountTxt.Text, out _))
                {
                    return 0;
                }
                else
                {
                    return int.Parse(CountTxt.Text);
                }
            }
            set
            {
                if (value != -1)
                {
                    CountTxt.Text = value.ToString();
                }
                else
                    CountTxt.Text = string.Empty;
            }
        }
        public double Sum
        {
            get
            {
                if (!int.TryParse(SumTxt.Text, out _))
                {
                    return 0;
                }
                else
                {
                    return int.Parse(SumTxt.Text);
                }
            }
            set
            {
                if (value != -1)
                {
                    SumTxt.Text = value.ToString();
                }
                else
                    SumTxt.Text = string.Empty;
            }
        }
        public string searchValue
        {
            get => SearchTxb.Text;
            set => SearchTxb.Text = value;
        }
        public bool IsEdit
        {
            get => _isEdit;
            set => _isEdit = value;
        }
        public bool IsSuccessful
        {
            get => _isSuccessful;
            set => _isSuccessful = value;
        }
        public string Message
        {
            get => _message;
            set => _message = value;
        }

        public event EventHandler SearchEvent;
        public event EventHandler AddNewEvent;
        public event EventHandler EditEvent;
        public event EventHandler DeleteEvent;
        public event EventHandler SaveEvent;
        public event EventHandler CancelEvent;
        public event EventHandler RemainingStockEvent;
        public event EventHandler RequestPrintEvent;
        public event EventHandler RequestOpen;
        public event EventHandler PrintWord;
        public event EventHandler PrintExcel;

        public CompositionRequestView()
        {
            InitializeComponent();
            AssosiateAndRaiseViewEvents();
            tabControl1.TabPages.Remove(tabPage2);
            tabControl1.TabPages.Remove(tabPage3);
            CloseBtn.Click += delegate
            {
                RequestOpen?.Invoke(this, EventArgs.Empty);
            };
            IdTxt.Text = Guid.Empty.ToString();
        }

        private void AssosiateAndRaiseViewEvents()
        {
            //Search
            SearchBtn.Click += delegate { SearchEvent?.Invoke(this, EventArgs.Empty); };
            SearchTxb.KeyDown += (s, e) =>
            {
                if (e.KeyData == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SearchEvent?.Invoke(this, EventArgs.Empty);
                }
            };

            //Add new
            AddBtn.Click += delegate
            {
                AddNewEvent?.Invoke(this, EventArgs.Empty);
                tabControl1.TabPages.Add(tabPage2);
                tabControl1.TabPages.Remove(tabPage1);
                tabPage2.Text = "Добавление";
            };

            //Edit
            EditBtn.Click += delegate
            {
                if (dataGridView1.Rows.Count >= 1)
                {
                    tabControl1.TabPages.Remove(tabPage1);
                    tabControl1.TabPages.Add(tabPage2);
                    EditEvent?.Invoke(this, EventArgs.Empty);
                    tabPage2.Text = "Редактирование";
                }
                else
                {
                    MessageBox.Show("Вы не выбрали запись");
                }
            };

            //Delete
            DeleteBtn.Click += delegate
            {
                var result = MessageBox.Show("Вы уверены что хотите удалить запись?", "Warning",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    DeleteEvent?.Invoke(this, EventArgs.Empty);
                    MessageBox.Show(Message);
                }
            };

            //Save 
            SaveBtn.Click += delegate
            {
                SaveEvent?.Invoke(this, EventArgs.Empty);
                if (IsSuccessful)
                {
                    tabControl1.TabPages.Add(tabPage1);
                    tabControl1.TabPages.Remove(tabPage2);
                }

                MessageBox.Show(Message);
            };

            //Cancel
            CancelBtn.Click += delegate
            {
                CancelEvent?.Invoke(this, EventArgs.Empty);
                tabControl1.TabPages.Add(tabPage1);
                tabControl1.TabPages.Remove(tabPage2);
            };

            CountTxt.KeyPress += (s, e) =>
            {
                if (!Char.IsDigit(e.KeyChar) && e.KeyChar != Convert.ToChar(8))
                {
                    e.Handled = true;
                }
            };

            SumTxt.KeyPress += (s, e) =>
            {
                if (!Char.IsDigit(e.KeyChar) && e.KeyChar != Convert.ToChar(8))
                {
                    e.Handled = true;
                }
            };


            //Print Word
            WordPrint.Click += delegate
            {
                PrintWord?.Invoke(this, EventArgs.Empty);
            };

            //Print Excel
            ExcelPrint.Click += delegate
            {
                PrintExcel?.Invoke(this, EventArgs.Empty);
            };


            CancelPrint.Click += delegate
            {
                tabControl1.TabPages.Add(tabPage1);
                tabControl1.TabPages.Remove(tabPage3);
            };

            RemainingStockBtn.Click += delegate
            {
                RemainingStockEvent.Invoke(this, EventArgs.Empty);
            };

            RequestBtn.Click += delegate
            {
                RequestPrintEvent.Invoke(this, EventArgs.Empty);
            };
        }

        public void SetCompositionBindingSource(BindingSource source)
        {
            dataGridView1.DataSource = source;
            dataGridView1.Columns[0].Visible = false;
            dataGridView1.Columns[1].Visible = false;
            dataGridView1.Columns[2].Visible = false;
        }

        public void SetProductBindingSource(BindingSource source)
        {
            ProductCmb.DataSource = source;
            ProductCmb.DisplayMember = "PName";
            ProductCmb.ValueMember = "Id";
        }

        public void SetRequestBindingSource(BindingSource source)
        {
            RequestCmb.DataSource = source;
            RequestCmb.DisplayMember = "Date";
            RequestCmb.ValueMember = "Id";
        }

        private static CompositionRequestView? instance;

        public static CompositionRequestView GetInstance(Form parentContainer)
        {
            if (instance == null || instance.IsDisposed)
            {
                if (parentContainer.ActiveMdiChild != null)
                    parentContainer.ActiveMdiChild.Close();

                instance = new CompositionRequestView();
                instance.MdiParent = parentContainer;
                instance.FormBorderStyle = FormBorderStyle.None;
                instance.Dock = DockStyle.Fill;
            }
            else
            {
                if (instance.WindowState == FormWindowState.Minimized)
                    instance.WindowState = FormWindowState.Normal;

                instance.BringToFront();
            }

            return instance;
        }
    }
}
