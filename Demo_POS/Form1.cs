using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using FileHelpers;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace French_Press_POS
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            updateOrders();
        }

        //initialize a public class-level data table to store the data for all orders
        public DataTable ordersTable { get; set; }

        //initialize a public class-level dictionary to store the data for each order’s items
        public Dictionary<string, decimal> itemDictionary { get; set; }

        //initialize a public class-level data view to access data from the ordersTable for a specific order
        public DataView selectedOrder { get; set; }

        //initialize a public class-level string to track the payment authorization ID for each authorization in a batch.
        public string authId { get; set; } = "0";

        //initialize a public class-level string to track the payment transaction ID for each transaction in a batch.
        public string transactionId { get; set; } = "0";

        //initialize a public class-level string to track the batch ID for each batch of transactions.       
        public string batchId { get; set; } = "0";


        public string dbPath()
        {
            //pull the directory path of the server to the orders.csv file. even if the app is
            //moved from server to server, or if using a development server and a production server
            string workingDirectory = Environment.CurrentDirectory;
            string filePath = Directory.GetParent(workingDirectory).Parent.FullName + "\\App_Data";
            string orders = filePath + "\\orders.csv";

            return orders;
        }

        public DataTable parseCsv()
        {
            //create a new DataTable object to store the parsed CSV data
            ordersTable = new DataTable();
            //use FileHelpers lib to parse the orders CSV
            ordersTable = CommonEngine.CsvToDataTable(dbPath(), ',');

            return ordersTable;
        }

        public void updateOrders()
        {
            //define the columns that we want to display on this screen
            string[] selectedColumns = new[] { "Order", "Employee", "Table", "Total", "Status" };
            //create a data view to display only the selected columns
            DataTable filteredColsView = new DataView(parseCsv()).ToTable(false, selectedColumns);

            //create a data view to display only open orders
            DataView openOrders = new DataView(filteredColsView);
            openOrders.RowFilter = "Status = 'Open' OR Status='Amount Adjusted'";
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            //use this view as the data source for dataGridView1
            dataGridView1.DataSource = openOrders;

            //create a data view to display only closed orders
            DataView closedOrders = new DataView(filteredColsView);
            closedOrders.RowFilter = "Status='Captured' OR Status='Batched' OR Status='Voided' OR Status='Refunded'";
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            //use this view as the data source for dataGridView2
            dataGridView2.DataSource = closedOrders;
        }

        public string createNewOrder()
        {
            //call the dbPath method to access the csv file
            string ordersFile = dbPath();
            //get the order number from the last record in orders.csv
            string lastLine = File.ReadLines(ordersFile).Last();
            string[] cellValues = lastLine.Split(',');
            string lastOrderValue;
            //check if there are existing records. if not, the first cell value will be equal to the first header value
            //which is "Order"
            if (cellValues.First() != "Order") { lastOrderValue = cellValues.First(); } else { lastOrderValue = "0"; }
            //increment the most recent order number by 1
            string newOrderValue = (Int32.Parse(lastOrderValue) + 1).ToString();

            //add a new record to the database with the following default values:
            //order # = Auto-incremented number
            //status = Open
            using (StreamWriter w = new StreamWriter(ordersFile, true))
            {
                w.WriteLine(newOrderValue + ", , ,Open,0.00, ,0.00,0.00,0.00,0,0, , ,0.00");
            }

            //update the gridview with the new order
            updateOrders();

            return newOrderValue;
        }

        public void newOrderButtonClick(object sender, EventArgs e)
        {
            string newOrderNumber = createNewOrder().ToString();
            //use a linq query to select the data table row where the order number is equal to
            //the result of calling createNewOrder()
            var query =
                from order in parseCsv().AsEnumerable()
                where order.Field<string>("Order") == newOrderNumber
                select order;

            //initialize itemDictionary as a new empty dictionary which will be filled in OrderDetail
            //and will include all items that are added to an order
            itemDictionary = new Dictionary<string, decimal>();

            //create a new dataview to execute the query and display the result
            DataView newOrder = query.AsDataView();
            selectedOrder = newOrder;
            //pass the dataview object to the displayRecordDetail
            //method of the OrderDetail Form2 instance. This will display details of the
            //new record on the Order Detail screen
            FormProvider.OrderDetail.displayRecordDetail();
            //show the OrderDetail instance of the Form2 class
            FormProvider.OrderDetail.Show();
            //hide this instance of the Form1 class
            this.Hide();
        }

        public void displayRecordSummary(object selectedOrderNumber)
        {
            //use a linq query to select the data table row where the order number is equal to
            //the order number of the selected grid view row
            var query =
                from order in parseCsv().AsEnumerable()
                where order.Field<string>("Order") == selectedOrderNumber.ToString()
                select order;

            //create a new dataview to execute the query and display the result
            selectedOrder = query.AsDataView();

            //convert the JSON object from the CSV to a Dictionary
            var json = selectedOrder[0]["Items"].ToString().Replace(';', ',');
            itemDictionary = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(json);

            //pass the dataview object to the displayRecordDetail method
            //of the OrderDetail Form2 instance. This will display details of the
            //selected record on the Order Detail screen
            FormProvider.OrderDetail.displayRecordDetail();
            //show the OrderDetail instance of the Form2 class
            FormProvider.OrderDetail.Show();
            //hide this instance of the Form1 class
            this.Hide();
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //get the order number of the selected order
            object selectedOrderNumber = dataGridView2.SelectedRows[0].Cells[0].Value;
            displayRecordSummary(selectedOrderNumber);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //get the order number of the selected order
            object selectedOrderNumber = dataGridView1.SelectedRows[0].Cells[0].Value;
            displayRecordSummary(selectedOrderNumber);
        }

        private void batchCloseButton_Click(object sender, EventArgs e)
        {
            //if the result of calling the Batch method of the CommonPayment class is 0 (meaning Approved), execute the code below
            if (CommonPayment.batch() == "000000")
            {
                //for more information about tracking IDs, see the "Transaction tracking" section of the
                //Payments Hub Developers PAX SI SDK Integration Guide.

                //reset the transactionId and authId to 0
                transactionId = "0";
                authId = "0";
                //increment batchId by 1
                batchId = (Int32.Parse(batchId) + 1).ToString();
            }
        }

        public void updateBatchStatus (string oldStatus, string newStatus)
        {
            var query =
                from order in parseCsv().AsEnumerable()
                where order.Field<string>("Status") == oldStatus
                select Int32.Parse(order.Field<string>("Order"));

            //save all rows in the csv to an array of strings
            string[] allRecords = File.ReadAllLines(dbPath());

            foreach (int order in query)
            {
                string[] cellValues = allRecords[order].Split(',');
                cellValues[3] = newStatus;
                allRecords[order] = String.Join(",", cellValues);
            }

            //rewrite the file with the new data
            File.WriteAllLines(dbPath(), allRecords);
            updateOrders();
        }
    }
}
