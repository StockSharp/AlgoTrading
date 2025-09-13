/*-------------------------------------------------------------------+
 |                                               FollowYourHeart.mq4 |
 |                                                  Copyright © 2013 |
 |                                              basisforex@gmail.com |
 +-------------------------------------------------------------------*/
#property copyright "Copyright © 2013, basisforex@gmail.com"
#property link      "basisforex@gmail.com"
//---------------------------------------
#define MagicNum  3165
//--------------
extern int     nBars             = 6;
extern double  dLevel            = 2.3;
//----
extern double  profB             = 75;
extern double  profS             = 56;
extern double  lossB             = -54;
extern double  lossS             = -51;
//----
extern int     TP                = 550;
extern int     SL                = 550;
extern bool    UseMM             = false;
extern double  PercentMM         = 5;
extern double  lots              = 0.1;
//----
extern bool    TradingHoursOn    = true;
extern int     OpenSessionHourB  = 6;
extern int     CloseSessionHourB = 12;
extern int     OpenSessionHourS  = 4;
extern int     CloseSessionHourS = 10;
//+------------------------------------------------------------------+
int Get_Broker_Digit()
 {  
   if(Digits == 5 || Digits == 3)
    { 
       return(10);
    } 
   else
    {    
       return(1); 
    }   
 }
//+------------------------------------------------------------------+
int CalculateCurrentOrders()
 {
   int orderT = OrdersTotal(), buys = 0, sells = 0;
   //----
   for(int i = 0; i < orderT; i++)
    {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) break;
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNum)
       {
         if(OrderType() == OP_BUY)  buys++;
         if(OrderType() == OP_SELL) sells++;
       }
    }
   //---- return orders volume
   if(buys > 0) return(buys);
   else if(sells > 0) return(-sells);
   else return(0);
 }
//+------------------------------------------------------------------+
double GetLots()
 { 
   if (UseMM)
    {
      double a;
      a = NormalizeDouble((PercentMM * AccountFreeMargin() / 100000), 1);      
      if(a > 49.9) return(49.9);
      else if(a < 0.1)
       {
         Print("Lots < 0.1");
         return(0);
       }
      else return(a);
    }    
   else return(lots);
 }
//+------------------------------------------------------------------+
void CheckForClose()
 {
   int d = Get_Broker_Digit();
   for(int cnt = 0; cnt < OrdersTotal(); cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() <= OP_SELL && OrderMagicNumber() == MagicNum)
       {
         if(OrderType() == OP_BUY)
          {
            if((OrderProfit() + OrderSwap() > profB) || (OrderProfit() < lossB))
             {
               OrderClose(OrderTicket(), OrderLots(), Bid, 2 * d, Violet);
             }               
          }
         else if(OrderType() == OP_SELL)
          {
            if((OrderProfit() + OrderSwap() > profS) || (OrderProfit() < lossS))
             {
               OrderClose(OrderTicket(), OrderLots(), Ask, 2 * d, Violet);            
             }  
          }
       }
    }
 }
//+------------------------------------------------------------------+
void CheckForOpen()
 { 
   int res;
   int d = Get_Broker_Digit();
   if(Volume[0] < 1) return;
   //======================
   int tot = OrdersHistoryTotal();
   if(tot > 0)
    {
      for(int n = tot - 1; n >= 0; n--)
       {
         OrderSelect(n, SELECT_BY_POS, MODE_HISTORY);
         if(OrderType() == OP_SELL && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNum)
          { 
            break;
          }
       }   
    } 
   if(iBarShift(NULL, 0, OrderCloseTime()) > 0 || tot == 0)
    {
      double o, c, h, l, sum;
      for(int i = 0; i < nBars; i++)
       {
         o = o + ((Open[i] - Open[i+1]) / Open[i+1]) / Point;
         c = c + ((Close[i] - Close[i+1]) / Close[i+1]) / Point;
         h = h + ((High[i] - High[i+1]) / High[i+1]) / Point;
         l = l + ((Low[i] - Low[i+1]) / Low[i+1]) / Point;
       }
      sum = (o + c + h + l) / 4;
      //=======================   
      if(sum > 0 && o > dLevel && c > dLevel && c > o) // BUY
       {
         if(TradingHoursOn == true)  
          {
            if(Hour() < OpenSessionHourB || Hour() > CloseSessionHourB)
             {
               return(0);
             }
          }
         if(1==1)
          {
            res = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 2 * d, 0, 0, "follow", MagicNum, 0, Green);
          }  
         if(res > 0)
          {
            OrderModify(res, Ask, Ask - SL * Point * d, Ask + TP * Point * d, 0, Green);
            return;
          }      
       }//==============================================
      if(sum < 0 && o < -dLevel && c < -dLevel && c < o)  // SELL
       {
         if(TradingHoursOn == true)  
          {
            if(Hour() < OpenSessionHourS || Hour() > CloseSessionHourS)
             {
               return(0);
             }
          }
         if(1==1)
          {
            res = OrderSend(Symbol(), OP_SELL, GetLots(), Bid, 2 * d, 0, 0, "follow", MagicNum, 0, Red);
          }  
         if(res > 0)
          {
            OrderModify(res, Bid, Bid + SL * Point * d,  Bid - TP * Point * d, 0, Red); 
            return;
          }   
       }
    }   
 }   
//+------------------------------------------------------------------+
void start()
 {  
   if(CalculateCurrentOrders() != 0) CheckForClose();
   if(CalculateCurrentOrders() == 0) CheckForOpen();
 }
//+------------------------------------------------------------------+