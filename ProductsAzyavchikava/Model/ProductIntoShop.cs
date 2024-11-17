using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductsAzyavchikava.Model
{
    public class ProductIntoShop
    {
        public Guid ProductIntoShopId { get; set; }

        [Required(ErrorMessage = "Количество это обязательное поле")]
        [Range(1, 10000, ErrorMessage = "Количество должно быть от 1 до 10000")]
        public int Count { get; set; }

        [Required(ErrorMessage = "Товар это обязательное поле")]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Магазин это обязательное поле")]
        public Guid ShopId { get; set; }

        public Product Product { get; set; }
        public Shop Shop { get; set; }
    }
}
