using Microsoft.Office.Interop.Word;
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
    public class ShopTypeController
    {
        private readonly IShop_TypeView _view;
        private readonly IRepository<Shop_TypeViewModel> _repository;
        private readonly IRepository<Product_TypeViewModel> _productRepository;
        private readonly IRepository<ShopViewModel> _shopRepository;

        private BindingSource shopTypeBindingSource;
        private BindingSource shopBindingSource;
        private BindingSource productBindingSource;

        private IEnumerable<Shop_TypeViewModel>? _shopTypes;
        private IEnumerable<ShopViewModel>? _shops;
        private IEnumerable<Product_TypeViewModel>? _products;

        public ShopTypeController(IShop_TypeView view, IRepository<Shop_TypeViewModel> repository, IRepository<Product_TypeViewModel> productRepository, IRepository<ShopViewModel> shopRepository)
        {
            _view = view;
            _repository = repository;
            _productRepository = productRepository;
            _shopRepository = shopRepository;

            shopTypeBindingSource = new BindingSource();
            shopBindingSource = new BindingSource();
            productBindingSource = new BindingSource();

            view.SearchEvent += Search;
            view.AddNewEvent += Add;
            view.EditEvent += LoadSelectedToEdit;
            view.DeleteEvent += DeleteSelected;
            view.SaveEvent += Save;
            view.CancelEvent += CancelAction;
            view.PrintWord += WordAction;
            view.PrintExcel += ExcelAction;

            LoadProductTypeList();
            LoadCombobox();

            view.SetShopTypeBindingSource(shopTypeBindingSource);
            view.SetProductBindingSource(productBindingSource);
            view.SetShopBindingSource(shopBindingSource);

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
            var properties = typeof(Shop_TypeViewModel).GetProperties();
            int startIndex = 3; // Пропускаем первый столбец

            // Добавляем заголовки столбцов с использованием DisplayName
            for (int i = startIndex; i < properties.Length; i++)
            {
                var prop = properties[i];
                var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? (i - startIndex + 1).ToString();
                worksheet.Cells[1, i - startIndex + 1] = displayName;
            }

            // Заполняем строки данными из коллекции, игнорируя первый столбец
            int rowIndex = 2;
            foreach (var item in _shopTypes)
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
            paragraph.Range.Text = "Типы Магазинов";
            paragraph.Range.InsertParagraphAfter();
            // Получаем список свойств (столбцов)
            var properties = typeof(Shop_TypeViewModel).GetProperties();
            int startIndex = 3;

            // Создаем таблицу в Word с количеством строк и столбцов
            int rowCount = 1 + (_shopTypes == null ? 0 : ((ICollection<Shop_TypeViewModel>)_shopTypes).Count);
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
            foreach (var item in _shopTypes)
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
            _shopTypes = _repository.GetAll();
            shopTypeBindingSource.DataSource = _shopTypes;
        }

        private void LoadCombobox()
        {
            _products = _productRepository.GetAll();
            productBindingSource.DataSource = _products;

            _shops = _shopRepository.GetAll();
            shopBindingSource.DataSource = _shops;
        }

        private void CleanViewFields()
        {
            _view.Id = Guid.Empty;
            _view.ProductId = new Product_TypeViewModel();
            _view.ShopId = new ShopViewModel();
            _view.Count = -1;
        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }

        private void Save(object? sender, EventArgs e)
        {
            if (_view.ShopId == null || _view.ProductId == null)
            {
                CleanViewFields();
                _view.Message = "Values are not specified in the combobox";
                return;
            }

            var model = new Shop_TypeViewModel();
            model.Shop_TypeId = _view.Id;
            model.ShopId = _view.ShopId.Id;
            model.Product_TypeId = _view.ProductId.Id;
            model.Shop_Count = _view.Count;

            try
            {
                if (_view.IsEdit)
                {
                    _repository.Update(model);
                    _view.Message = "Shop edited successfuly";
                }
                else
                {
                    _repository.Create(model);
                    _view.Message = "Shop added successfuly";
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
                var model = (Shop_TypeViewModel)shopTypeBindingSource.Current;
                if (model == null)
                {
                    throw new Exception();
                }
                _repository.Delete(model);
                _view.IsSuccessful = true;
                _view.Message = "Shop deleted successfuly";
                LoadProductTypeList();
            }
            catch (Exception)
            {
                _view.IsSuccessful = false;
                _view.Message = "An error ocurred, could not delete Shop";
            }
        }

        private void LoadSelectedToEdit(object? sender, EventArgs e)
        {
            var model = (Shop_TypeViewModel)shopTypeBindingSource.Current;
            _view.Id = model.Shop_TypeId;
            _view.ShopId.Id = model.ShopId;
            _view.ProductId.Id = model.Product_TypeId;
            _view.Count = model.Shop_Count;
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
                _shopTypes = _repository.GetAllByValue(_view.searchValue);
            else
                _shopTypes = _repository.GetAll();

            shopTypeBindingSource.DataSource = _shopTypes;
        }
    }
}
