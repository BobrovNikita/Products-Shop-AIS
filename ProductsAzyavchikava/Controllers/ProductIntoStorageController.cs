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

namespace ProductsAzyavchikava.Controllers
{
    public class ProductIntoStorageController
    {
        private readonly IProductIntoStorageView _view;
        private readonly IMainView _mainView;
        private readonly IRepository<ProductIntoStorageViewModel> _repository;
        private readonly IRepository<ProductViewModel> _productRepository;
        private readonly IRepository<StorageViewModel> _storageRepository;

        private BindingSource ProductIntoStorageBindingSource;
        private BindingSource ProductBindingSource;
        private BindingSource StorageBindingSource;

        private IEnumerable<ProductIntoStorageViewModel>? _productsInStorage;
        private IEnumerable<ProductViewModel>? _products;
        private IEnumerable<StorageViewModel>? _storage;

        public ProductIntoStorageController(IProductIntoStorageView view, IRepository<ProductIntoStorageViewModel> repository, IRepository<ProductViewModel> productRepository, IRepository<StorageViewModel> storageRepository, IMainView mainView)
        {
            _view = view;
            _repository = repository;
            _productRepository = productRepository;
            _storageRepository = storageRepository;
            _mainView = mainView;


            ProductIntoStorageBindingSource = new BindingSource();
            ProductBindingSource = new BindingSource();
            StorageBindingSource = new BindingSource();

            view.SearchEvent += Search;
            view.AddNewEvent += Add;
            view.EditEvent += LoadSelectedToEdit;
            view.DeleteEvent += DeleteSelected;
            view.SaveEvent += Save;
            view.CancelEvent += CancelAction;
            view.StorageOpen += StorageOpen;

            LoadProductTypeList();
            LoadCombobox();

            view.SetProductIntoStorageBindingSource(ProductIntoStorageBindingSource);
            view.SetStorageBindingSource(StorageBindingSource);
            view.SetProductBindingSource(ProductBindingSource);

            _view.Show();
            
        }

        private void StorageOpen(object? sender, EventArgs e)
        {
            IStorageView view = StorageView.GetInstance((MainView)_mainView);
            IRepository<StorageViewModel> repository = new StorageRepository(new ApplicationContext());
            new StorageCotnroller(view, repository, _mainView);
        }

        private void LoadProductTypeList()
        {
            _productsInStorage = _repository.GetAll();
            ProductIntoStorageBindingSource.DataSource = _productsInStorage;
        }

        private void LoadCombobox()
        {
            _products = _productRepository.GetAll();
            ProductBindingSource.DataSource = _products;

            _storage = _storageRepository.GetAll();
            StorageBindingSource.DataSource = _storage;
        }

        private void CleanViewFields()
        {
            _view.Id = Guid.Empty;
            _view.StorageId = new StorageViewModel();
            _view.ProductId = new ProductViewModel();
            _view.Count = -1;
        }

        private void CancelAction(object? sender, EventArgs e)
        {
            CleanViewFields();
        }

        private void Save(object? sender, EventArgs e)
        {
            if (_view.StorageId == null || _view.ProductId == null)
            {
                CleanViewFields();
                _view.Message = "Нет значения в выпадающем списке";
                return;
            }

            var model = new ProductIntoStorageViewModel();
            model.Id = _view.Id;
            model.StorageId = _view.StorageId.Id;
            model.ProductId = _view.ProductId.ProductId;
            model.Count = _view.Count;

            try
            {
                if (_view.IsEdit)
                {
                    _repository.Update(model);
                    _view.Message = "Товар обновлен";
                }
                else
                {
                    _repository.Create(model);
                    _view.Message = "Товар добавлен";
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
                var model = (ProductIntoStorageViewModel)ProductIntoStorageBindingSource.Current;
                if (model == null)
                {
                    throw new Exception();
                }
                _repository.Delete(model);
                _view.IsSuccessful = true;
                _view.Message = "Товар удален успешно";
                LoadProductTypeList();
            }
            catch (Exception)
            {
                _view.IsSuccessful = false;
                _view.Message = "An error ocurred, could not delete Product into shop";
            }
        }

        private void LoadSelectedToEdit(object? sender, EventArgs e)
        {
            var model = (ProductIntoStorageViewModel)ProductIntoStorageBindingSource.Current;
            _view.Id = model.Id;
            _view.StorageId.Id = model.StorageId;
            _view.ProductId.ProductId = model.ProductId;
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
                _productsInStorage = _repository.GetAllByValue(_view.searchValue);
            else
                _productsInStorage = _repository.GetAll();

            ProductIntoStorageBindingSource.DataSource = _productsInStorage;
        }
    }
}
