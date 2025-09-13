//+------------------------------------------------------------------+
//|                                                      EMA WMA.mq4 |
//|                               Copyright © 2009, Vladimir Hlystov |
//|                                                cmillion@narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, Vladimir Hlystov"
#property link      "cmillion@narod.ru"
//--------------------------------------------------------------------
extern int     period_EMA           = 28,
               period_WMA           = 8 ,
               stoploss             = 50,
               takeprofit           = 50,
               risk                 = 10;
double  LOT;
//--------------------------------------------------------------------
double SL,TP;
int TimeBar; //global variable
//--------------------------------------------------------------------
int start()
{
   if (TimeBar==Time[0]) return(0);
   if (TimeBar==0) {TimeBar=Time[0];return(0);}//first program run
   double EMA0 = iMA(NULL,0,period_EMA,0,MODE_EMA, PRICE_OPEN,0);
   double WMA0 = iMA(NULL,0,period_WMA,0,MODE_LWMA,PRICE_OPEN,0);
   double EMA1 = iMA(NULL,0,period_EMA,0,MODE_EMA, PRICE_OPEN,1);
   double WMA1 = iMA(NULL,0,period_WMA,0,MODE_LWMA,PRICE_OPEN,1);
   if (EMA0<WMA0&&EMA1>WMA1) //Buy
   {
      TimeBar=Time[0];                            
      TP  = Ask + takeprofit*Point;
      SL  = Ask - stoploss*Point;     
      LOT = LOT(risk,1);
      CLOSEORDER("Sell");
      OPENORDER ("Buy");
   }
   if (EMA0>WMA0&&EMA1<WMA1) //Sell
   {
      TimeBar=Time[0];                            
      TP = Bid - takeprofit*Point;
      SL = Bid + stoploss*Point;            
      LOT = LOT(risk,1);
      CLOSEORDER("Buy");
      OPENORDER ("Sell");
   }
return(0);
}
//--------------------------------------------------------------------
void CLOSEORDER(string ord)
{
   for (int i=OrdersTotal()-1; i>=0; i--)
   {                                               
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
      {
         if (OrderSymbol()!=Symbol()) continue;
         if (OrderType()==OP_BUY && ord=="Buy")
            OrderClose(OrderTicket(),OrderLots(),Bid,3,CLR_NONE);// Close Buy
         if (OrderType()==OP_SELL && ord=="Sell")
            OrderClose(OrderTicket(),OrderLots(),Ask,3,CLR_NONE);// Close Sell
      }   
   }
}
//--------------------------------------------------------------------
void OPENORDER(string ord)
{
   int error;
   if (ord=="Buy" ) error=OrderSend(Symbol(),OP_BUY, LOT,Ask,2,SL,TP,"", 1,3);
   if (ord=="Sell") error=OrderSend(Symbol(),OP_SELL,LOT,Bid,2,SL,TP,"",-1,3);
   if (error==-1) //operation failed
   {  
      ShowERROR(error,0,0);
   }
return;
}                  
//--------------------------------------------------------------------
void ShowERROR(int Ticket,double SL,double TP)
{
   int err=GetLastError();
   switch ( err )
   {                  
      case 1:                                                                               return;
      case 2:   Alert("Error Connection with trade server absent    ",Ticket," ",Symbol());return;
      case 3:   Alert("Error Invalid trade parameters   Ticket ",  Ticket," ",Symbol());return;
      case 130: Alert("Error Invalid stops   Ticket ",             Ticket," ",Symbol());return;
      case 134: Alert("Error Not enough money   ",                 Ticket," ",Symbol());return;
      case 146: Alert("Error Trade context is busy. ",             Ticket," ",Symbol());return;
      case 129: Alert("Error Invalid price ",                      Ticket," ",Symbol());return;
      case 131: Alert("Error Invalid volume ",                     Ticket," ",Symbol());return;
      case 4051:Alert("Error Invalid function parameter value ", Ticket," ",Symbol());return;
      case 4105:Alert("Error No order selected ",                Ticket," ",Symbol());return;
      case 4063:Alert("Error Integer parameter expected ",       Ticket," ",Symbol());return;
      case 4200:Alert("Error Объект уже существует ",            Ticket," ",Symbol());return;
      default:  Alert("Error Object already exists " ,err,"   Ticket ", Ticket," ",Symbol());return;
   }
}
//--------------------------------------------------------------------
double LOT(int risk,int ord)
{
   double MINLOT = MarketInfo(Symbol(),MODE_MINLOT);
   LOT = AccountFreeMargin()*risk/100/MarketInfo(Symbol(),MODE_MARGINREQUIRED)/ord;
   if (LOT>MarketInfo(Symbol(),MODE_MAXLOT)) LOT = MarketInfo(Symbol(),MODE_MAXLOT);
   if (LOT<MINLOT) LOT = MINLOT;
   if (MINLOT<0.1) LOT = NormalizeDouble(LOT,2); else LOT = NormalizeDouble(LOT,1);
   return(LOT);
}
//--------------------------------------------------------------------

------------------------------------------------------------