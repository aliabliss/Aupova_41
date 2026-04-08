using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Aupova_41.Manager;

namespace Aupova_41
{
    /// <summary>
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        private List<Product> allProducts;
        private List<OrderProduct> selectedOrderProducts = new List<OrderProduct>();
        private List<Product> selectedProducts = new List<Product>();
        public ProductPage(User user)
        {
            InitializeComponent();
            FIOTB.Text = user.UserSurname + " " + user.UserName + " " + user.UserPatronymic;
            switch (user.UserRole)
            {
                case 1:
                    RoleTB.Text = "Клиент";break;
                case 2:
                    RoleTB.Text = "Менеджер"; break;
                case 3:
                    RoleTB.Text = "Администратор"; break;
            }
            var currentProduct = aupova_41Entities.GetContext().Product.ToList();
            allProducts = aupova_41Entities.GetContext().Product.ToList();
            ProductListView.ItemsSource = allProducts;

            ComboType.SelectedIndex = 0;
            UpdateProduct();
            UpdateOrderButton();
        }
        public void ClearCart()
        {
            selectedOrderProducts.Clear();
            selectedProducts.Clear();
            UpdateOrderButton();

            // Дополнительно обновляем интерфейс
            if (ProductListView != null)
            {
                ProductListView.Items.Refresh();
            }
        }
        private void UpdateStatus()
        {
            int displayedCount = ProductListView.Items.Count;
            int totalCount = allProducts.Count;
            Status.Text = "кол-во " + displayedCount + " из " + totalCount;
        }
        private void UpdateProduct()
        {
            var currentProduct = allProducts.ToList();

            if (ComboType.SelectedIndex == 0)
            {
                currentProduct = currentProduct.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 0 && Convert.ToInt32(p.ProductDiscountAmount) <= 100)).ToList();
            }
            if (ComboType.SelectedIndex == 1)
            {
                currentProduct = currentProduct.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 0 && Convert.ToInt32(p.ProductDiscountAmount) < 9.99)).ToList();
            }
            if (ComboType.SelectedIndex == 2)
            {
                currentProduct = currentProduct.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 10 && Convert.ToInt32(p.ProductDiscountAmount) < 14.99)).ToList();
            }
            if (ComboType.SelectedIndex == 3)
            {
                currentProduct = currentProduct.Where(p => (Convert.ToInt32(p.ProductDiscountAmount) >= 15 && Convert.ToInt32(p.ProductDiscountAmount) < 100)).ToList();
            }
            
            
            currentProduct = currentProduct.Where(p => p.ProductName.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList();

            ProductListView.ItemsSource = currentProduct.ToList();

            if (RButtonDown.IsChecked == true)
            {
                currentProduct = currentProduct.OrderByDescending(p => p.ProductCost).ToList();
            }
            else if (RButtonUp.IsChecked == true)
            {
                currentProduct = currentProduct.OrderBy(p => p.ProductCost).ToList();
            }
            ProductListView.ItemsSource = currentProduct;
            UpdateStatus();
        }
        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProduct();
        }
        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProduct();
        }
        private void RButtonDown_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProduct();
        }
        private void RButtonUp_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProduct();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
            {
            Manager.MainFrame.Navigate(new AddEditPage());
            }
       
        private void ProductListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void UpdateOrderButton()
        {
            if (selectedOrderProducts.Count > 0)
            {
                ViewOrderBtn.Visibility = Visibility.Visible;
                ViewOrderBtn.Content = $"Корзина ({selectedOrderProducts.Count})";
            }
            else
            {
                ViewOrderBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void AddToOrderMenuItem_Click(object sender, RoutedEventArgs e)
        {
           
            
                var selectedProduct = ProductListView.SelectedItem as Product;
                if (selectedProduct == null) return;

                // Проверяем, есть ли уже такой товар
                var existingOrderProduct = selectedOrderProducts
                    .FirstOrDefault(op => op.ProductArticleNumber == selectedProduct.ProductArticleNumber);

                if (existingOrderProduct == null)
                {
                    // Добавляем новый товар
                    var newOrderProduct = new OrderProduct
                    {
                        ProductArticleNumber = selectedProduct.ProductArticleNumber,
                        OrderProductCount = 1
                    };
                   
                    selectedOrderProducts.Add(newOrderProduct);
                    selectedProducts.Add(selectedProduct);

                    // Клонируем товар для отображения
                    

                    ViewOrderBtn.Visibility = Visibility.Visible;
                    ViewOrderBtn.Content = $"Корзина ({selectedOrderProducts.Count})";

                    MessageBox.Show($"Товар '{selectedProduct.ProductName}' добавлен в заказ",
                                  "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    existingOrderProduct.OrderProductCount++;

                    MessageBox.Show($"Количество товара '{selectedProduct.ProductName}' увеличено до {existingOrderProduct.OrderProductCount}",
                                  "Обновлено", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Обновляем кнопку
                UpdateOrderButton();
                ProductListView.SelectedIndex = -1;
            
            
        }
        private void OrderBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenOrderWindow();
        }


        private void ViewOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenOrderWindow();
        }
        private void OpenOrderWindow()
        {
            try
            {
                if (selectedOrderProducts.Count == 0)
                {
                    MessageBox.Show("Добавьте товары в заказ!", "Информация",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Получаем ФИО пользователя
                string clientFIO = FIOTB.Text;

                // Создаем список продуктов для заказа
                var productsForOrder = new List<Product>();
                using (var context = new aupova_41Entities())
                {
                    foreach (var orderProduct in selectedOrderProducts)
                    {
                        var product = context.Product
                            .FirstOrDefault(p => p.ProductArticleNumber == orderProduct.ProductArticleNumber);
                        if (product != null)
                        {
                            // ИСПРАВЛЕНО: Устанавливаем количество в заказе напрямую из OrderProductCount
                            product.ProductQuantityInStock = orderProduct.OrderProductCount;
                            productsForOrder.Add(product);
                        }
                    }
                }
                User currentUser = UserSession.CurrentUser;
                if (currentUser == null)
                {
                    currentUser = new User
                    {
                        UserID = 0,
                        UserLogin = "guest",
                        UserPassword = "",
                        UserName = "Гость",
                        UserSurname = "",
                        UserPatronymic = "",
                        UserRole = 1
                    };
                }
                // Создаем и показываем окно заказа
                var orderWindow = new OrderWindow(selectedOrderProducts, productsForOrder, clientFIO, currentUser);
                orderWindow.Closed += OrderWindow_Closed;
                orderWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }







        // Обновление контекстного меню
        private void UpdateContextMenu()
        {
            if (ProductListView.ContextMenu is ContextMenu contextMenu)
            {
                foreach (var item in contextMenu.Items)
                {
                    if (item is MenuItem menuItem && menuItem.Header.ToString().Contains("Просмотреть заказ"))
                    {
                        menuItem.IsEnabled = selectedProducts.Count > 0;
                        menuItem.Header = selectedProducts.Count > 0
                            ? $"Корзина ({selectedProducts.Count})"
                            : "Корзина";
                    }
                }
            }
        }

        // Обработчик закрытия окна заказа
        private void OrderWindow_Closed(object sender, EventArgs e)
        {
            var orderWindow = sender as OrderWindow;

            // Проверяем, был ли заказ успешно оформлен
            bool orderSuccess = orderWindow != null && orderWindow.DialogResult == true;

            if (orderSuccess)
            {
                // Очищаем корзину
                selectedOrderProducts.Clear();
                selectedProducts.Clear();

                // Обновляем кнопку
                UpdateOrderButton();

                // Обновляем список товаров
                UpdateProduct();

                MessageBox.Show("Заказ успешно оформлен! Корзина очищена.", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (orderWindow != null && orderWindow.DialogResult == false)
            {
                // Заказ не был оформлен, но всё равно обновляем кнопку на всякий случай
                UpdateOrderButton();
            }
        }


    }
}
