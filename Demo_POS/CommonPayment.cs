using System;
using System.Windows.Forms;
using POSLink;

namespace French_Press_POS
{
    public static class CommonPayment
    {
        public static bool isTransactionProcessing;

        public static CommSetting commSettings()
        {
            CommSetting commSetting;
            commSetting = new CommSetting();

            commSetting.CommType = "HTTP";
            commSetting.DestIP = "192.168.50.208";
            commSetting.DestPort = "10009";
            commSetting.TimeOut = "60000";
            //save communication parameters to commsetting.ini file
            //located in current “execute” folder (bin/debug).
            //The file is written by CommSetting class and read by POSLink class.
            commSetting.saveFile();

            return commSetting;
        }

        public static string[] transaction(int transType, string labelMessage, string authId = "0", string amount = "0", string newStatus = "")
        {
            //the code in this method follows the specifications in PAX's POSLink API Guide.

            //instantiate a new, empty PosLink object named poslink
            PosLink poslink;
            poslink = new PosLink();

            //instantiate a new, empty PaymentRequest object named paymentReq
            //to view more information about the POSLink PaymentRequest class, see PAX's POSLink API Guide
            PaymentRequest paymentReq;
            paymentReq = new PaymentRequest();

            //set the CommSetting property of the poslink object equal to the result of calling the commSettings method, defined above.
            poslink.CommSetting = commSettings();
            //tenderType 1 accepts card payments, but can be set in the POS to dynamically accept cash, card or other tender types.
            //view the complete list of tender types in PAX's POSLink API Guide.
            paymentReq.TenderType = 1;
            paymentReq.TransType = transType;
            //if the transaction is an authorization or a refund, don't set the OrigRefNum, which is equal to the authId.
            //for authorizations, the transaction is not being performed on an auth (a new auth is being created) so there is no
            //existing auth to reference.
            //refunds are performed on captures, not auths, so nothing is referenced in OrigRefNum for refunds. the ECRRefNum property below is used to reference the ID of the capture to be refunded.
            if (paymentReq.TransType != 1 && paymentReq.TransType != 3)
            {
                paymentReq.OrigRefNum = authId;
            }
            //if the transaction is a void, don't set the amount. Voids don't accept an amount because they are always
            //for the full authorized amount.
            if (paymentReq.TransType != 4)
            {
                paymentReq.Amount = amount.Trim('$');
            }
            //submit the transactionId as the ECR (Electronic Card Reader) Reference Number
            paymentReq.ECRRefNum = FormProvider.OrderSummaries.transactionId;
            //set the PaymentRequest property of the poslink object equal to paymentReq
            poslink.PaymentRequest = paymentReq;

            //before calling the PAX method to process the transaction, set the isTransactionProcessing boolean to true.
            //this is used in Form 2 to prevent any user action that might disrupt the transaction request and response from being processed.
            isTransactionProcessing = true;
            //call the POSLink API method ProcessTrans of the poslink object
            poslink.ProcessTrans();
            //save the response to a PaymentResponse object, paymentRes
            //to view more information about the POSLink PaymentResponse class, see PAX's POSLink API Guide
            PaymentResponse paymentRes = poslink.PaymentResponse;
            //set the isTransactionProcessing boolean to false
            isTransactionProcessing = false;


            Console.WriteLine("paymentRes.ResultCode = " + paymentRes.ResultCode);

            //the ResultCode property of the PaymentResponse object will include a numeric value indicating the transaction's status.
            //if the ResultCode is 0, the transaction was successful, and the code below should be executed.
            if (paymentRes.ResultCode == "000000")
            {
                //set the labelText property on Form 2 equal to the message arg that was passed into this method
                //to display a status message to the user based on the transaction type that was performed.
                FormProvider.OrderDetail.labelText = labelMessage;
                //if the transaction type isn't auth, update the order status to reflect the result of the
                //payment function that was performed. the order status is displayed in the tables on Form 1
                if (paymentReq.TransType != 1)
                {
                    FormProvider.OrderDetail.updateOrderStatus(newStatus);
                }
            }
            //if the ResultCode property of paymentRes is not 0, the transaction was not successful, and the code below should be executed
            else
            {
                //display the error response that was received from the processor
                FormProvider.OrderDetail.labelText = paymentRes.ResultTxt;
            }
            //return an array of strings, including the HostRespons and the HostCode.
            //the HostCode is the token that can be used to refer to the transaction for various reasons
            //including troubleshooting with the payment processor or performing subsequent actions on a transaction from a separate API.
            string[] result = { paymentRes.ResultCode, paymentRes.HostCode };

            return result;
        }

        public static string batch()
        {
            //the code in this method follows the specifications in PAX's POSLink API Guide.

            //instantiate a new, empty PosLink object named poslink
            PosLink poslink;
            poslink = new PosLink();

            //instantiate a new, empty BatchRequest object named batchReq
            //to view more information about the POSLink BatchRequest class, see PAX's POSLink API Guide
            BatchRequest batchReq;
            batchReq = new BatchRequest();

            //set the CommSetting property of the poslink object equal to the result of calling the commSettings method, defined above.
            poslink.CommSetting = commSettings();

            //1 = close current batch, 
            batchReq.TransType = 1;
            poslink.BatchRequest = batchReq;

            //before calling the PAX method to close the batch, set the isTransactionProcessing boolean to true.
            //this is used in Form 1 to prevent any user action that might disrupt the batch request and response from being processed.
            isTransactionProcessing = true;
            //call the POSLink API method ProcessTrans of the poslink object
            poslink.ProcessTrans();
            //save the response to a BatchResponse object, batchRes
            //to view more information about the POSLink BatchResponse class, see PAX's POSLink API Guide
            BatchResponse batchRes = poslink.BatchResponse;
            //set the isTransactionProcessing boolean to false
            isTransactionProcessing = false;

            Console.WriteLine("batchRes.ResultCode = " + batchRes.ResultCode);

            //if the ResultCode property of batchRes is 0, the batch was successful, and the code below should be executed
            if (batchRes.ResultCode == "000000")
            {
                MessageBox.Show("Your batch was submitted to the payment processor for settlement and funding.", "Batch Successful");
                //call the updateBatchStatus method to update the status of any Captured transactions to Batched
                FormProvider.OrderSummaries.updateBatchStatus("Captured", "Batched");
            }
            else
            {
                //if the ResultCode property of batchRes is not 0, the batch was not successful, and the code below should be executed
                MessageBox.Show(batchRes.ResultTxt, "Batch Error");
            }

            return batchRes.ResultCode;

        }
    }
}

