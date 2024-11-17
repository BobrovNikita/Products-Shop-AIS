using ProductsAzyavchikava.Repositories;
using ProductsAzyavchikava.Views.Intefraces;
using ProductsAzyavchikava.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProductsAzyavchikava.Views;
using Word = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.ComponentModel;

namespace ProductsAzyavchikava.Controllers
{
    public class CompositionSellingController
    {
        private readonly ICompositionSellingView _view;
        private readonly IMainView _mainView;
        private readonly ICompositionSellingWithBaseRepository _repository;
        private readonly IRepository<SellViewModel> _sellRepository;
        private readonly IRepository<ProductViewModel> _productRepository;

        private BindingSource compositionSellBindingSource;
        private BindingSource sellBindingSource;
        private BindingSource productBindingSource;

        private IEnumerable<CompositionSellingViewModel>? _compositionSelling;
        private IEnumerable<SellViewModel>? _sell;
        private IEnumerable<ProductViewModel>? _product;

        public CompositionSellingController(ICompositionSellingView view, ICompositionSellingWithBaseRepository repository, IRepository<SellViewModel> sellRepository, IRepository<ProductViewModel> productRepository, IMainView mainView)
        {
            _view = view;
            _repository = repository;
            _sellRepository = sellRepository;
            _productRepository = productRepository;
            _mainView = mainView;

            compositionSellBindingSource = new BindingSource();
            sellBindingSource = new BindingSource();
            productBindingSource = new BindingSource();

            view.SearchEvent += Search;
            view.AddNewEvent += Add;
            view.EditEvent += LoadSelectedToEdit;
            view.DeleteEvent += DeleteSelected;
            view.SaveEvent += Save;
            view.CancelEvent += CancelAction;
            view.SellOpen += SellOpen;
            view.PrintWord += WordAction;
            view.PrintExcel += ExcelAction;

            LoadCompositionSellingList();
            LoadCombobox();

            view.SetCompositionSellingBindingSource(compositionSellBindingSource);
            view.SetProductBindingSource(productBindingSource);
            view.SetSellBindingSource(sellBindingSource);

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
            var properties = typeof(CompositionSellingViewModel).GetProperties();
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
            foreach (var item in _compositionSelling)
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
            paragraph.Range.Text = "Состав продажи";
            paragraph.Range.InsertParagraphAfter();
            // Получаем список свойств (столбцов)
            var properties = typeof(CompositionSellingViewModel).GetProperties();
            int startIndex = 3;

            // Создаем таблицу в Word с количеством строк и столбцов
            int rowCount = 1 + (_compositionSelling == null ? 0 : ((ICollection<CompositionSellingViewModel>)_compositionSelling).Count);
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
            foreach (var item in _compositionSelling)
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



        private void SellOpen(object? sender, EventArgs e)
        {
            ISellView view = SellView.GetInstance((MainView)_mainView);
            IRepository<SellViewModel> repository = new SellRepository(new ApplicationContext());
            IRepository<ShopViewModel> shopRepository = new ShopRepository(new ApplicationContext());
            new SellController(view, repository, shopRepository, _mainView);
        }

        private void LoadCompositionSellingList()
        {
            _compositionSelling = _repository.GetAll();
            compositionSellBindingSource.DataSource = _compositionSelling;
        }

        private void LoadCombobox()
        {
            _product = _productRepository.GetAll();
            productBindingSource.DataSource = _product;

            _sell = _sellRepository.GetAll();
            sellBindingSource.DataSource = _sell;
        }

        private void CleanViewFields()
        {
            _view.Id = Guid.Empty;
            _view.SellId = new SellViewModel();
            _view.ProductId = new ProductViewModel();
            _view.Count = -1;
        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }

        private void Save(object? sender, EventArgs e)
        {
            if (_view.SellId == null || _view.ProductId == null)
            {
                CleanViewFields();
                _view.Message = "Нет значения в выпадающем списке";
                return;
            }

            var model = new CompositionSellingViewModel();
            model.Id = _view.Id;
            model.SellId = _view.SellId.SellId;
            model.ProductId = _view.ProductId.ProductId;
            model.Count = _view.Count;

            if (!_productRepository.GetModel(model.ProductId).Availability)
            {
                _view.Message = "Товара нет в наличии";
                return;
            }

            try
            {
                if (_view.IsEdit)
                {
                    _repository.Update(model);
                    _view.Message = "Состав продажи изменена";
                }
                else
                {
                    _repository.Create(model);
                    _view.Message = "Состав продажи добавлен";
                }
                _view.IsSuccessful = true;
                LoadCompositionSellingList();
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
                var model = (CompositionSellingViewModel)compositionSellBindingSource.Current;
                if (model == null)
                {
                    throw new Exception();
                }
                _repository.Delete(model);
                _view.IsSuccessful = true;
                _view.Message = "Состав продажи удален";
                LoadCompositionSellingList();
            }
            catch (Exception)
            {
                _view.IsSuccessful = false;
                _view.Message = "An error ocurred, could not delete Request";
            }
        }

        private void LoadSelectedToEdit(object? sender, EventArgs e)
        {
            var model = (CompositionSellingViewModel)compositionSellBindingSource.Current;
            _view.Id = model.Id;
            _view.ProductId.ProductId = model.ProductId;
            _view.SellId.SellId = model.SellId;
            _view.Count = model.Count;
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
                _compositionSelling = _repository.GetAllByValue(_view.searchValue);
            else
                _compositionSelling = _repository.GetAll();

            compositionSellBindingSource.DataSource = _compositionSelling;
        }
    }
}
