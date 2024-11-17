using Microsoft.Office.Interop.Word;
using ProductsAzyavchikava.Repositories;
using ProductsAzyavchikava.Views;
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
    public class RequestContorller
    {
        private readonly IRequestView _view;
        private readonly IMainView _mainView;
        private readonly IRepository<RequestViewModel> _repository;
        private readonly IRepository<StorageViewModel> _storageRepository;
        private readonly IRepository<ShopViewModel> _shopRepository;

        private BindingSource requestBindingSource;
        private BindingSource shopBindingSource;
        private BindingSource storageBindingSource;

        private IEnumerable<RequestViewModel>? _requests;
        private IEnumerable<ShopViewModel>? _shops;
        private IEnumerable<StorageViewModel>? _storages;

        public RequestContorller(IRequestView view, IRepository<RequestViewModel> repository, IRepository<StorageViewModel> storageRepository, IRepository<ShopViewModel> shopRepository, IMainView mainView)
        {
            _view = view;
            _repository = repository;
            _storageRepository = storageRepository;
            _shopRepository = shopRepository;
            _mainView = mainView;

            requestBindingSource = new BindingSource();
            shopBindingSource = new BindingSource();
            storageBindingSource = new BindingSource();

            view.SearchEvent += Search;
            view.AddNewEvent += Add;
            view.EditEvent += LoadSelectedToEdit;
            view.DeleteEvent += DeleteSelected;
            view.SaveEvent += Save;
            view.CancelEvent += CancelAction;
            view.SearchWithDateEvent += SearchWithDate;
            view.CompositionRequestOpen += CompositionRequestOpen;
            view.PrintWord += WordAction;
            view.PrintExcel += ExcelAction;

            LoadProductTypeList();
            LoadCombobox();

            view.SetRequestBindingSource(requestBindingSource);
            view.SetStorageBindingSource(storageBindingSource);
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
            var properties = typeof(RequestViewModel).GetProperties();
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
            foreach (var item in _requests)
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
            paragraph.Range.Text = "Заявки";
            paragraph.Range.InsertParagraphAfter();
            // Получаем список свойств (столбцов)
            var properties = typeof(RequestViewModel).GetProperties();
            int startIndex = 3;

            // Создаем таблицу в Word с количеством строк и столбцов
            int rowCount = 1 + (_requests == null ? 0 : ((ICollection<RequestViewModel>)_requests).Count);
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
            foreach (var item in _requests)
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

        private void CompositionRequestOpen(object? sender, EventArgs e)
        {
            ICompositionRequestView view = CompositionRequestView.GetInstance((MainView)_mainView);
            IRepository<CompositionRequestViewModel> repository = new CompositionRequestRepository(new ApplicationContext());
            IRepository<ProductViewModel> productRepository = new ProductRepository(new ApplicationContext());
            IRepository<RequestViewModel> requestRepository = new RequestRepository(new ApplicationContext());
            IRepository<StorageViewModel> storageRepository = new StorageRepository(new ApplicationContext());
            new CompositionRequestController(view, repository, productRepository, requestRepository, storageRepository, _mainView);
        }

        private void LoadProductTypeList()
        {
            _requests = _repository.GetAll();
            requestBindingSource.DataSource = _requests;
        }

        private void LoadCombobox()
        {
            _storages = _storageRepository.GetAll();
            storageBindingSource.DataSource = _storages;

            _shops = _shopRepository.GetAll();
            shopBindingSource.DataSource = _shops;
        }

        private void CleanViewFields()
        {
            _view.Id = Guid.Empty;
            _view.ShopId = new ShopViewModel();
            _view.StorageId = new StorageViewModel();
            _view.Date = DateTime.Now;
            _view.Product_Count = -1;
            _view.Cost = -1;
            _view.Number_Packages = -1;
            _view.Weigh = -1;
            _view.Car = string.Empty;
            _view.Driver = string.Empty;
        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }

        private void Save(object? sender, EventArgs e)
        {
            if (_view.ShopId == null || _view.StorageId == null)
            {
                CleanViewFields();
                _view.Message = "Нет значения в выпадающем списке";
                return;
            }

            var model = new RequestViewModel();
            model.Id = _view.Id;
            model.ShopId = _view.ShopId.Id;
            model.StorageId = _view.StorageId.Id;
            model.Date = _view.Date;
            model.SupplyDate = _view.SupplyDate;
            model.Products_Count = _view.Product_Count;
            model.Cost = _view.Cost;
            model.Number_Packages = _view.Number_Packages;
            model.Weigh = _view.Weigh;
            model.Car = _view.Car;
            model.Driver = _view.Driver;

            try
            {
                if (_view.IsEdit)
                {
                    _repository.Update(model);
                    _view.Message = "Request edited successfuly";
                }
                else
                {
                    _repository.Create(model);
                    _view.Message = "Request added successfuly";
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
                var model = (RequestViewModel)requestBindingSource.Current;
                if (model == null)
                {
                    throw new Exception();
                }
                _repository.Delete(model);
                _view.IsSuccessful = true;
                _view.Message = "Request deleted successfuly";
                LoadProductTypeList();
            }
            catch (Exception)
            {
                _view.IsSuccessful = false;
                _view.Message = "An error ocurred, could not delete Request";
            }
        }

        private void LoadSelectedToEdit(object? sender, EventArgs e)
        {
            var model = (RequestViewModel)requestBindingSource.Current;
            _view.Id = model.Id;
            _view.ShopId.Id = model.ShopId;
            _view.StorageId.Id = model.StorageId;
            _view.Date = model.Date;
            _view.SupplyDate = model.SupplyDate;
            _view.Product_Count = model.Products_Count;
            _view.Cost = model.Cost;
            _view.Number_Packages = model.Number_Packages;
            _view.Weigh = model.Weigh / model.Products_Count;
            _view.Car = model.Car;
            _view.Driver = model.Driver;
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
                _requests = _repository.GetAllByValue(_view.searchValue);
            else
                _requests = _repository.GetAll();

            requestBindingSource.DataSource = _requests;
        }

        private void SearchWithDate(object? sender, EventArgs e)
        {
            _requests = _repository.GetAllByValue(_view.firstDate, _view.lastDate);

            requestBindingSource.DataSource = _requests;
        }
    }
}
