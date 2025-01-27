using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
namespace CoordFinder
{
    public partial class MainWindow : Window
    {
        // API ключ для доступа к сервису Яндекс Геокодер
        private const string YandexApiKey = "ВАШ_КЛЮЧ";
        // HTTP-клиент для выполнения запросов
        private readonly HttpClient _client = new HttpClient();
        private string _objectType;     // Хранит тип объекта (например, "street", "house")
        public MainWindow()
        {
            InitializeComponent();
        }

        // Обработчик нажатия кнопки поиска
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что поле ввода адреса не пустое
            if (string.IsNullOrWhiteSpace(AddressInput.Text))
            {
                MessageBox.Show("Введите адрес!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ToggleUIState(false); // Отключаем элементы интерфейса на время выполнения запроса

            try
            {
                await GetCoordinates(AddressInput.Text);
            }
            catch (Exception ex)
            {
                // Показываем сообщение об ошибке
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ToggleUIState(true); // Восстанавливаем элементы интерфейса
            }
        }

        // Метод для получения координат через API Яндекс Геокодер
        private async Task GetCoordinates(string address)
        {
            try
            {
                // Формируем запрос к API Яндекс Геокодер
                var response = await _client.GetStringAsync(
                    $"https://geocode-maps.yandex.ru/1.x/?format=json&apikey={YandexApiKey}&geocode={Uri.EscapeDataString(address)}");

                // Разбираем JSON-ответ от API
                var json = JObject.Parse(response);
                var pos = json["response"]?["GeoObjectCollection"]?["featureMember"]?[0]?
                    ["GeoObject"]?["Point"]?["pos"]?.ToString();
                _objectType = json["response"]?["GeoObjectCollection"]?["featureMember"]?[0]?
                    ["GeoObject"]?["metaDataProperty"]?["GeocoderMetaData"]?["kind"]?.ToString();
                if (pos != null)
                {
                    var parts = pos.Split(' ');
                    if (parts.Length == 2)
                    {
                        longResult.Text = parts[0];
                        latResult.Text = parts[1];
                    }
                }
            }
            catch
            {
                latResult.Text = "Не найдено";
                longResult.Text = " ";
                _objectType = null;
            }
        }

        // Управление состоянием элементов интерфейса
        private void ToggleUIState(bool isEnabled)
        {
            AddressInput.IsEnabled = isEnabled; // Поле ввода адреса
            SearchButton.IsEnabled = isEnabled; // Кнопка поиска
        }

        // Обработчик кнопки для копирования результата в буфер обмена
        private void CopyYandex_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, есть ли текст для копирования
            if (latResult.Text == "-" || latResult.Text == "Не найдено")
            {
                MessageBox.Show("Нет данных для копирования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Копируем текст в буфер обмена
            Clipboard.SetText($"{latResult.Text}, {longResult.Text}");
            new ToolTip
            {
                Content = "Скопировано!",
                StaysOpen = false,
                IsOpen = true
            };
        }

        // Определение приближения карты по типу объекта
        private int GetZoomLevel(string objectType)
        {
            if (string.IsNullOrEmpty(objectType))
                return 12;
            var zoomMap = new Dictionary<string, int>
            {
                { "country", 6 },           // Страна 
                { "province", 9 },          // Область 
                { "locality", 12 },         // Город 
                { "district", 14 },         // Район 
                { "metro", 16 },            // Метро 
                { "street", 17 },           // Улица 
                { "house", 19 },            // Дом 
                { "hydro", 14 },            // Водоемы 
                { "railway_station", 16 },  // Ж/д станция 
                { "station", 16 },          // Другие станции 
                { "route", 15 },            // Линии 
                { "vegetation", 16 },       // Парки
                { "airport", 14 },          // Аэропорт
                { "entrance", 19 },         // Вход
                { "other", 16 }             // Прочее 
            };

            return zoomMap.TryGetValue(objectType, out int zoom) ? zoom : 12; // Значение по умолчанию
        }
        
        // Открытие карты в браузере по координатам
        private void OpenUrl(Func<int, string> urlGenerator)
        {
            try
            {
                if (latResult.Text == "-" || latResult.Text == "Не найдено")
                {
                    throw (new Exception("Нет данных"));
                }
                Process.Start(urlGenerator(GetZoomLevel(_objectType)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}","Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OpenUrlYandexButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(zoom => $"https://yandex.ru/maps/?ll={longResult.Text}%2C{latResult.Text}&pt={longResult.Text}%2C{latResult.Text}&z={zoom}");
        }

        private void OpenUrlGisButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(zoom => $"https://2gis.ru/?m={longResult.Text}%2C{latResult.Text}%2F{zoom}");
        }
    }
}
