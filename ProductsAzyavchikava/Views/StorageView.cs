﻿using ProductsAzyavchikava.Views.Intefraces;
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
    public partial class StorageView : Form, IStorageView
    {
        private string? _message;
        private bool _isSuccessful;
        private bool _isEdit;

        public Guid Id
        {
            get => Guid.Parse(IdTxt.Text);
            set => IdTxt.Text = value.ToString();
        }
        public int Identity
        {
            get
            {
                if (!int.TryParse(NumberTxt.Text, out _))
                {
                    return 0;
                }
                else
                {
                    return int.Parse(NumberTxt.Text);
                }
            }
            set
            {
                if (value != -1)
                {
                    NumberTxt.Text = value.ToString();
                }
                else
                    NumberTxt.Text = string.Empty;
            }
        }
        public string Adress
        {
            get => AdressTxt.Text;
            set=> AdressTxt.Text = value;
        }
        public string Purpose
        {
            get => PurposeTxt.Text;
            set => PurposeTxt.Text = value;
        }
        public string searchValue
        {
            get => SearchTxb.Text;
            set=> SearchTxb.Text = value;
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
        public event EventHandler ProductIntoStorageOpen;
        public event EventHandler PrintWord;
        public event EventHandler PrintExcel;

        public StorageView()
        {
            InitializeComponent();
            AssosiateAndRaiseViewEvents();
            tabControl1.TabPages.Remove(tabPage2);
            CloseBtn.Click += delegate { this.Close(); };
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
                tabControl1.TabPages.Remove(tabPage1);
                tabControl1.TabPages.Add(tabPage2);
                EditEvent?.Invoke(this, EventArgs.Empty);
                tabPage2.Text = "Редактирование";
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

            ProductIntoStorageBtn.Click += delegate
            {
                ProductIntoStorageOpen?.Invoke(this, EventArgs.Empty);
            };

            NumberTxt.KeyPress += (s, e) =>
            {
                if (!Char.IsDigit(e.KeyChar) && e.KeyChar != Convert.ToChar(8))
                {
                    e.Handled = true;
                }
            };
        }

        public void SetStorageBindingSource(BindingSource source)
        {
            dataGridView1.DataSource = source;
            dataGridView1.Columns[0].Visible = false;
        }

        private static StorageView? instance;

        public static StorageView GetInstance(Form parentContainer)
        {
            if (instance == null || instance.IsDisposed)
            {
                if (parentContainer.ActiveMdiChild != null)
                    parentContainer.ActiveMdiChild.Close();

                instance = new StorageView();
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
