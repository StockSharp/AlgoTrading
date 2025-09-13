//+------------------------------------------------------------------+
//|                          StatusMailandAlertOnOrderClose.mq4      |
//|                                              Guido Mittmann      |
//|                                           gm4poker@wtnet.de      |
//|                                                                  |
//+------------------------------------------------------------------+
//This EA is made with many help thrue http://www.codesbase.mql4.com
//and the WWW.
//The advantage of an external EA to send the emails  is,
//he tells you all closed order, 
//no matter how many different EA you have run in an MT4-Cient.

extern bool SEND_REPORT_EMAIL       = false;
extern string MinuteOptions         = "--choose somewhat between 10 and 58--";
extern int STATUS_EMAIL_MINUTE     =    55;
extern bool SEND_CLOSED_EMAIL       = false;
extern string StartBalanceOptions   = "--tell him here your startbalance--";
extern int StartBalance             =   500;

datetime lastClose;


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
      for(int pos=0; pos < OrdersHistoryTotal(); pos++) 
         if (OrderSelect(pos, SELECT_BY_POS, MODE_HISTORY) &&  OrderCloseTime()  > lastClose)                
       { 
       lastClose = OrderCloseTime();
       }

   
   
   start();
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
  
//--- Status EMail Settings 

   int flagg;
   if (Minute() < STATUS_EMAIL_MINUTE) flagg=0;{
   if (Minute() == STATUS_EMAIL_MINUTE && SEND_REPORT_EMAIL==true && (DayOfWeek()!=0 || DayOfWeek()!=6) && flagg==0)
      {
      string AccBal = DoubleToStr(AccountBalance(),2);
      double Profit = (AccountBalance()-StartBalance);
      string Profitdec = DoubleToStr(Profit,2);
      double ProfPercent = (AccountBalance()*100/StartBalance-100);
      string Percent = DoubleToStr(ProfPercent,2);
      SendMail("Your EA CashFlowReport ","Your account balance is " + AccBal+" "+AccountCurrency()+". "+"Since you started with  "+StartBalance+" "+AccountCurrency()+" I have already earned \n \n --->> " + Profitdec+" "+AccountCurrency()+" <<--- \n \n for you, which is  "+Percent+" %! not bad right? \n"+" Currently I have  "+OrdersTotal()+" open order.\n");
      if (Minute()>STATUS_EMAIL_MINUTE) flagg=1;

      }
      Sleep(60000);
      RefreshRates();
      } 
      
//--- Closed Orders EMail Settings     

      int flag=0;
      string ordertyp;
      string EAType;
      double x=OrderOpenPrice(),y=OrderClosePrice(),pips;
      pips =y - x;
      pips =pips * 10000;
//      if(pips < 0)pips = - pips;
      if(OrderType()==0)ordertyp="BUY";
      if(OrderType()==1)ordertyp="SELL";
      
   for(int pos=0; pos < OrdersHistoryTotal(); pos++)
    if (OrderSelect(pos, SELECT_BY_POS, MODE_HISTORY))
      {   
      if (OrderCloseTime()    > lastClose )flag=1;
      }             
         if (flag == 1 && SEND_CLOSED_EMAIL==true)
         { 
        SendMail("CLOSED ORDER! Profit: "+DoubleToStr(OrderProfit(),2)+", Balance: "+DoubleToStr(AccountBalance(),2)+", eq: "+DoubleToStr(AccountEquity(),2)+"",
      "Symbol: "+Symbol()+"  "+ordertyp+"  "+(OrderMagicNumber())+" \n"+
      "Comment: "+OrderComment()+" \n"+ 
      "Ticket#: "+OrderTicket()+" \n"+ 
      "Lot: "+DoubleToStr(OrderLots(),2)+" \n"+ 
      "OpenTime: "+TimeToStr(OrderOpenTime())+" \n"+
      "Close Time: "+TimeToStr(OrderCloseTime())+" \n"+
      "Open: "+DoubleToStr(OrderOpenPrice(),5)+" \n"+
      "Close: "+DoubleToStr(OrderClosePrice(),5)+" \n"+
      "Profit: "+DoubleToStr(OrderProfit(),2)+" \n"+
      "Pips: "+DoubleToStr(pips,1)+" \n\n"+

      "Balance: "+DoubleToStr(AccountBalance(),2)+" \n"+
      "Used Margin: "+DoubleToStr(AccountMargin(),2)+" \n"+
      "Free Margin: "+DoubleToStr(AccountFreeMargin(),2)+" \n"+
      "Equity: "+DoubleToStr(AccountEquity(),2)+" \n"+
      "Open Orders: "+DoubleToStr(OrdersTotal(),0)+" \n\n");
      lastClose = OrderCloseTime();
      flag = 0;
      }
   
      return(0);
 }



//----
  
//+------------------------------------------------------------------+