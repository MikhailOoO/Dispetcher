using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Windows.Forms;

namespace disOfTskGrph
{
    public partial class Form1 : Form
    {
        DataTable dt;
        private List<SortInfo> sortInfoList = new List<SortInfo>();
        int rowIndex = 0;
        public Form1()
        {
            InitializeComponent();
            timer1.Start();
            timer2.Start();
            try
            {
                this.Text = "Компьютер: " + Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName").GetValue("ComputerName").ToString() +
                    " / " + "ОС: " + Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion").GetValue("ProductName").ToString();
            }
            catch (Exception e)
            {
                this.Text = "Ошибка обращения в реестр";
                Console.Write(e.Message);
            }
        }


        private void tabControl1_Enter(object sender, EventArgs e)
        {

            dataGridView1.DataSource = getInfoFir();
        }

        private void tabPage2_Enter(object sender, EventArgs e)
        {
            dataGridView2.DataSource = getInfoSec();
        }

        private void tabPage3_Enter(object sender, EventArgs e)
        {
            dataGridView3.DataSource = getInfoThi();
            foreach (SortInfo sortInfo in sortInfoList)
            {
                DataGridViewColumn column = dataGridView3.Columns[sortInfo.ColumnName];
                if (column != null)
                {
                    if (sortInfo.SortOrder == SortOrder.Ascending)
                    {
                        dataGridView3.Sort(column, System.ComponentModel.ListSortDirection.Ascending);
                    }
                    else if (sortInfo.SortOrder == SortOrder.Descending)
                    {
                        dataGridView3.Sort(column, System.ComponentModel.ListSortDirection.Descending);
                    }
                }
            }
            rowIndex = dataGridView3.FirstDisplayedScrollingRowIndex;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dataGridView2.DataSource = getInfoFir();
        }


        private DataTable getInfoFir()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Компонент", typeof(string));
            dataTable.Columns.Add("Название", typeof(string));
            dataTable.Rows.Add("Процессор", Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0").GetValue("ProcessorNameString").ToString());
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            long totalMemory = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                totalMemory += Convert.ToInt64(obj["Capacity"]);
            }
            dataTable.Rows.Add("Объем оперативной памяти (MB)", (totalMemory / 1024 / 1024).ToString());
            dataGridView1.DataSource = dataTable;
            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
            {
                dataTable.Rows.Add("Диск " + obj["Model"] + " (GB)", (float.Parse(obj["Size"].ToString()) / 1024 / 1024 / 1024).ToString());
            }

            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                dataTable.Rows.Add("Видеокарта ", obj["Name"]);
            }
            return dataTable;
        }
        private DataTable getInfoSec()
        {

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Компонент", typeof(string));
            dataTable.Columns.Add("Параметр 1", typeof(string));
            dataTable.Columns.Add("Параметр 2", typeof(string));
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
            dataTable.Rows.Add("Загрузка процессора", counterCPU.NextValue().ToString() + "%", "");
            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                long totalMemory = Convert.ToInt64(obj["TotalVisibleMemorySize"]);
                long freeMemory = Convert.ToInt64(obj["FreePhysicalMemory"]);
                long usedMemory = totalMemory - freeMemory;
                dataTable.Rows.Add("Загрузка памяти (MB)", (usedMemory / 1024).ToString() + " (занято)", (freeMemory / 1024).ToString() + " (свободно)");
            }

            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3");
            foreach (ManagementObject obj in searcher.Get())
            {
                dataTable.Rows.Add("Диск " + obj["DeviceID"] + " (GB)", (Convert.ToUInt64(obj["Size"]) / (1024 * 1024 * 1024)).ToString() + " (занято)", (Convert.ToUInt64(obj["FreeSpace"]) / (1024 * 1024 * 1024)).ToString() + " (свободно)");
            }
            return dataTable;
        }
        private DataTable getInfoThi() {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Процесс", typeof(string));
            dataTable.Columns.Add("Нагрузка на процессор (%)", typeof(double));
            dataTable.Columns.Add("Память (MB)", typeof(double));
            var processes = Process.GetProcesses();
            double memoryUsage = 0;
            double totalCpuTime = 0;
            foreach (Process process in processes)
            {
                try
                {
                    totalCpuTime += process.TotalProcessorTime.TotalMilliseconds;
                }
                catch (Win32Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"Ошибка при получении информации о процессе {process.ProcessName}: {exc.Message}");
                }
            }
            double cpuUsage = 0;
            foreach (Process process in processes)
            {
                try
                {
                    cpuUsage = Math.Round((process.TotalProcessorTime.TotalMilliseconds / totalCpuTime) * 100, 2);
                    memoryUsage = Math.Round(Convert.ToDouble(process.PrivateMemorySize64)/1024/1024, 2);
                    dataTable.Rows.Add(process.ProcessName, cpuUsage, memoryUsage);
                }
                catch (Win32Exception ex)
                {
                    Console.WriteLine($"Ошибка при получении информации о процессе {process.ProcessName}: {ex.Message}");
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"Ошибка при получении информации о процессе {process.ProcessName}: {exc.Message}");
                }
            }
            return dataTable;
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
            rowIndex = dataGridView3.FirstDisplayedScrollingRowIndex;
            if (tabControl1.SelectedIndex == 2 && !backgroundWorker1.IsBusy) {
                backgroundWorker1.RunWorkerAsync();
            }
            
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 1)
            {
                dataGridView2.DataSource = getInfoSec();
            }
        }   

        private void DataGridView3_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DataGridViewColumn clickedColumn = dataGridView3.Columns[e.ColumnIndex];
            SortInfo sortInfo = new SortInfo
            {
                ColumnName = clickedColumn.Name,
                SortOrder = dataGridView3.SortOrder
            }; 
            sortInfoList.Add(sortInfo);
        }

        private class SortInfo
        {
            public string ColumnName { get; set; }
            public SortOrder SortOrder { get; set; }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            dt = getInfoThi();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dataGridView3.DataSource = dt;
            dataGridView3.FirstDisplayedScrollingRowIndex = rowIndex;
            foreach (SortInfo sortInfo in sortInfoList)
            {
                DataGridViewColumn column = dataGridView3.Columns[sortInfo.ColumnName];
                if (column != null)
                {
                    if (sortInfo.SortOrder == SortOrder.Ascending)
                    {
                        dataGridView3.Sort(column, System.ComponentModel.ListSortDirection.Ascending);
                    }
                    else if (sortInfo.SortOrder == SortOrder.Descending)
                    {
                        dataGridView3.Sort(column, System.ComponentModel.ListSortDirection.Descending);
                    }
                }
            }
            
        }
    }
}
