//+------------------------------------------------------------------+
//|                                                    EA_E-mail.mq4 |
//|                                               erekit83@yahoo.com |
//|                                         http://100k2.blogspt.com |
//+------------------------------------------------------------------+
#property copyright "erekit83@yahoo.com"
#property link      "http://100k2.blogspt.com"

//---- input parameters
extern int       TimeInterval=30;    // xx minutes to send the e-mail
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----

   while(TimeInterval>0)
   {
       
       SendMail("Forex Account : " +AccountName()+ " Details",
      "Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+
      "Balance       : "+DoubleToStr(AccountBalance(),2)+" \n"+
      "Used Margin   : "+DoubleToStr(AccountMargin(),2)+" \n"+
      "Free Margin   : "+DoubleToStr(AccountFreeMargin(),2)+" \n"+
      "Equity        : "+DoubleToStr(AccountEquity(),2)+" \n"+
      "Open Orders   : "+DoubleToStr(OrdersTotal(),0)+" \n\n"+

      "Broker  : "+AccountCompany()+" \n"+
      "Leverage: "+AccountLeverage()+"" );
       
       
       Sleep( TimeInterval*60*1000);    //sleep in miliseconds, so use 60*1000 to change to minute
   
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+