using ProductsAzyavchikava.Repositories;
using ProductsAzyavchikava.Views.Intefraces;
using ProductsAzyavchikava.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.ComponentModel;

namespace ProductsAzyavchikava.Controllers
{
    public class ProductController
    {
        private readonly IProductView _view;
        private readonly IRepository<ProductViewModel> _repository;
        private readonly IRepository<StorageViewModel> _storageRepository;
        private readonly IRepository<Product_TypeViewModel> _productTypeRepository;

        private BindingSource productBindingSource;
        private BindingSource productTypeBindingSource;
        private BindingSource storageBindingSource;

        private IEnumerable<ProductViewModel>? _products;
        private IEnumerable<Product_TypeViewModel>? _productsType;
        private IEnumerable<StorageViewModel>? _storages;

        public ProductController(IProductView view, IRepository<ProductViewModel> repository, IRepository<StorageViewModel> storageRepository, IRepository<Product_TypeViewModel> productTypeRepository)
        {
            _view = view;
            _repository = repository;
            _storageRepository = storageRepository;
            _productTypeRepository = productTypeRepository;

            productBindingSource = new BindingSource();
            productTypeBindingSource = new BindingSource();
            storageBindingSource = new BindingSource();

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

            view.SetProductBindingSource(productBindingSource);
            view.SetStorageBindingSource(storageBindingSource);
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
            var properties = typeof(ProductViewModel).GetProperties();
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
            foreach (var item in _products)
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
            var properties = typeof(ProductViewModel).GetProperties();
            int startIndex = 3;

            // Создаем таблицу в Word с количеством строк и столбцов
            int rowCount = 1 + (_products == null ? 0 : ((ICollection<ProductViewModel>)_products).Count);
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
            foreach (var item in _products)
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
            _products = _repository.GetAll();
            productBindingSource.DataSource = _products;
        }

        private void LoadCombobox()
        {
            _storages = _storageRepository.GetAll();
            storageBindingSource.DataSource = _storages;

            _productsType = _productTypeRepository.GetAll();
            productTypeBindingSource.DataSource = _productsType;
        }

        private void CleanViewFields()
        {
            _view.Id = Guid.Empty;
            _view.ProductTypeId = new Product_TypeViewModel();
            _view.StorageId = new StorageViewModel();
            _view.PName = string.Empty;
            _view.VendorCode = string.Empty;
            _view.Hatch = string.Empty;
            _view.Cost = -1;
            _view.NDS = -1;
            _view.Markup = -1;
            _view.Production = string.Empty;
            _view.Weight_Per_Price = -1;
            _view.Weight = -1;
            _view.Availability = false;
        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }

        private void Save(object? sender, EventArgs e)
        {
            if (_view.ProductTypeId == null || _view.StorageId == null)
            {
                CleanViewFields();
                _view.Message = "Values are not specified in the combobox";
                return;
            }

            var model = new ProductViewModel();
            model.ProductId = _view.Id;
            model.Product_TypeId = _view.ProductTypeId.Id;
            model.StorageId = _view.StorageId.Id;
            model.PName = _view.PName;
            model.VendorCode = _view.VendorCode;
            model.Hatch = _view.Hatch;
            model.Cost = _view.Cost;
            model.NDS = _view.NDS;
            model.Markup = _view.Markup;
            model.Production = _view.Production;
            model.Weight_Per_Price = _view.Weight_Per_Price;
            model.Weight = _view.Weight;
            model.Availability = _view.Availability;

            try
            {
                if (_view.IsEdit)
                {
                    _repository.Update(model);
                    _view.Message = "Product edited successfuly";
                }
                else
                {
                    _repository.Create(model);
                    _view.Message = "Product added successfuly";
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
                var model = (ProductViewModel)productBindingSource.Current;
                if (model == null)
                {
                    throw new Exception();
                }
                _repository.Delete(model);
                _view.IsSuccessful = true;
                _view.Message = "Product deleted successfuly";
                LoadProductTypeList();
            }
            catch (Exception)
            {
                _view.IsSuccessful = false;
                _view.Message = "An error ocurred, could not delete Product";
            }
        }

        private void LoadSelectedToEdit(object? sender, EventArgs e)
        {
            var model = (ProductViewModel)productBindingSource.Current;
            _view.Id = model.ProductId;
            _view.ProductTypeId.Id = model.Product_TypeId;
            _view.StorageId.Id = model.StorageId;
            _view.PName = model.PName;
            _view.VendorCode = model.VendorCode;
            _view.Hatch = model.Hatch;
            _view.Cost = model.Cost;
            _view.NDS = model.NDS;
            _view.Markup = model.Markup;
            _view.Production = model.Production;
            _view.Weight_Per_Price= model.Weight_Per_Price;
            _view.Weight= model.Weight;
            _view.Availability= model.Availability;
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
                _products = _repository.GetAllByValue(_view.searchValue);
            else
                _products = _repository.GetAll();

            productBindingSource.DataSource = _products;
        }
    }
}
