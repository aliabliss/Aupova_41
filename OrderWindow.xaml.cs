using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Aupova_41
{
    /// <summary>
    /// Логика взаимодействия для OrderWindow.xaml
    /// </summary>
    public partial class OrderWindow : Window
    {
        List<OrderProduct> selectedOrderProducts = new List<OrderProduct>();
        List<Product> selectedProducts = new List<Product>();
        private Order currentOrder = new Order();
        private OrderProduct currentOrderProduct = new OrderProduct();
        public OrderWindow(List<OrderProduct> selectedOrderProducts, List<Product> selectedProducts, string FIO)
        {
            InitializeComponent();
            var currentPickups = aupova_41Entities.GetContext().PickUpPoint.ToList();
            ComboPickUpPoint.ItemsSource = currentPickups;

            FIOTB.Text = FIO;
            NUMBERTB.Text = selectedOrderProducts.First().OrderID.ToString();

            
            ShoeListView.ItemsSource = selectedProducts;
            foreach (Product p in selectedProducts)
            {
               
                p.ProductQuantityInStock = 1;
                foreach (OrderProduct q in selectedOrderProducts)
                {
                    if (p.ProductArticleNumber == q.ProductArticleNumber)
                        p.ProductQuantityInStock = q.ProductQuantityInStock;
                }
            }
            this.selectedOrderProducts = selectedOrderProducts;
            this.selectedProducts = selectedProducts;
            DateOrder.Text = DateTime.Now.ToString();
            SetDateDelivery();
            

        }

        private void SetDateDelivery()
        {
            DateTime OrderDate = DateTime.Now;

            int ProductQuantityInStock = selectedProducts.Count(p => p.ProductQuantityInStock > 3);

            if (ProductQuantityInStock == selectedProducts.Count && selectedProducts.Count > 0)
            {
                DateDelivery.SelectedDate = OrderDate.AddDays(3);

                DateDelivery.ToolTip = "Срок доставки: 3 дня (все позиции в наличии > 3 шт.)";
            }
            else
            {
                DateDelivery.SelectedDate = OrderDate.AddDays(6);

                DateDelivery.ToolTip = "Срок доставки: 6 дней (не все позиции в наличии или недостаточное количество)";

            }
            DateDelivery.DisplayDateStart = OrderDate.AddDays(1);
        }

        private void ShoeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            currentOrder.OrderClientID = FIOTB;
        }

        private void ComboPickUpPoint_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            var prod = (sender as Button).DataContext as Product;
            prod.ProductQuantityInStock++;
            var selectedOP = selectedOrderProducts.FirstOrDefault(p => p.ProductArticleNumber == prod.ProductArticleNumber);
            int index = selectedOrderProducts.IndexOf(selectedOP);
            selectedOrderProducts[index].ProductQuantityInStock++;
            SetDateDelivery();
            ShoeListView.Items.Refresh();
        }

    }
}
