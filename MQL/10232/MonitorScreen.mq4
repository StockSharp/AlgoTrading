//+----------------------------------------------------------------+
//|MonitorScreen.mq4                                               |
//|Copyright 2011, Trade Pro Co.                                   |
//|http://www.fxpingpong.com                                       |
//|                                                                |
//| After open a trade                                             |
//| i) take a screen shot in METATRADER/expert/files               |
//| ii) send email to you                                          |
//|                                                                |
//|+++++++++++ Global Variables (Press function Key F3) +++++++++++|
//|                                                                |
//| GMT_Time_Diff : Time difference between Local Time and GMT     |
//| Our Local Time is GMT+8                                        |
//| EST Time is GMT-4 (The last Sunday in April)                   |
//| EST Time is GMT-5 (The first Sunday in November)               |
//|                                                                |
//| GMT_Time_Diff : 12*60*60 = 43200                               |
//| _MonitorScreen_ScreenShot : Set to "1" , take screen shot      |
//| _MonitorScreen_Email : Set to "1" , send email to you          |
//|                                                                |
//+----------------------------------------------------------------+
#property copyright "Copyright 2011, Trade Pro Company"
#property link      "http://www.fxpingpong.com"

string dtbahl()
{
  string dt = "Local Time : "+ TimeToStr(LocalTime( ),TIME_DATE|TIME_SECONDS)+
       "    Est Time : "+ TimeToStr(LocalTime()-GlobalVariableGet("GMT_Time_Diff"),TIME_DATE|TIME_SECONDS)+
       "         "+((Time[0]-Time[1])/60)+" mins Chart"+"   tradeprocom@hotmail.com"+"\n"+
       "Bid :"+DoubleToStr(Bid,Digits)+" Ask :"+DoubleToStr(Ask,Digits) +
       " High :"+DoubleToStr(High[0],Digits)+" Low :"+DoubleToStr(Low[0],Digits);  
  return (dt) ;
}   


//+------------------------------------------------------------------+
//| GlobalVariables initialization                                   |
//+------------------------------------------------------------------+
  void GlobalVar_init() 
  {
  if(!GlobalVariableCheck("GMT_Time_Diff")) GlobalVariableSet("GMT_Time_Diff",43200); 
  if(!GlobalVariableCheck("_MonitorScreen_ScreenShot")) GlobalVariableSet("_MonitorScreen_ScreenShot",1);
  if(!GlobalVariableCheck("_MonitorScreen_Email")) GlobalVariableSet("_MonitorScreen_Email",1);
  }  // End of bool GlobalVar_init()

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
  GlobalVar_init(); 
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
//| Variables define                                                 |
//+------------------------------------------------------------------+

  int LastOpenBars = 0 ;
  int ticket = 0 ;  int cnt = 0 ;
  int cmd ;
  string DisplayTime = "";
  double OldAccountMargin =  0;
  int   OldOrdersTotal    =  0;
  
int start()
  {
    if (OldOrdersTotal != OrdersTotal())
    {
    OldOrdersTotal = OrdersTotal();
    LastOpenBars = 0 ;
    } 
  
    if (OldAccountMargin != AccountMargin())
    {
    OldAccountMargin = AccountMargin();
    LastOpenBars = 0 ;
    } 
  
  if(LastOpenBars == 0)
  { 
  string filename = "" ;
  
  for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
  {
    if (OrderSelect(cnt, SELECT_BY_POS)==true) 
    {
      if (OrderSymbol() == Symbol())
      {
      cmd=OrderType();
      ticket=OrderTicket();
        if(cmd==OP_BUY || cmd==OP_SELL)
        {
        DisplayTime = dtbahl();
        if (cmd==OP_BUY) Comment (DisplayTime,"\n","Buy at "+DoubleToStr(OrderOpenPrice(),Digits)) ;
        if (cmd==OP_SELL) Comment (DisplayTime,"\n","Sell at "+DoubleToStr(OrderOpenPrice(),Digits)) ;
                     
        filename = Symbol()+"_"+ StringSubstr(("0"+Period()),StringLen("0"+Period())-2,2)+"mins"+"_"+ticket+"_Open_MS.gif";
        int handle = FileOpen(filename,FILE_CSV|FILE_READ);
        if(handle<1)
        {
        if (GlobalVariableGet("_MonitorScreen_ScreenShot") != 0 ) 
        {if(!WindowScreenShot(filename,570,428)) Print("WindowScreenShot error: "+GetLastError());}

            if (GlobalVariableGet("_MonitorScreen_Email") != 0 ) 
            {
            if (cmd==OP_BUY) SendMail(OrderSymbol() +", Buy : "+ DoubleToStr(OrderOpenPrice(),Digits)+" tp : " +DoubleToStr(OrderTakeProfit(),Digits) +" sl : "+DoubleToStr(OrderStopLoss(),Digits),"") ;
            if (cmd==OP_SELL) SendMail(OrderSymbol()+", Sell : "+ DoubleToStr(OrderOpenPrice(),Digits)+" tp : " +DoubleToStr(OrderTakeProfit(),Digits) +" sl : "+DoubleToStr(OrderStopLoss(),Digits),"") ;
            }
        }
        else
        FileClose(handle);
        }
      }
    }
  } // for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
  LastOpenBars = Bars ;
  } //    

  DisplayTime = dtbahl();
  Comment (DisplayTime);     
   
  return(0);
  }
//+------------------------------------------------------------------+