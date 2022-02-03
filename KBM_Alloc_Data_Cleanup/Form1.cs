using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Globalization;

namespace KBM_Alloc_Data_Cleanup
{
    public partial class Form1 : Form
    {

        static string exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

        static string sql_script_file = exePath + "\\KBM_Alloc_Data_Cleanup.txt";
        static string ini_file = exePath + "\\KBM_Alloc_Data_Cleanup.ini";

        static string conn_string = "";
        static string well_name = "";
        static string start_date = "";
        static string end_date = "";

        public Form1()
        {
            InitializeComponent();

            // Form resize block
            FormBorderStyle = FormBorderStyle.FixedSingle;

            Read_ini_file();
            
            if (conn_string.Length>10)
             textBox1.Text = conn_string;
            if (well_name.Length > 1)
                textBox2.Text = well_name;
            if (start_date.Length > 9)
                dateTimePicker1.Value = DateTime.ParseExact(start_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (end_date.Length > 9)
                dateTimePicker2.Value = DateTime.ParseExact(end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        }

        private void button1_Click(object sender, EventArgs e)
        {


            StreamWriter writer = new StreamWriter(sql_script_file);
           //writer.WriteLine("File created using StreamWriter class.");

            try
            {
                //String str = "server=MUNESH-PC;database=windowapp;UID=sa;password=123";
                //String str = @"Data Source=localhost,1433;Initial Catalog=AVOCET_EXPERT_PETROLEUM_UA;User ID=sa;Password=713Avm";
                //@"Data Source=192.168.166.30,1433;Initial Catalog=AVM_WINTERSHALL;User ID=sa;Password=713Avm";

                conn_string = textBox1.Text.Trim();
                well_name   = textBox2.Text.Trim();
                start_date  = dateTimePicker1.Value.Date.ToString("yyyy-MM-dd");
                end_date    = dateTimePicker2.Value.Date.ToString("yyyy-MM-dd");

                writer.WriteLine("INPUT PARAMETERS");
                writer.WriteLine("-----------------------------------");
                writer.WriteLine("Connection string: " + conn_string);
                writer.WriteLine("Well name:         " + well_name);
                writer.WriteLine("Start date:        " + start_date);
                writer.WriteLine("End date:          " + end_date);
                writer.WriteLine("-----------------------------------\n");

                SqlConnection con = new SqlConnection(conn_string);
                String        query = "SELECT ITEM_ID, ITEM_NAME FROM VI_COMPLETION_en_US WHERE ITEM_NAME IN ('" + well_name + "')";
                SqlCommand    cmd = new SqlCommand(query, con);
                con.Open();

                String well_item_id = "";
                String well_item_name = "";

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {

                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    int recs_count = dt.Rows.Count;

                    if (recs_count == 1) 
                    {

                        //============= WELL =================
                        DataRow[] rows = dt.Select();
                        for (int i = 0; i < rows.Length; i++)
                        {
                            //Console.WriteLine(rows[i]["CompanyName"]);
                            well_item_id   = rows[i][0].ToString();
                            well_item_name = rows[i][1].ToString();
                             
                            writer.WriteLine("WELL COMPLETION NAME:    " + well_item_name);
                            writer.WriteLine("WELL COMPLETION ITEM_ID: " + well_item_id);
                        }

                                //============= ZONES =================
                                String query2 = "select ITEM_ID, ITEM_NAME from VI_ZONE_en_US where ITEM_ID in (select TO_ITEM_ID from ITEM_LINK where LINK_TYPE = 'WELL_ZONE' and FROM_ITEM_ID in ('" + well_item_id + "')) order by ITEM_NAME";
                                SqlCommand cmd2 = new SqlCommand(query2, con);
                                SqlDataReader reader2 = cmd2.ExecuteReader();

                                String zone_item_id = "";
                                String zone_item_name = "";


                                if (reader2.HasRows)
                                {
                                    writer.WriteLine("-----------------------------------\n");
                                    writer.WriteLine("ZONE COMPLETION ITEM_ID                ZONE COMPLETION NAME");

                                   //string[] array_for_sql_select;
                                   //string[] array_for_sql_delete;
                                   //List<string> for_sql_select = new List<string>();
                                   //List<string> for_sql_delete = new List<string>();


                                    // ==== SQL QUERY PARTS ====
                                    string sql_part_dates   = " where START_DATETIME >= '" + start_date + "' and START_DATETIME <= '" + end_date + "'";
                                    string sql_part_item_id = "   and ITEM_ID in (";

                                    while (reader2.Read())
                                    {
                                        zone_item_id = reader2[0].ToString();
                                        zone_item_name = reader2[1].ToString();
                                        writer.WriteLine(zone_item_id + "       " + zone_item_name);

                                        sql_part_item_id += "'" + zone_item_id + "',";
                                    }
                                    reader2.Close();

                                    //sql_part_item_id = sql_part_item_id.Remove(sql_part_item_id.Trim().Length - 1);
                                    sql_part_item_id = sql_part_item_id.Remove(sql_part_item_id.Length - 1);
                                    sql_part_item_id += ")";

                                    // ==== SELECTS ====
                                    writer.WriteLine("-----------------------------------\n");
                                    
                                    writer.WriteLine("select *");
                                    writer.WriteLine("  from IE_ITEM_ACT_DAY");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'ITEM_ACT_DAY'");
                                    writer.WriteLine("");

                                    writer.WriteLine("select *");
                                    writer.WriteLine("  from IE_ITEM_ACT_MTH");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'ITEM_ACT_MTH'");
                                    writer.WriteLine("");

                                    writer.WriteLine("select *");
                                    writer.WriteLine("  from IE_ACT_DAY");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'ACT_DAY'");
                                    writer.WriteLine("");

                                    writer.WriteLine("select *");
                                    writer.WriteLine("  from IE_ACT_MTH");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'ACT_MTH'");
                                    writer.WriteLine("");

                                    writer.WriteLine("select *");
                                    writer.WriteLine("  from ITEM_EVENT");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'TOTALS_DAY'");
                                    writer.WriteLine("");

                                    writer.WriteLine("select *");
                                    writer.WriteLine("  from ITEM_EVENT");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'TOTALS_MTH'");
                                    writer.WriteLine("");

                                    // ==== DELETES ====
                                    writer.WriteLine("-----------------------------------\n");

                                    writer.WriteLine("delete");
                                    writer.WriteLine("  from IE_ITEM_ACT_DAY");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'ITEM_ACT_DAY'");
                                    writer.WriteLine("");

                                    writer.WriteLine("delete");
                                    writer.WriteLine("  from IE_ITEM_ACT_MTH");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'ITEM_ACT_MTH'");
                                    writer.WriteLine("");

                                    writer.WriteLine("delete");
                                    writer.WriteLine("  from IE_ACT_DAY");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'ACT_DAY'");
                                    writer.WriteLine("");

                                    writer.WriteLine("delete");
                                    writer.WriteLine("  from IE_ACT_MTH");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'ACT_MTH'");
                                    writer.WriteLine("");

                                    writer.WriteLine("delete");
                                    writer.WriteLine("  from ITEM_EVENT");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'TOTALS_DAY'");
                                    writer.WriteLine("");

                                    writer.WriteLine("delete");
                                    writer.WriteLine("  from ITEM_EVENT");
                                    writer.WriteLine(sql_part_dates);
                                    writer.WriteLine(sql_part_item_id);
                                    writer.WriteLine("   and EVENT_TYPE = 'TOTALS_MTH'");
                                    writer.WriteLine("");

                                    //for_sql_select.Add("select *");
                                    //for_sql_select.Add("  from IE_ITEM_ACT_DAY");
                                    //for_sql_select.Add(sql_part_dates);
                                    //for_sql_select.Add(sql_part_item_id);
                                    //for_sql_select.Add("   and EVENT_TYPE = 'ITEM_ACT_DAY'");
                                    //for_sql_select.Add("\n");

                                    //array_for_sql_select = for_sql_select.ToArray();
                                    //array_for_sql_delete = for_sql_delete.ToArray();

                                    //writer.Close();
                                    //System.IO.File.AppendAllLines(sql_script_file, array_for_sql_select);

                                }

                        /* EXAMPLE how to put array into file directly
                        string[] lines = { "First line", "Second line", "Third line" };
                        System.IO.File.WriteAllLines(@"C:\Users\Public\TestFolder\WriteLines.txt", lines);
                        lines = { "Fourth line", "Fith line" };
                        System.IO.File.AppendAllLines(@"C:\Users\Public\TestFolder\WriteLines.txt", lines);
                        */
                    }
                    else
                     MessageBox.Show("Найдено более одной скважины!");

                }
                else
                 MessageBox.Show("Не найдено ни одной скважины!");

                con.Close();
            }

            catch (Exception es)
            {
                MessageBox.Show(es.Message);
            }

            writer.Close();

            MessageBox.Show("Скрипт готов!");

        }

        static void Read_ini_file()
        {
            
            StreamReader f = new StreamReader(ini_file);

            try
            {

                while (!f.EndOfStream)
                {
                    string s = f.ReadLine();
                    // что-нибудь делаем с прочитанной строкой s

                    if (s.ToLower().Contains("connection_string:"))
                    {
                        conn_string = s.Replace("connection_string:", "").Trim();
                    }
                    if (s.ToLower().Contains("well:"))
                    {
                        well_name = s.Replace("well:", "").Trim();
                    }
                    if (s.ToLower().Contains("start_date:"))
                    {
                        start_date = s.Replace("start_date:", "").Trim();
                    }
                    if (s.ToLower().Contains("end_date:"))
                    {
                        end_date = s.Replace("end_date:", "").Trim();
                    }
                }
                f.Close();

            }

            catch (Exception es)
            {
                MessageBox.Show(es.Message);
            }

        }
    }
}
