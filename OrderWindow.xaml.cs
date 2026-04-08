using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Windows.Threading;
using static Aupova_41.Manager;

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
        private User _currentUser;
        private int generatedOrderCode; // Добавьте это поле

        public OrderWindow(List<OrderProduct> selectedOrderProducts, List<Product> selectedProducts, string FIO, User currentUser)
        {
            InitializeComponent();
            DateOrder.SelectedDate = DateTime.Today;

            var currentPickups = aupova_41Entities.GetContext().PickUpPoint.ToList();
            ComboPickUpPoint.DisplayMemberPath = "FullAddress";
            ComboPickUpPoint.ItemsSource = currentPickups;


            FIOTB.Text = FIO;
            _currentUser = UserSession.CurrentUser;


            generatedOrderCode = GenerateRandomOrderCode();
            NUMBERTB.Text = generatedOrderCode.ToString();
            NUMBERTB.FontWeight = FontWeights.Bold;

            ShoeListView.ItemsSource = selectedProducts;


            foreach (Product p in selectedProducts)
            {
                // Устанавливаем количество по умолчанию
                p.ProductQuantityInStock = 1;

                // Ищем соответствующую запись в заказе
                var orderProduct = selectedOrderProducts?
                    .FirstOrDefault(q => p.ProductArticleNumber == q.ProductArticleNumber);

                // ИСПРАВЛЕНО: OrderProductCount теперь int, не нужно парсить
                if (orderProduct != null)
                {
                    p.ProductQuantityInStock = orderProduct.OrderProductCount;
                }
            }


            this.selectedOrderProducts = selectedOrderProducts ?? new List<OrderProduct>();
            this.selectedProducts = selectedProducts ?? new List<Product>();

            DateOrder.SelectedDate = DateTime.Now;
            SetDateDelivery();
            UpdateOrderSummary();
        }

        private int GenerateRandomOrderCode()
        {
            Random random = new Random();
            return random.Next(1, 1000); // Возвращает число от 1 до 1000
        }



        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {


            try
            {
                var context = aupova_41Entities.GetContext();

                // Создаем новый заказ
                Order newOrder = new Order();
                newOrder.OrderDate = DateOrder.SelectedDate ?? DateTime.Now;
                newOrder.OrderDeliveryDate = DateDelivery.SelectedDate ?? DateTime.Now.AddDays(6);
                newOrder.OrderStatus = "Новый";

                // Устанавливаем ID клиента
                if (_currentUser != null && _currentUser.UserID > 0)
                {
                    newOrder.OrderClientID = _currentUser.UserID;
                }
                newOrder.OrderCode = generatedOrderCode;


                // Проверяем, не существует ли уже такой код в БД
                bool codeExists = context.Order.Any(o => o.OrderCode == generatedOrderCode);
                if (codeExists)
                {
                    // Если код уже существует, генерируем новый
                    generatedOrderCode = GenerateUniqueOrderCode(context);
                    newOrder.OrderCode = generatedOrderCode;
                    NUMBERTB.Text = generatedOrderCode.ToString();
                    MessageBox.Show($"Код {generatedOrderCode} уже существует. Присвоен новый код: {generatedOrderCode}",
                                  "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Получаем выбранный пункт выдачи
                var selectedPickup = ComboPickUpPoint.SelectedItem as PickUpPoint;
                if (selectedPickup != null)
                {
                    newOrder.OrderPickupPoint = selectedPickup.PickUpPointID;
                }
                else
                {
                    MessageBox.Show("Выберите пункт выдачи заказа!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Добавляем заказ
                context.Order.Add(newOrder);
                context.SaveChanges();

                // ОБНОВЛЯЕМ НОМЕР ЗАКАЗА В UI (показываем сгенерированный код)
                NUMBERTB.Text = newOrder.OrderCode.ToString();
                NUMBERTB.FontWeight = FontWeights.Bold;

                // Добавляем товары в заказ
                foreach (var product in selectedProducts)
                {
                    var orderProduct = new OrderProduct
                    {
                        OrderID = newOrder.OrderID,
                        ProductArticleNumber = product.ProductArticleNumber,
                        OrderProductCount = product.ProductQuantityInStock
                    };
                    context.OrderProduct.Add(orderProduct);
                }

                context.SaveChanges();

                MessageBox.Show($"Заказ успешно сохранен!\nНомер заказа (код): {newOrder.OrderCode}", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                this.Close();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                string errorMessage = "Ошибки валидации:\n";
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessage += $"- {validationError.PropertyName}: {validationError.ErrorMessage}\n";
                    }
                }
                MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для генерации уникального кода заказа от 1 до 1000
        private int GenerateUniqueOrderCode(aupova_41Entities context)
        {
            Random random = new Random();
            int maxAttempts = 1000;

            var existingCodes = context.Order
                .Where(o => o.OrderCode != null && o.OrderCode != 0)
                .Select(o => o.OrderCode)
                .ToList();

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int code = random.Next(1, 1001);
                if (!existingCodes.Contains(code))
                {
                    return code;
                }
            }

            // Если все коды заняты, ищем первый свободный
            for (int code = 1; code <= 1000; code++)
            {
                if (!existingCodes.Contains(code))
                {
                    return code;
                }
            }

            return 1;
        }

        private void SetDateDelivery()
        {
            DateTime today = DateTime.Now;
            bool allProductsHaveEnoughStock = true;

            // Проверяем остатки на складе для каждого товара в заказе
            foreach (Product p in selectedProducts)
            {
                var realProduct = aupova_41Entities.GetContext().Product
                    .FirstOrDefault(prod => prod.ProductArticleNumber == p.ProductArticleNumber);

                if (realProduct == null)
                {
                    // Товар отсутствует в БД
                    allProductsHaveEnoughStock = false;
                    break;
                }
                // Если заказывают больше, чем есть на складе
                if (realProduct.ProductQuantityInStock < p.ProductQuantityInStock)
                {
                    allProductsHaveEnoughStock = false;
                    break;
                }
                if (realProduct.ProductQuantityInStock < 3)
                {
                    // Товара меньше 3 штук на складе
                    allProductsHaveEnoughStock = false;
                    break;
                }
            }

            // Устанавливаем дату доставки
            if (allProductsHaveEnoughStock && selectedProducts.Count > 0)
            {
                // Все товары в наличии ≥ 3 штук
                DateDelivery.SelectedDate = today.AddDays(3);
                DateDelivery.ToolTip = $"Срок доставки: 3 дня\nВсе товары в наличии (≥ 3 шт.)";

            }
            else
            {
                // Хотя бы один товар отсутствует или его < 3 на складе
                DateDelivery.SelectedDate = today.AddDays(6);
                DateDelivery.ToolTip = $"Срок доставки: 6 дней\nНекоторые товары в наличии менее 3 позиций или отсутствуют";

            }

            DateDelivery.DisplayDateStart = today.AddDays(1);
            DateDelivery.DisplayDateEnd = today.AddDays(30);
        }
        // Метод для генерации уникального кода заказа от 1 до 1000

        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.DataContext as Product;

            if (product != null)
            {
                product.ProductQuantityInStock++;

                var orderProduct = selectedOrderProducts
                    .FirstOrDefault(p => p.ProductArticleNumber == product.ProductArticleNumber);

                if (orderProduct != null)
                {
                    // ИСПРАВЛЕНО: OrderProductCount теперь int
                    orderProduct.OrderProductCount = product.ProductQuantityInStock;
                }
                else
                {
                    var newOrderProduct = new OrderProduct
                    {
                        ProductArticleNumber = product.ProductArticleNumber,
                        OrderProductCount = product.ProductQuantityInStock,  // Убрали .ToString()
                        OrderID = currentOrder.OrderID > 0 ? currentOrder.OrderID : 0
                    };
                    selectedOrderProducts.Add(newOrderProduct);
                }

                SetDateDelivery();
                UpdateOrderSummary();
                ShoeListView.Items.Refresh();
            }
        }

        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.DataContext as Product;

            if (product != null && product.ProductQuantityInStock > 0)
            {
                product.ProductQuantityInStock--;

                var orderProduct = selectedOrderProducts
                    .FirstOrDefault(op => op.ProductArticleNumber == product.ProductArticleNumber);

                if (orderProduct != null)
                {
                    // ИСПРАВЛЕНО: OrderProductCount теперь int
                    orderProduct.OrderProductCount = product.ProductQuantityInStock;

                    if (product.ProductQuantityInStock == 0)
                    {
                        selectedOrderProducts.Remove(orderProduct);
                        selectedProducts.Remove(product);
                    }
                }

                SetDateDelivery();
                UpdateOrderSummary();
                ShoeListView.Items.Refresh();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.Tag as Product;

            if (product != null)
            {
                var orderProduct = selectedOrderProducts
                    .FirstOrDefault(op => op.ProductArticleNumber == product.ProductArticleNumber);

                if (orderProduct != null)
                {
                    selectedOrderProducts.Remove(orderProduct);
                }

                selectedProducts.Remove(product);
                SetDateDelivery();
                UpdateOrderSummary();
                ShoeListView.Items.Refresh();
            }
        }

        private decimal CalculateTotal()
        {
            decimal total = 0;
            foreach (var product in selectedProducts)
            {
                decimal price = product.ProductCost;
                decimal discount = product.ProductDiscountAmount;
                decimal finalPrice = price * (1 - discount / 100);
                total += finalPrice * product.ProductQuantityInStock;
            }
            return total;
        }

        private void UpdateOrderSummary()
        {
            decimal totalWithoutDiscount = 0;
            decimal totalWithDiscount = 0;
            decimal totalDiscount = 0;

            foreach (var product in selectedProducts)
            {
                decimal price = product.ProductCost;
                decimal discount = product.ProductDiscountAmount;
                decimal finalPrice = price * (1 - discount / 100);
                int quantity = product.ProductQuantityInStock;

                totalWithoutDiscount += price * quantity;
                totalWithDiscount += finalPrice * quantity;
                totalDiscount += (price - finalPrice) * quantity;
            }

            // Обновляем UI элементы
            

            if (TotalAmountText != null)
                TotalAmountText.Text = totalWithDiscount.ToString("F2");

            
        }


        private void ShoeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем текущее окно
            this.Close();
        }

        private void ComboPickUpPoint_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
