using ProductsAzyavchikava.Repositories;
using ProductsAzyavchikava.Views;
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
    public class SellController
    {
        private readonly ISellView _view;
        private readonly IMainView _mainView;
        private readonly IRepository<SellViewModel> _repository;
        private readonly IRepository<ShopViewModel> _shopRepository;

        private BindingSource sellBindingSource;
        private BindingSource shopBindingSource;

        private IEnumerable<SellViewModel>? _sells;
        private IEnumerable<ShopViewModel>? _shops;

        public SellController(ISellView view, IRepository<SellViewModel> repository, IRepository<ShopViewModel> shopRepository, IMainView mainView)
        {
            _view = view;
            _repository = repository;
            _shopRepository = shopRepository;
            _mainView = mainView;

            sellBindingSource = new BindingSource();
            shopBindingSource = new BindingSource();

            view.SearchEvent += Search;
            view.AddNewEvent += Add;
            view.EditEvent += LoadSelectedToEdit;
            view.DeleteEvent += DeleteSelected;
            view.SaveEvent += Save;
            view.CancelEvent += CancelAction;
            view.CompositionSellingShow += CompositionSellingOpen;
            view.PrintWord += WordAction;
            view.PrintExcel += ExcelAction;

            LoadSellsList();
            LoadCombobox();

            view.SetSellBindingSource(sellBindingSource);
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
            var properties = typeof(SellViewModel).GetProperties();
            int startIndex = 2; // Пропускаем первый столбец

            // Добавляем заголовки столбцов с использованием DisplayName
            for (int i = startIndex; i < properties.Length; i++)
            {
                var prop = properties[i];
                var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? (i - startIndex + 1).ToString();
                worksheet.Cells[1, i - startIndex + 1] = displayName;
            }

            // Заполняем строки данными из коллекции, игнорируя первый столбец
            int rowIndex = 2;
            foreach (var item in _sells)
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
            paragraph.Range.Text = "Продажи";
            paragraph.Range.InsertParagraphAfter();
            // Получаем список свойств (столбцов)
            var properties = typeof(SellViewModel).GetProperties();
            int startIndex = 2;

            // Создаем таблицу в Word с количеством строк и столбцов
            int rowCount = 1 + (_sells == null ? 0 : ((ICollection<SellViewModel>)_sells).Count);
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
            foreach (var item in _sells)
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

        private void CompositionSellingOpen(object? sender, EventArgs e)
        {
            ICompositionSellingView view = CompositionSellingView.GetInstance((MainView)_mainView);
            ICompositionSellingWithBaseRepository repository = new CompositionSellingRepository(new ApplicationContext());
            IRepository<SellViewModel> sellRepository = new SellRepository(new ApplicationContext());
            IRepository<ProductViewModel> productRepository = new ProductRepository(new ApplicationContext());
            new CompositionSellingController(view, repository, sellRepository, productRepository, _mainView);
        }

        private void LoadSellsList()
        {
            _sells = _repository.GetAll();
            sellBindingSource.DataSource = _sells;
        }

        private void LoadCombobox()
        {
            _shops = _shopRepository.GetAll();
            shopBindingSource.DataSource = _shops;
        }

        private void CleanViewFields()
        {
            _view.Id = Guid.Empty;
            _view.ShopId = new ShopViewModel();
            _view.PaymentMethod = "Наличные";
            _view.Date = DateTime.Now;
            _view.FIOSalesman = string.Empty;
        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }

        private void Save(object? sender, EventArgs e)
        {
            if (_view.ShopId == null)
            {
                CleanViewFields();
                _view.Message = "Нет значения в выпадающем списке";
                return;
            }

            var model = new SellViewModel();
            model.SellId = _view.Id;
            model.ShopId = _view.ShopId.Id;
            model.Date = _view.Date;
            model.FIOSalesman = _view.FIOSalesman;
            model.PaymentMethod = _view.PaymentMethod;

            try
            {
                if (_view.IsEdit)
                {
                    _repository.Update(model);
                    _view.Message = "Продажа отредактирована успешно";
                }
                else
                {
                    _repository.Create(model);
                    _view.Message = "Продажа добавлена успешно";
                }
                _view.IsSuccessful = true;
                LoadSellsList();
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
                var model = (SellViewModel)sellBindingSource.Current;
                if (model == null)
                {
                    throw new Exception();
                }
                _repository.Delete(model);
                _view.IsSuccessful = true;
                _view.Message = "Продажа удалена успешно";
                LoadSellsList();
            }
            catch (Exception)
            {
                _view.IsSuccessful = false;
                _view.Message = "Произошла ошибка, не удалось удалить запись";
            }
        }

        private void LoadSelectedToEdit(object? sender, EventArgs e)
        {
            var model = (SellViewModel)sellBindingSource.Current;
            _view.Id = model.SellId;
            _view.ShopId.Id = model.ShopId;
            _view.Date = model.Date;
            _view.PaymentMethod = model.PaymentMethod;
            _view.FIOSalesman = model.FIOSalesman;
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
                _sells = _repository.GetAllByValue(_view.searchValue);
            else
                _sells = _repository.GetAll();

            sellBindingSource.DataSource = _sells;
        }
    }
}
