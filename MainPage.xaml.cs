using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Text.RegularExpressions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;


namespace FoodChecker
{
    public struct foodItem
    {
        public foodItem(string name, int type)
        {
            this.name = name;
            this.type = type;
        }

        public string name;
        public int type;
    }

    public struct stockListItem
    {
        public stockListItem(string name, string exp_date)
        {
            this.name = name;
            this.exp_date = exp_date;
            state = "Accept";
        }

        public stockListItem(string name, string exp_date, string state)
        {
            this.name = name;
            this.exp_date = exp_date;
            this.state = state;
        }

        public string name;
        public string exp_date;
        public string state;
    }

    public sealed partial class MainPage : Page
    {
        
        int CURRUSERID { get; set; }
        string CURRENTUSERLOGIN { get; set; }


        string MSQL_CREDENTIALS
        {
            get
            {
                return "server=achillesv.pl;user id=foodChecker;password=foodChecker;database=foodChecker";
            }
        }

        private List<foodItem> foodList;
        private ObservableCollection<stockListItem> stockList = new ObservableCollection<stockListItem>();

        string[] foodTypes = { "Nabiał", "Mięso", "Pieczywo", "Mrożonki", "Konserwy", "Inne" };

        readonly SqlDatabase dbFood, dbStock, dbPass;

        public int FoodId(string name)
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT food_id FROM food WHERE food_name = '{name}'", dbStock.Conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Read();
            int temp = rdr.GetInt32(0);
            rdr.Close();
            return temp;
        }


        public MainPage()
        {
            CURRENTUSERLOGIN = "";
            InitializeComponent();
            if(CURRENTUSERLOGIN == "")
            {
                UsrId.Text = "Logged out";
                AddFoodToDatabase.IsEnabled = false;
                DeleteStock.IsEnabled = false;
            }
            dbFood = new SqlDatabase(MSQL_CREDENTIALS, "food");
            dbStock = new SqlDatabase(MSQL_CREDENTIALS, "stock");
            dbPass = new SqlDatabase(MSQL_CREDENTIALS, "users");
            Refresh();
            Regex rx = new Regex(@"[\d.]+");
            CurrentDateTextblock.Text = rx.Match(DateTime.Today.ToString()).Value;
        }

        public string HashPassword(string pass)
        {
            IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(pass, BinaryStringEncoding.Utf8);
            HashAlgorithmProvider Alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            buffer = Alg.HashData(buffer);
            return CryptographicBuffer.EncodeToBase64String(buffer);
        }

        private void Refresh()
        {
            stockList.Clear();
            if (CURRENTUSERLOGIN != "")
            {
                ReadStockList();
                ReadExpired();
                ReadNearlyExpired();
            }
        }

        private void AddNewFood_Click(object sender, RoutedEventArgs e)
        {
            List<string> temp = dbFood.Read();
            foodList = new List<foodItem>();

            Regex rx = new Regex(@"[^\d;]+.+");
            Regex rx2 = new Regex(@"\d+");

            for(int i = 0; i< temp.Count; i++)
            {
                foodList.Add(new foodItem(rx.Match(temp[i]).Value.Trim(';'), int.Parse(rx2.Matches(temp[i])[1].Value)));
            }
        }

        private void AddFoodSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if(args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if(Operations.FilterList(foodList, AddFoodSearch.Text).Count > 0)
                {
                    AddFoodSearch.ItemsSource = Operations.FilterList(foodList, AddFoodSearch.Text);
                }
                else 
                {
                    AddFoodSearch.ItemsSource = null;
                }
            }
        }

        private void AddFoodSearch_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            AddFoodSearch.Text = (string)args.SelectedItem;
        }

        private void AddFocusSearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if(args.ChosenSuggestion != null)
            {
                AddFoodSearch.Text = (string)args.ChosenSuggestion;
                for(int i = 0; i < foodList.Count; i++)
                {
                    if(foodList[i].name == AddFoodSearch.Text)
                    {
                        CategoryComboBox.SelectedIndex = foodList[i].type;
                    }
                }
            }
            else
            {
                List<string> temp = new List<string>();
                foreach(foodItem f in foodList)
                {
                    temp.Add(f.name);
                }
                AddFoodSearch.ItemsSource = temp;
            }
        }

        /// <summary>
        /// Pobieranie elementow na liscie urzytkownika
        /// </summary>
        private void ReadStockList()
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT food.food_name,stock.exp_date FROM food,stock WHERE (stock.food_id=food.food_id) AND user_id = {CURRUSERID};", dbStock.Conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            while(rdr.Read())
            {
                Regex rx = new Regex(@"([\d.])+");
                stockList.Add(new stockListItem(rdr.GetString(0), rx.Match(rdr.GetString(1)).Value));
            }
            rdr.Close();
        }

        /// <summary>
        /// Pobieranie elementow po dacie warznosci
        /// </summary>
        private void ReadExpired()
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT food.food_name,stock.exp_date FROM food,stock WHERE (stock.food_id=food.food_id AND stock.exp_date<CURDATE()) AND user_id = {CURRUSERID};", dbStock.Conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            List<stockListItem> expired = new List<stockListItem>();
            while (rdr.Read())
            {
                Regex rx = new Regex(@"([\d.])+");
                expired.Add(new stockListItem(rdr.GetString(0), rx.Match(rdr.GetString(1)).Value));
            }
            rdr.Close();

            for(int i = 0; i < stockList.Count; i++)
            {
                foreach(stockListItem x in expired)
                {
                    if(stockList[i].name == x.name && stockList[i].exp_date == x.exp_date)
                    {
                        stockList[i] = new stockListItem(stockList[i].name, stockList[i].exp_date, "Delete");
                    }
                }
            }
        }

        /// <summary>
        /// Odczyt elementów ze zblizajaca sie data waznosci
        /// </summary>
        private void ReadNearlyExpired()
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT food.food_name,stock.exp_date FROM food,stock WHERE (stock.food_id=food.food_id AND (stock.exp_date BETWEEN CURDATE() AND DATE_SUB(CURDATE(),INTERVAL -3 DAY))) AND user_id = {CURRUSERID};", dbStock.Conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            List<stockListItem> expired = new List<stockListItem>();
            while (rdr.Read())
            {
                Regex rx = new Regex(@"([\d.])+");
                expired.Add(new stockListItem(rdr.GetString(0), rx.Match(rdr.GetString(1)).Value));
            }
            rdr.Close();

            for (int i = 0; i < stockList.Count; i++)
            {
                foreach (stockListItem x in expired)
                {
                    if (stockList[i].name == x.name && stockList[i].exp_date == x.exp_date)
                    {
                        stockList[i] = new stockListItem(stockList[i].name, stockList[i].exp_date, "Clock");
                    }
                }
            }
        }

        private void DeleteStock_Button(object sender, RoutedEventArgs e)
        {
            stockListItem temp = stockList[StockListView.SelectedIndex];
            dbStock.DeleteFoodFromStack(temp.name, temp.exp_date);
            stockList.Remove(temp);
            DeleteStock.IsEnabled = false;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void StockSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StockListView.SelectedIndex >= 0)
            {
               DeleteStock.IsEnabled = true;
            }
        }

        /// <summary>
        /// Funkcja do logowania/przelogowywania
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeUserId(object sender, RoutedEventArgs e)
        {
            DeleteStock.IsEnabled = false;
            if (UserIdBox.Text != "")
            { 
                bool Authenticated = false;

                if (PasswordBox.Password != "")
                {
                    string hash = HashPassword(PasswordBox.Password);
                    Authenticated = dbPass.Auth(hash, UserIdBox.Text);
                }

                if (Authenticated)
                {
                    CURRENTUSERLOGIN = UserIdBox.Text;
                    UsrId.Text = CURRENTUSERLOGIN;
                    PasswordBox.Password = UserIdBox.Text = "";
                    CURRUSERID = dbPass.GetUserId(CURRENTUSERLOGIN);
                    UsrIdFlyout.Hide();
                    AddFoodToDatabase.IsEnabled = true;
                    Refresh();
                }
                else
                {
                    PasswordBox.Password = "";
                }
            }
            else
            {
                CURRENTUSERLOGIN = "";
                UsrId.Text = "Logged out";
                AddFoodToDatabase.IsEnabled = false;
                DeleteStock.IsEnabled = false;
                Refresh();
            }
        }

        private void AddToListButton_Click(object sender, RoutedEventArgs e)
        {
            if(AddFoodSearch.Text.Length >= 50)
            {
                return;
            }

            AddFlyout.Hide();
            if (foodList.Exists(s => s.name.Equals(AddFoodSearch.Text)))
            {
                int foodId = FoodId(AddFoodSearch.Text);
                dbStock.AddFoodToStock(foodId, CURRUSERID, ExpirationDatePicker.Date.Value.Date);
                stockList.Add(new stockListItem(AddFoodSearch.Text, ExpirationDatePicker.Date.Value.Date.ToString("dd.MM.yyyy")));
            }
            else
            {
                dbFood.InsertNewFood(AddFoodSearch.Text, CategoryComboBox.SelectedIndex);
                dbStock.AddFoodToStock(FoodId(AddFoodSearch.Text), CURRUSERID, ExpirationDatePicker.Date.Value.Date);
                stockList.Add(new stockListItem(AddFoodSearch.Text, ExpirationDatePicker.Date.Value.Date.ToString("dd.MM.yyyy")));
            }
            ReadExpired();
            ReadNearlyExpired();
        }
    }
}
