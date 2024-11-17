using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Office.Interop.Word;
using ProductsAzyavchikava.Model;
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

namespace ProductsAzyavchikava.Controllers
{
    public class CompositionRequestController
    {
        private readonly ICompositionRequestView _view;
        private readonly IMainView _mainView;
        private readonly IRepository<CompositionRequestViewModel> _repository;
        private readonly IRepository<ProductViewModel> _productRepository;
        private readonly IRepository<RequestViewModel> _requestRepository;
        private readonly IRepository<StorageViewModel> _storageRepository;

        private BindingSource compositionRequestBindingSource;
        private BindingSource ProductBindingSource;
        private BindingSource RequestBindingSource;

        private IEnumerable<RequestViewModel>? _requests;
        private IEnumerable<ProductViewModel>? _products;
        private IEnumerable<CompositionRequestViewModel>? _composition;

        public CompositionRequestController(ICompositionRequestView view, IRepository<CompositionRequestViewModel> repository, IRepository<ProductViewModel> productRepository, IRepository<RequestViewModel> requestRepository, IRepository<StorageViewModel> storageRepository, IMainView mainView)
        {
            _view = view;
            _repository = repository;
            _productRepository = productRepository;
            _requestRepository = requestRepository;
            _storageRepository = storageRepository;
            _mainView = mainView;

            compositionRequestBindingSource = new BindingSource();
            ProductBindingSource = new BindingSource();
            RequestBindingSource = new BindingSource();

            view.SearchEvent += Search;
            view.AddNewEvent += Add;
            view.EditEvent += LoadSelectedToEdit;
            view.DeleteEvent += DeleteSelected;
            view.SaveEvent += Save;
            view.CancelEvent += CancelAction;
            view.RemainingStockEvent += RemainingStock;
            view.RequestOpen += RequestOpen;
            view.PrintWord += WordAction;
            view.PrintExcel += ExcelAction;

            LoadProductTypeList();
            LoadCombobox();

            view.SetCompositionBindingSource(compositionRequestBindingSource);
            view.SetRequestBindingSource(RequestBindingSource);
            view.SetProductBindingSource(ProductBindingSource);

            _view.Show();
        }

        private void RequestOpen(object? sender, EventArgs e)
        {
            IRequestView view = RequestView.GetInstance((MainView)_mainView);
            IRepository<RequestViewModel> repository = new RequestRepository(new ApplicationContext());
            IRepository<ShopViewModel> shopRepository = new ShopRepository(new ApplicationContext());
            IRepository<StorageViewModel> storageRepository = new StorageRepository(new ApplicationContext());
            new RequestContorller(view, repository, storageRepository, shopRepository, _mainView);
        }

        private void ExcelAction(object? sender, EventArgs e)
        {
            // Создаем новое приложение Excel
            var excelApp = new Excel.Application();
            var workbook = excelApp.Workbooks.Add();
            var worksheet = (Excel.Worksheet)workbook.Sheets[1];
            excelApp.Visible = true;

            // Получаем список свойств, игнорируя первый столбец (GUID)
            var properties = typeof(CompositionRequestViewModel).GetProperties();
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
            foreach (var item in _composition)
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
            paragraph.Range.Text = "Состав заявки";
            paragraph.Range.InsertParagraphAfter();
            // Получаем список свойств (столбцов)
            var properties = typeof(CompositionRequestViewModel).GetProperties();
            int startIndex = 3;

            // Создаем таблицу в Word с количеством строк и столбцов
            int rowCount = 1 + (_composition == null ? 0 : ((ICollection<CompositionRequestViewModel>)_composition).Count);
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
            foreach (var item in _composition)
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
            _composition = _repository.GetAll();
            compositionRequestBindingSource.DataSource = _composition;
        }

        private void LoadCombobox()
        {
            _products = _productRepository.GetAll();
            ProductBindingSource.DataSource = _products;

            _requests = _requestRepository.GetAll();
            RequestBindingSource.DataSource = _requests;
        }

        private void CleanViewFields()
        {
            _view.Id = Guid.Empty;
            _view.RequestId = new RequestViewModel();
            _view.ProductId = new ProductViewModel();
            _view.Count = -1;
            _view.Sum = -1;
        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }

        private void Save(object? sender, EventArgs e)
        {
            if (_view.RequestId == null || _view.ProductId == null)
            {
                CleanViewFields();
                _view.Message = "Values are not specified in the combobox";
                return;
            }

            var model = new CompositionRequestViewModel();
            model.Id = _view.Id;
            model.RequestId = _view.RequestId.Id;
            model.ProductId = _view.ProductId.ProductId;
            model.Sum = _view.Sum;
            model.Count = _view.Count;
            model.ProductVenderCode = _view.ProductId.VendorCode;
            model.ProductCount = _view.RequestId.Products_Count;
            model.Date = _view.RequestId.Date;
            model.ProductName = _view.ProductId.PName;

            try
            {
                if (_view.IsEdit)
                {
                    _repository.Update(model);
                    _view.Message = "Composition edited successfuly";
                }
                else
                {
                    _repository.Create(model);
                    _view.Message = "Composition added successfuly";
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
                var model = (CompositionRequestViewModel)compositionRequestBindingSource.Current;
                if (model == null)
                {
                    throw new Exception();
                }
                _repository.Delete(model);
                _view.IsSuccessful = true;
                _view.Message = "Composition deleted successfuly";
                LoadProductTypeList();
            }
            catch (Exception)
            {
                _view.IsSuccessful = false;
                _view.Message = "An error ocurred, could not delete Composition";
            }
        }

        private void LoadSelectedToEdit(object? sender, EventArgs e)
        {
            var model = (CompositionRequestViewModel)compositionRequestBindingSource.Current;
            _view.Id = model.Id;
            _view.RequestId.Id = model.RequestId;
            _view.ProductId.ProductId = model.ProductId;
            _view.Count = model.Count;
            _view.Sum = model.Sum;
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
                _composition = _repository.GetAllByValue(_view.searchValue);
            else
                _composition = _repository.GetAll();

            compositionRequestBindingSource.DataSource = _composition;
        }


        private void RemainingStock(object? sender, EventArgs e)
        {
            var viewModel = (CompositionRequestViewModel)compositionRequestBindingSource.Current;
            var productViewModel = _productRepository.GetModel(viewModel.ProductId);
            var storageViewModel = _storageRepository.GetModel(productViewModel.StorageId);

            var compositionRequestsList = _repository.GetAllByValue(viewModel.Id.ToString());
            var productList = compositionRequestsList.Select(p => _productRepository.GetModel(p.ProductId)).ToList();


            

            Word.Application wApp = new Word.Application();
            wApp.Visible = true;
            object missing = Type.Missing;
            object falseValue = false;
            Word.Document wordDocument = wApp.Documents.Open(Path.Combine(System.Windows.Forms.Application.StartupPath, Directory.GetCurrentDirectory() + "\\ОстатокНаСкладе.docx"));
            ReplaceWordStub("{Adress}", storageViewModel.Adress, wordDocument);
            ReplaceWordStub("{dateNow}", DateTime.Now.ToShortDateString(), wordDocument);
            Word.Table tb = wordDocument.Tables[1];
            foreach (var rw in productList)
            {
                Word.Row r = tb.Rows.Add();
                r.Cells[1].Range.Text = rw.PName.Trim();
                r.Cells[2].Range.Text = rw.Retail_Price.ToString();
                r.Cells[3].Range.Text = rw.Cost.ToString();
                r.Cells[4].Range.Text = rw.Availability.ToString();

            }
            tb.Rows[2].Delete(); // удаляем пустую строку после шапки таблицы
        }

        private void ReplaceWordStub(string stubToReplace, string text, Word.Document wordDocumet)
        {
            var range = wordDocumet.Content;
            range.Find.ClearFormatting();
            range.Find.Execute(FindText: stubToReplace, ReplaceWith: text);
        }

    }
}
