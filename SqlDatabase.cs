using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;


namespace FoodChecker
{
    public class SqlDatabase
    {
        private string Table { get; }


        public SqlDatabase(string credentials, string table)
        {
            Conn = new MySqlConnection(credentials);
            this.Table = table;
            Conn.Open();
        }

        ~SqlDatabase()
        {
            Close();
        }

        public MySqlConnection Conn { get; }

        public void Close()
        {
            Conn.Close();
        }

        public int InsertNewFood(string name, int type)
        {
            MySqlCommand cmd = new MySqlCommand($"INSERT INTO {Table} (id_type, food_name) VALUES ({type}, '{name}');", Conn);
            return cmd.ExecuteNonQuery();
        }

        public int AddFoodToStock(int foodId, int userId, DateTime expirationDate)
        {
            MySqlCommand cmd = new MySqlCommand($"INSERT INTO {Table} (user_id, food_id, exp_date) VALUE ({userId}, {foodId}, '{expirationDate.ToString("yyyy-MM-dd")}');", Conn);
            return cmd.ExecuteNonQuery();

        }

        public int DeleteFoodFromStack(string name, string exp_date)
        {
            Regex rx = new Regex(@"\d+");
            MatchCollection mx = rx.Matches(exp_date);
            MySqlCommand cmd = new MySqlCommand($"SELECT  stock.stock_id FROM stock,food  WHERE stock.food_id=food.food_id AND food.food_name='{name}' AND stock.exp_date='{mx[2].Value}-{mx[1].Value}-{mx[0].Value}'  LIMIT 1;", Conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Read();
            cmd = new MySqlCommand($"DELETE FROM stock WHERE stock.stock_id={rdr.GetString(0)};", Conn);
            rdr.Close();
            return cmd.ExecuteNonQuery();
        }

        public List<string> Read()
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT * FROM {Table};", Conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            List<string> list = new List<string>();
            while (rdr.Read())
            {
                list.Add("");
                for (int i = 0; i < rdr.VisibleFieldCount; i++)
                {
                    list[list.Count - 1] += $"{rdr.GetString(i)};";
                }
            }
            rdr.Close();
            return list;
        }

        public int GetUserId(string login)
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT user_id FROM users WHERE login = '{login}'", Conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Read();
            int a = int.Parse(rdr.GetString(0));
            rdr.Close();
            return a;
        }

        public bool Auth(string hash, string login)
        {
            MySqlCommand cmd = new MySqlCommand($"SELECT user_id FROM users WHERE login = '{login}';", Conn);
            MySqlDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                rdr.Close();
                cmd = new MySqlCommand($"SELECT user_id FROM users WHERE login = '{login}' AND hash = '{hash}';", Conn);
                rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    rdr.Close();
                    return true;
                }
                else
                {
                    rdr.Close();
                    return false;
                }
            }
            else
            {
                rdr.Close();
                cmd = new MySqlCommand($"INSERT INTO users (login, hash) VALUES ('{login}', '{hash}');", Conn);
                cmd.ExecuteNonQuery();
                return true;
            }
        }
    }
}
