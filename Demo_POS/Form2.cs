using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using Newtonsoft.Json;
using POSLink2.Transaction;

namespace French_Press_POS
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        public string labelText
        {
            get { return label1.Text; }
            set { label1.Text = value; }
        }
        public string response { get; set; }
        public string authToken { get; set; }
        public string transactionToken {  get; set; }

        public void displayRecordDetail()
        {
            //assign values from the selectedOrder dataview to the corresponding TextBox values
            orderNumValue.Text = (string)FormProvider.OrderSummaries.selectedOrder[0]["Order"];
            tableNumValue.Text = (string)FormProvider.OrderSummaries.selectedOrder[0]["Table"];
            serverNumValue.Text = (string)FormProvider.OrderSummaries.selectedOrder[0]["Employee"];
            statusValue.Text = (string)FormProvider.OrderSummaries.selectedOrder[0]["Status"];
            //in a real-world scenario that uses a full-featured database, the following currency values can be
            //easily stored as decimals, eliminating the need to convert to and from strings.
            //since this tutorial uses a flat-file csv database, all values are being stored as strings.
            subtotalValue.Text = string.Format("{0:C}", FormProvider.OrderSummaries.selectedOrder[0]["Subtotal"]);
            taxValue.Text = string.Format("{0:C}", FormProvider.OrderSummaries.selectedOrder[0]["Tax"]);
            tipValue.Text = string.Format("{0:C}", FormProvider.OrderSummaries.selectedOrder[0]["Tip"]);
            totalValue.Text = string.Format("{0:C}", FormProvider.OrderSummaries.selectedOrder[0]["Total"]);
            authAmtValue.Text = string.Format("{0:C}", FormProvider.OrderSummaries.selectedOrder[0]["AuthAmount"]);
            //bind the itemDictionary as the data source for listBox1.
            //the itemDictionary includes all items that have been added to an order.
            listBox1.DataSource = new BindingSource(FormProvider.OrderSummaries.itemDictionary, null);
            listBox1.DisplayMember = "Key" + "Value";
        }

        static void editRecord(string newText, string fileName, int recordToEdit)
        {
            //save all rows in the csv to an array of strings
            string[] allRecords = File.ReadAllLines(fileName);
            //save the new text to the string in the array with index equal to recordToEdit
            allRecords[recordToEdit - 1] = newText;
            //rewrite the file with the new data
            File.WriteAllLines(fileName, allRecords);
        }

        public void updateDb()
        {
            //serialize the Dictionary that stores the order items to a JSON object
            var dictionaryToJson = JsonConvert.SerializeObject(FormProvider.OrderSummaries.itemDictionary).Replace(',', ';');

            //call the editRecord method to apply the changes.
            //pass as the filename parameter the result of calling the dbPath method of the OrderSummaries instance
            //of the Form1 class to access the csv file.
            //use the order number value + 1 for the record index, adding 1 to account for the header row
            editRecord(orderNumValue.Text + "," +
                serverNumValue.Text + "," +
                tableNumValue.Text + "," +
                statusValue.Text + "," +
                totalValue.Text + "," + dictionaryToJson + "," +
                subtotalValue.Text + "," +
                taxValue.Text + "," +
                tipValue.Text + "," +
                authIdValue.Text + "," +
                transactionIdValue.Text + "," +
                FormProvider.OrderSummaries.batchId + "," +
                authToken + "," +
                transactionToken + "," +
                authAmtValue.Text, FormProvider.OrderSummaries.dbPath(), Int32.Parse(orderNumValue.Text) + 1);
        }

        public void updateOrder()
        {
            listBox1.DataSource = new BindingSource(FormProvider.OrderSummaries.itemDictionary, null);
            listBox1.DisplayMember = "Key" + "Value";
            //set a variable named subTotal equal to the result of adding all decimal values in the itemDictionary.
            decimal subTotal = FormProvider.OrderSummaries.itemDictionary.Sum(x => x.Value);
            subtotalValue.Text = string.Format("{0:C}", subTotal);
            decimal tax = .06m * subTotal;
            decimal tip = decimal.Parse(tipValue.Text);
            taxValue.Text = string.Format("{0:C}", tax);
            totalValue.Text = string.Format("{0:C}", (tax + subTotal + tip));
            transactionIdValue.Text = FormProvider.OrderSummaries.transactionId;
        }

        public void addItem(string item, decimal amount)
        {
            if (FormProvider.OrderSummaries.itemDictionary.ContainsKey(item) == true)
            {
                FormProvider.OrderSummaries.itemDictionary[item] = FormProvider.OrderSummaries.itemDictionary[item] + amount;
            }
            else { FormProvider.OrderSummaries.itemDictionary.Add(item, amount); }
            updateOrder();
            authIdValue.Text = FormProvider.OrderSummaries.authId;
        }

        public void removeItem(string item, decimal amount)
        {
            if (FormProvider.OrderSummaries.itemDictionary.ContainsKey(item) != true)
            {
                return;
            } else if (FormProvider.OrderSummaries.itemDictionary[item] != amount)
            {
                FormProvider.OrderSummaries.itemDictionary[item] = FormProvider.OrderSummaries.itemDictionary[item] - amount;
            }
            else { FormProvider.OrderSummaries.itemDictionary.Remove(item); }
            updateOrder();
            authIdValue.Text = FormProvider.OrderSummaries.authId;
        }

        public void updateOrderStatus(string newStatus)
        {
            statusValue.Text = newStatus;
            updateDb();
        }

        private void tipButton_Click(object sender, EventArgs e)
        {
            updateOrder();
            //update the Auth Amount on the UI.
            authAmtValue.Text = totalValue.Text;
            updateDb();
            FormProvider.OrderSummaries.displayRecordSummary(orderNumValue.Text);
        }

        //clicking the Sale button will perform an auth and immediately capture it.
        //see the “SDK payment method definitions and examples” section of the
        //Payments Hub Developers PAX SI SDK Integration Guide for more details about these functions and their use cases.

        private void saleButton_Click(object sender, EventArgs e)
        {
            //clicking this button will perform an auth and immediately capture it.
            //see the “SDK payment method definitions and examples” section of the
            //Payments Hub Developers PAX SI SDK Integration Guide for more details about these functions and their use cases.

            //after a new batch is created, transactionId and authId must have a positive int value, so they must be
            //incremented before the if condition is met and the Auth transaction method is called.
            FormProvider.OrderSummaries.authId = (Int32.Parse(FormProvider.OrderSummaries.transactionId) + 1).ToString();
            FormProvider.OrderSummaries.transactionId = (Int32.Parse(FormProvider.OrderSummaries.transactionId) + 1).ToString();
            //display the authId on the UI
            authIdValue.Text = FormProvider.OrderSummaries.authId;
            updateOrder();
            //call the transaction method of the CommonPayment class passing in 1 for the transaction type, which is used to Authorize a payment.
            string[] authResult = CommonPayment.transaction(1, "Transaction Authorized", "0", totalValue.Text);
            //if the result of calling the Auth transaction method is 0 (meaning the transaction was Approved), execute the code below
            if (authResult[0] == "000000")
            {
                //save the transaction token returned by the processor from the Auth method as an auth token.
                authToken = authResult[1];
                authAmtValue.Text = totalValue.Text;
                //call the transaction method of the CommonPayment class passing in 5 for the transaction type, which is used to Capture an Authorization.
                string[] transactionResult = CommonPayment.transaction(5, "Transaction Successful", authIdValue.Text, totalValue.Text, "Captured");
                //if the result of calling the Capture transaction method is 0, execute the code below
                if (transactionResult[0] == "000000")
                {
                    //save the transaction token returned by the processor from the Capture method as a transaction token.
                    transactionToken = transactionResult[1];
                    //increment the transaction ID by 1 and update the database, but note that the authId is not incremented here.
                    //for more information about tracking transactions and auths, see the "Transaction tracking" section of the Payments Hub Developers PAX SI SDK Integration Guide.
                    FormProvider.OrderSummaries.transactionId = (Int32.Parse(FormProvider.OrderSummaries.transactionId) + 1).ToString();
                }
                //if the result of calling the Capture transaction method is not 0, do nothing else.
            }
            //if the result of calling the Auth transaction method is not 0,
            //decrement the transaction ID by 1 so that it is reverted to the original value.
            else
            {
                FormProvider.OrderSummaries.transactionId = (Int32.Parse(FormProvider.OrderSummaries.transactionId) - 1).ToString();
                FormProvider.OrderSummaries.authId = (Int32.Parse(FormProvider.OrderSummaries.authId) - 1).ToString();
            }
            //update the authId on the UI. if the auth isn't successful, the ID number will have been reverted.
            authIdValue.Text = FormProvider.OrderSummaries.authId;
            updateOrder();
            updateDb();
        }

        //clicking the Void button will release the auth hold on the customer’s funds if the auth hasn’t been captured.
        private void voidButton_Click(object sender, EventArgs e)
        {
            //transaction type 4 is used to Void a payment.
            string[] result = CommonPayment.transaction(4, "Transaction Voided", (string)FormProvider.OrderSummaries.selectedOrder[0]["AuthId"], "0", "Voided");
            //if the result of calling the Capture transaction method is 0, execute the code below, otherwise do nothing else.
            if (result[0] == "000000")
            {
                //increment the transactionId by 1
                FormProvider.OrderSummaries.transactionId = (Int32.Parse(FormProvider.OrderSummaries.transactionId) + 1).ToString();
                authIdValue.Text = (string)FormProvider.OrderSummaries.selectedOrder[0]["AuthId"];
                updateOrder();
                updateDb();
            }
        }

        //clicking the Refund button will return the captured funds to the customer’s bank account.
        private void refundCPButton_Click(object sender, EventArgs e)
        {
            //transaction type 3 is used to Refund a payment.
            string[] result = CommonPayment.transaction(3, "Transaction Refunded", (string)FormProvider.OrderSummaries.selectedOrder[0]["AuthId"], totalValue.Text, "Refunded");
            //if the result of calling the Refund transaction method is 0, execute the code below, otherwise do nothing else.
            if (result[0] == "000000")
            {
                {
                    //increment the transactionId by 1
                    FormProvider.OrderSummaries.transactionId = (Int32.Parse(FormProvider.OrderSummaries.transactionId) + 1).ToString();
                    authIdValue.Text = (string)FormProvider.OrderSummaries.selectedOrder[0]["AuthId"];
                    updateOrder();
                    updateDb();
                }
            }
        }

        //clicking the Authorize Payment button will authorize a transaction without capturing it.

        private void authButton_Click_1(object sender, EventArgs e)
        {
            //after a new batch is created, transactionId and authId must have a positive int value, so they must be incremented before the if condition is met and the Auth transaction method is called.
            FormProvider.OrderSummaries.authId = (Int32.Parse(FormProvider.OrderSummaries.transactionId) + 1).ToString();
            FormProvider.OrderSummaries.transactionId = (Int32.Parse(FormProvider.OrderSummaries.transactionId) + 1).ToString();
            //display the authId on the UI
            authIdValue.Text = FormProvider.OrderSummaries.authId;
            updateOrder();
            //call the transaction method of the CommonPayment class passing in 1 for the transaction type, which is used to Authorize a payment.
            string[] authResult = CommonPayment.transaction(1, "Transaction Authorized", "0", totalValue.Text);
            //if the result of calling the Auth transaction method is not 0, decrement the transaction and auth IDs by 1 so that they are reverted to the original values.
            if (authResult[0] != "000000")
            {
                FormProvider.OrderSummaries.transactionId = (Int32.Parse(FormProvider.OrderSummaries.transactionId) - 1).ToString();
                FormProvider.OrderSummaries.authId = (Int32.Parse(FormProvider.OrderSummaries.authId) - 1).ToString();
            }
            //update the Auth Amount on the UI.
            authAmtValue.Text = totalValue.Text;
            //save the auth token returned by the processor.
            authToken = authResult[1];
            //update the AuthID on the UI. if the auth isn't successful, the ID number will have been reverted.
            authIdValue.Text = FormProvider.OrderSummaries.authId;
            updateOrder();
            updateDb();
        }


        //clicking the Capture button will capture the auth
        private void captureButton_Click_1(object sender, EventArgs e)
        {
            //call the transaction method of the CommonPayment class passing in 5 for the transaction type, which is used to capture an auth.
            string[] transactionResult = CommonPayment.transaction(5, "Transaction Successful", (string)FormProvider.OrderSummaries.selectedOrder[0]["AuthId"],totalValue.Text, "Captured");
            //if the result of calling the Capture transaction method is 0, execute the code below, otherwise do nothing else.
            if (transactionResult[0] == "000000")
            {
                //increment the transactionId by 1
                authIdValue.Text = (string)FormProvider.OrderSummaries.selectedOrder[0]["AuthId"];
                //save the transaction token returned by the processor.
                transactionToken = transactionResult[1];
                updateOrder();
                updateDb();
            }
        }

        //clicking the Back button will save the changes that have been made on the Order Detail screen to the database
        private void backButton_Click(object sender, EventArgs e)
        {
            //update the record in the database with any changes made on the Order Detail screen
            updateDb();
            //if the value of the isTransactionProcessing boolean is true, do nothing
            if (CommonPayment.isTransactionProcessing)
            {
                MessageBox.Show("A transaction is processing.", "Warning");
            }
            else
            {
                //hide the Order Detail screen
                this.Hide();
                //call the updateOrders method from the existing object before loading
                //the Orders List screen so that the most current order data is displayed
                FormProvider.OrderSummaries.updateOrders();
                //reset the values of the UI message labels
                labelText = "";
                authIdValue.Text = "";
                authAmtValue.Text = "";
                //display the Orders List screen
                FormProvider.OrderSummaries.Show();
            }
        }

        private void addSmCoffeeButton_Click(object sender, EventArgs e)
        {
            addItem("Small Coffee", 1.00m);
        }

        private void removeSmCoffeeButton_Click(object sender, EventArgs e)
        {
            removeItem("Small Coffee", 1.00m);
        }

        private void addMdCoffeeButton_Click(object sender, EventArgs e)
        {
            addItem("Medium Coffee", 2.00m);
        }

        private void removeMdCoffeeButton_Click(object sender, EventArgs e)
        {
            removeItem("Medium Coffee", 2.00m);
        }

        private void addLgCoffeeButton_Click(object sender, EventArgs e)
        {
            addItem("Large Coffee", 3.00m);
        }

        private void removeLgCoffeeButton_Click(object sender, EventArgs e)
        {
            removeItem("Large Coffee", 3.00m);
        }

        private void addSmLatteButton_Click(object sender, EventArgs e)
        {
            addItem("Small Latte", 2.00m);
        }

        private void removeSmLatteButton_Click(object sender, EventArgs e)
        {
            removeItem("Small Latte", 2.00m);
        }

        private void addMdLatteButton_Click(object sender, EventArgs e)
        {
            addItem("Medium Latte", 4.00m);
        }

        private void removeMdLatteButton_Click(object sender, EventArgs e)
        {
            removeItem("Medium Latte", 4.00m);
        }

        private void addLgLatteButton_Click(object sender, EventArgs e)
        {
            addItem("Large Latte", 6.00m);
        }

        private void removeLgLatteButton_Click(object sender, EventArgs e)
        {
            removeItem("Large Latte", 6.00m);
        }

        private void addSmTeaButton_Click(object sender, EventArgs e)
        {
            addItem("Small Tea", 1.00m);
        }

        private void removeSmTeaButton_Click(object sender, EventArgs e)
        {
            removeItem("Small Tea", 1.00m);
        }

        private void addMdTeaButton_Click(object sender, EventArgs e)
        {
            addItem("Medium Tea", 2.00m);
        }

        private void removeMdTeaButton_Click(object sender, EventArgs e)
        {
            removeItem("Medium Tea", 2.00m);
        }

        private void addLgTeaButton_Click(object sender, EventArgs e)
        {
            addItem("Large Tea", 3.00m);
        }

        private void removeLgTeaButton_Click(object sender, EventArgs e)
        {
            removeItem("Large Tea", 3.00m);
        }
    }
}
