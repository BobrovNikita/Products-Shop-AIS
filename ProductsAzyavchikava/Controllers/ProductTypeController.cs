﻿using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Office.Interop.Word;
using ProductsAzyavchikava.Model;
using ProductsAzyavchikava.Repositories;
using ProductsAzyavchikava.Views.Intefraces;
using ProductsAzyavchikava.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Word = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.ComponentModel;

namespace ProductsAzyavchikava.Controllers
{
    public class ProductTypeController
    {
        private readonly IProduct_TypeView _view;
        private readonly IRepository<Product_TypeViewModel> _repository;

        private BindingSource productTypeBindingSource;

        private IEnumerable<Product_TypeViewModel>? _product_types;

        public ProductTypeController(IProduct_TypeView view, IRepository<Product_TypeViewModel> repository)
        {
            _view = view;
            _repository = repository;

            productTypeBindingSource = new BindingSource();

            view.SearchEvent += Search;
            view.AddNewEvent += Add;
            view.EditEvent += LoadSelectedToEdit;
            view.DeleteEvent += DeleteSelected;
            view.SaveEvent += Save;
            view.CancelEvent += CancelAction;
            view.PrintWord += WordAction;
            view.PrintExcel += ExcelAction;

            LoadProductTypeList();

            view.SetProductTypeBindingSource(productTypeBindingSource);

            _view.Show();
        }


        private void ExcelAction(object? sender, EventArgs e)
        {
            // Создаем новое приложение Excel
            var excelApp = new Excel.Application();
            var workbook = excelApp.Workbooks.Add();
            var worksheet = (Excel.Worksheet)workbook.Sheets[1];
            excelApp.Visible = true;

            // Получаем список свойств, игнорируя первый столбец (GUID)
            var properties = typeof(Product_TypeViewModel).GetProperties();
            int startIndex = 1; // Пропускаем первый столбец

            // Добавляем заголовки столбцов с использованием DisplayName
            for (int i = startIndex; i < properties.Length; i++)
            {
                var prop = properties[i];
                var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? (i - startIndex + 1).ToString();
                worksheet.Cells[1, i - startIndex + 1] = displayName;
            }

            // Заполняем строки данными из коллекции, игнорируя первый столбец
            int rowIndex = 2;
            foreach (var item in _product_types)
            {
                for (int colIndex = startIndex; colIndex < properties.Length; colIndex++)
                {
                    var value = properties[colIndex].GetValue(item, null)?.ToString() ?? "";
                    worksheet.Cells[rowIndex, colIndex - startIndex + 1] = value;
                }
                rowIndex++;
            }

            // Настройка форматирования и автонастройка ширины колонок
            worksheet.Columns.AutoFit();

            // Освобождение ресурсов
            ReleaseObject(worksheet);
            ReleaseObject(workbook);
            ReleaseObject(excelApp);
        }

        private void WordAction(object? sender, EventArgs e)
        {
            var wordApp = new Word.Application();
            wordApp.Visible = true;
            var document = wordApp.Documents.Add();
            var paragraph = document.Content.Paragraphs.Add();
            paragraph.Range.Text = "Склады";
            paragraph.Range.InsertParagraphAfter();
            // Получаем список свойств (столбцов)
            var properties = typeof(Product_TypeViewModel).GetProperties();
            int startIndex = 1;

            // Создаем таблицу в Word с количеством строк и столбцов
            int rowCount = 1 + (_product_types == null ? 0 : ((ICollection<Product_TypeViewModel>)_product_types).Count);
            var table = document.Tables.Add(paragraph.Range, rowCount, properties.Length - startIndex);
            table.Borders.Enable = 1;

            // Добавляем заголовки столбцов с использованием DisplayName
            for (int i = startIndex; i < properties.Length; i++)
            {
                var prop = properties[i];
                var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? (i - startIndex + 1).ToString();

                table.Cell(1, i - startIndex + 1).Range.Text = displayName;
                table.Cell(1, i - startIndex + 1).Range.Bold = 1;
                table.Cell(1, i - startIndex + 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            }

            // Заполняем строки данными из коллекции, игнорируя первый столбец
            int rowIndex = 2;
            foreach (var item in _product_types)
            {
                for (int colIndex = startIndex; colIndex < properties.Length; colIndex++)
                {
                    var value = properties[colIndex].GetValue(item, null)?.ToString() ?? "";
                    table.Cell(rowIndex, colIndex - startIndex + 1).Range.Text = value;
                }
                rowIndex++;
            }

            // Освобождение ресурсов
            ReleaseObject(table);
            ReleaseObject(paragraph);
            ReleaseObject(document);
            ReleaseObject(wordApp);

        }

        private void ReleaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception ex)
            {
                obj = null;
                MessageBox.Show("Ошибка при освобождении объекта: " + ex.Message);
            }
            finally
            {
                GC.Collect();
            }
        }


        private void LoadProductTypeList()
        {
            _product_types = _repository.GetAll();
            productTypeBindingSource.DataSource = _product_types;
        }

        private void CleanViewFields()
        {
            _view.Product_Type_Id = Guid.Empty;
            _view.Product_Type_Name = string.Empty;
            _view.Product_Type_Type = string.Empty;
        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }

        private void Save(object? sender, EventArgs e)
        {
            var model = new Product_TypeViewModel();
            model.Id = _view.Product_Type_Id;
            model.Name = _view.Product_Type_Name;
            model.Type = _view.Product_Type_Type;
            try
            {
                if (_view.IsEdit)
                {
                    _repository.Update(model);
                    _view.Message = "Product type edited successfuly";
                }
                else
                {
                    _repository.Create(model);
                    _view.Message = "Product type added successfuly";
                }
                _view.IsSuccessful = true;
                LoadProductTypeList();
                CleanViewFields();
            }
            catch (Exception ex)
            {
                _view.IsSuccessful = false;
                _view.Message = ex.Message;
            }
        }

        private void DeleteSelected(object? sender, EventArgs e)
        {
            try
            {
                var model = (Product_TypeViewModel)productTypeBindingSource.Current;

                _repository.Delete(model);
                _view.IsSuccessful = true;
                _view.Message = "Product type deleted successfuly";
                LoadProductTypeList();
            }
            catch (Exception)
            {
                _view.IsSuccessful = false;
                _view.Message = "An error ocurred, could not delete Product type";
            }
        }

        private void LoadSelectedToEdit(object? sender, EventArgs e)
        {
            var model = (Product_TypeViewModel)productTypeBindingSource.Current;
            _view.Product_Type_Id = model.Id;
            _view.Product_Type_Name = model.Name;
            _view.Product_Type_Type = model.Type;
            _view.IsEdit = true;
        }

        private void Add(object? sender, EventArgs e)
        {
            _view.IsEdit = false;
        }

        private void Search(object? sender, EventArgs e)
        {
            bool emptyValue = String.IsNullOrWhiteSpace(_view.searchValue);

            if (emptyValue == false)
                _product_types = _repository.GetAllByValue(_view.searchValue);
            else
                _product_types = _repository.GetAll();

            productTypeBindingSource.DataSource = _product_types;
        }
    }
}
