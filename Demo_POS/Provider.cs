namespace French_Press_POS
{
    //set up Singleton design pattern to
    //provide a single point of access to each class instance
    public static class FormProvider
    {
        public static Form1 OrderSummaries
        {
            get
            {
                if (orderSummaries == null)
                {
                    orderSummaries = new Form1();
                }
                return orderSummaries;
            }
        }
        private static Form1 orderSummaries;

        public static Form2 OrderDetail
        {
            get
            {
                if (orderDetail == null)
                {
                    orderDetail = new Form2();
                }
                return orderDetail;
            }
        }
        private static Form2 orderDetail;
    }
}