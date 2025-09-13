//+------------------------------------------------------------------+
//|                                              gpfTCPivotLimit.mq4 |
//|                                  Copyright © 2006, George-on-Don |
//|                                       http://www.forex.aaanet.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, George-on-Don"
#property link      "http://www.forex.aaanet.ru"

#include <stdlib.mqh>
#include <stderror.mqh>

#define MAGICMA  20050610

extern double               Lots=0.1;   // lot size
extern bool               SndMl=true;   // is send e-mail message?
extern bool      isFloatLots = false;   // is cals lot size 
extern double                DcF = 3;   // optimization factor 
extern double            MaxR = 0.02;   // max risk
extern int             TgtProfit = 1;   // 
extern int             Trailing = 30;   //
extern bool       isTradeDay = False;   // is trade intraday or extraday?
extern bool          isTrace = False;   // is tracer on?


double    Pivot;
double  Resist1;
double  Resist2;
double Support1;
double Support2;
double Support3;
double  Resist3;

double   StpBuy;
double  StpSell;
double  PrftBuy;
double PrftSell;


double                  pPoint;
double r1 , s1 ,s2, r2, r3, s3;
int                        err;
bool              isCals=false;
//+------------------------------------------------------------------+
//| CalculateCurrentOrders function                                   |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0;
   int sells=0;
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) 
         break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  
            buys++;
         if(OrderType()==OP_SELL) 
           sells++;
        }
     }
//---- return orders volume
      if(buys>0) 
         return(buys);
      else
        return(-sells);
  }

//+------------------------------------------------------------------+
//| LotsOptimized function                                           |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   if (isFloatLots == true) 
    {  
 	int    orders=HistoryTotal();     // history orders total
	int    losses=0;                  // number of losses orders without a break
	//---- select lot size
	   lot=NormalizeDouble(AccountFreeMargin()*MaxR/1000.0,1);
	//---- calcuulate number of losses orders without a break
	   if(DcF>0)
     	{
      	for(int i=orders-1;i>=0;i--)
        		{
	         	if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Ошибка в истории!"); break; }
         		if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         	//----
	         	if(OrderProfit()>0) break;
         		if(OrderProfit()<0) losses++;
        		}
      	if(losses>1) lot=NormalizeDouble(lot-lot*losses/DcF,1);
     	}
   }
//---- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
}
//+------------------------------------------------------------------+
//| initialization of Calspivot function                                              |
//+------------------------------------------------------------------+
bool isCalsPivot()
{
   int    tHour;
   double hLine;
   double lLine;
   int i,k;
   if(Volume[0]>1) return(false);
   if (TimeHour(Time[1])== 0 )
   
   {
   tHour  = TimeHour(Time[2]);
 	k= tHour+1;
 	if (isTrace == true)
   {
    Print("tHour",tHour);
    Print("K",k);
   }
 	
   hLine   = High[tHour];
	lLine   = Low[tHour];
  
 	for (i = tHour+1; i > 0; i--)
 	{
 	   
   if (Low[i+1]  < lLine) lLine = Low[i+1];
	if (High[i+1] > hLine) hLine = High[i+1];
    if (isTrace == true)
      {
      Print("i# ",i," lLine# ", lLine, " Low# ", Low[i+1], " hLine #", hLine, " High #", High[i+1], " time #" , TimeHour(Time[i+1]), " day #", TimeDay(Time[i+1]));
      }
   }
   
   pPoint = (lLine + hLine + Close[2]) / 3;
	r1 = 2 * pPoint - lLine;
	s1 = 2 * pPoint - hLine;
	r2 = pPoint + (r1 - s1);
	s2 = pPoint - (r1 - s1);
   r3 = hLine + 2 * (pPoint - lLine);
   s3 = lLine - 2 * (hLine - pPoint);
   CalsPivot();
   isCals = true;
   return (true);
   }
   return (false);
}
//+------------------------------------------------------------------+
//| Cals Pivot    function                                           |
//+------------------------------------------------------------------+
void CalsPivot()
{
    
      Pivot    =NormalizeDouble(pPoint,Digits);
      Resist1  =NormalizeDouble(r1,Digits);
      Resist2  =NormalizeDouble(r2,Digits);
      Resist3  =NormalizeDouble(r3,Digits);
      Support1 =NormalizeDouble(s1,Digits);
      Support2 =NormalizeDouble(s2,Digits);
      Support3 =NormalizeDouble(s3,Digits);
      if (isTrace == true)
         {
         Print("Pivot# ",Pivot," Resist1# ",Resist1," Support1# ",Support1);
         Print("Pivot# ",Pivot," Resist2# ",Resist2," Support2# ",Support2);
         Print("Pivot# ",Pivot," Resist3# ",Resist3," Support3# ",Support3);
         }
}
//+------------------------------------------------------------------+
//| CheckForOpen trade function                                      |
//+------------------------------------------------------------------+

void CheckForOpen()
{
   string sHeaderLetter;
   string   sBodyLetter;
   int             err2;
   double       StopBuy;
   double     ProfitBuy;
   double        bLevel;
   double      StopSell;
   double    ProfitSell;
   double        sLevel;   
   
   // is first tick?
   if(Volume[0]>1) return;
   switch (TgtProfit)
         {
          case 1 : 
            sLevel =      Resist1;
            StopSell =    Resist2;
            ProfitSell = Support1;
            bLevel =     Support1;
            StopBuy =    Support2;
            ProfitBuy =   Resist1;
        
            break;
          case 2 : 
            sLevel =      Resist1;
            StopSell =    Resist2;
            ProfitSell = Support2;
            bLevel =     Support1;
            StopBuy =    Support2;
            ProfitBuy =   Resist2;
    
            break;
          case 3 : 
            sLevel =      Resist2;
            StopSell =    Resist3;
            ProfitSell = Support1;
            bLevel =     Support2;
            StopBuy =    Support3;
            ProfitBuy =   Resist1;
            
            break;
          case 4 : 
            sLevel =      Resist2;
            StopSell =    Resist3;
            ProfitSell = Support2;
            bLevel =     Support2;
            StopBuy =    Support3;
            ProfitBuy =   Resist2;
            
            break;
         case 5 : 
            sLevel =      Resist2;
            StopSell =    Resist3;
            ProfitSell = Support3;
            bLevel =     Support2;
            StopBuy =    Support3;
            ProfitBuy =   Resist3;
            
            break;
         //default : 
    } 
   
   //---- Sell trade
   
     
     if ( ((High[2]>sLevel || Close[2]>=sLevel) && Open[2]< sLevel) && Close[1]<=sLevel)
     {
         if (isTrace == true)
         {
         Print("Close1#", Close[1]);
         Print("Pivot#",Pivot);
         Print("Close2#",Close[2]);
         }
          err = OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,StopSell,ProfitSell,"",MAGICMA,0,Red);
       if (err < 0)
        {
         Print("OrderSend Sell failed err#", GetLastError());
         if (isTrace == true)
               {
               Print("Bid#",Bid);
               Print("Pivot #",Pivot);
               Print("Stop loss#",Resist1);
               Print("Take prof#",Support2);
               }
         if (GetLastError()== 130) err2 =OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,Resist2,Support3,"",MAGICMA,0,Red);
            if ( err2 <0)
            {
            Print("OrderSend Sell failed err2#", GetLastError());
             if (isTrace == True)
             {
               Print("Bid#",Bid);
               Print("Pivot #",Pivot);
               Print("Stop loss#",Resist2);
               Print("Take prof#",Support3);
             }  
            }
         return(0);   
        }else{
          if (SndMl == True ) 
          {
               sHeaderLetter = "Operation SELL by " + Symbol()+"";
               sBodyLetter = "Order Sell by "+ Symbol() + " at " + DoubleToStr(Bid,4)+ ", and set stop/loss at " + DoubleToStr(StpSell,4)+"";
               sndMessage(sHeaderLetter, sBodyLetter);
          }
        }
      //return;
     }
      //---- Buy trade
   if ((( Low[2]<bLevel || Close[2]<=bLevel) && Open[2]> bLevel) && Close[1]>=bLevel)
      {
      if (isTrace == true)
      {
      Print("Close1#", Close[1]);
      Print("Pivot#",Pivot);
      Print("Close2#",Close[2]);
      }
      err=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,StopBuy,ProfitBuy,"",MAGICMA,0,Blue); 
      if (err < 0)
        {
         Print("OrderSend Buy failed err#", GetLastError());
         if (isTrace== true)
            {
            Print("Bid#",Bid);
            Print("Pivot #",Pivot);
            Print("Stop loss#",Support2);
            Print("Take prof#",Resist3);
            }
         if (GetLastError() == 130) err2=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,Support2,Resist3,"",MAGICMA,0,Blue);
               StpBuy  = Support2;
               PrftBuy = Resist2;
            if (err2 < 0)
            {
            Print("OrderSend Buy failed err2#", GetLastError());
            if (isTrace== true)
            {
            Print("Bid#",Bid);
            Print("Pivot #",Pivot);
            Print("Stop loss#",Support2);
            Print("Take prof#",Resist3);
            }
         }
         return(0);   
        }else{
            if ( SndMl == True )
            { 
               sHeaderLetter = "Operation BUY at " + Symbol()+"";
               sBodyLetter = "Order Buy at "+ Symbol() + " for " + DoubleToStr(Ask,4)+ ", and set stop/loss at " + DoubleToStr(StpBuy,4)+"";
               sndMessage(sHeaderLetter, sBodyLetter);
            }
        }      
      return;
      }    
}  

//+------------------------------------------------------------------+
//| CheckForClose open trade function                                |
//+------------------------------------------------------------------+
void CheckForClose()
{
   string sHeaderLetter;
   string   sBodyLetter;
   bool        CloseOrd;
 
//---- 
   if(Volume[0]>1) return;
  
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //----  
      if(OrderType()==OP_BUY)
        {
        if (isTrace== true)
            {
         Print("in OP_BUY");
            }
            if (isTradeDay == True && TimeHour(Time[1])== 23 ) 
               {
            CloseOrd=OrderClose(OrderTicket(),OrderLots(),Bid,3,Lime);
               }
            // trailing stop
            
            if(Trailing>0)  
            {                 
            if (isTrace== true)
               {
            Print("in BUY modify");
               }
            if(Bid-OrderOpenPrice()>Point*Trailing)
               {
               if(OrderStopLoss()<Bid-Point*Trailing)
                  {
                 err= OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*Trailing,OrderTakeProfit(),0,Green);
                                      
                     if (err < 0)
                     {
                        Print("OrderModify Buy failed err#", GetLastError());
                        if (isTrace== true)
                        {
                        Print("Bid#",Bid);
                        Print("Pivot #",OrderOpenPrice());
                        }
                     return(0);
                     }
                  return ;
                     }
                  }
                }
 
            if ( SndMl == True && CloseOrd == True)
               {
                 sHeaderLetter = "Operation CLOSE BUY at" + Symbol()+"";
                 sBodyLetter = "Close order Buy at "+ Symbol() + " for " + DoubleToStr(Bid,4)+ ", and finish this Trade";
                 sndMessage(sHeaderLetter, sBodyLetter);
               }
         break;
        }
      if(OrderType()==OP_SELL)
        {
            if (isTrace== true)
            {
            Print("in OP_SELL");
            }
                                       
            if (isTradeDay == True && TimeHour(Time[1])== 23 ) 
            {
            CloseOrd=OrderClose(OrderTicket(),OrderLots(),Ask,3,Lime);
            }
           // trailing stop
           if(Trailing>0)  
            {                 
             if (isTrace== true)
               {
            Print("in Sell modify");
               }    
            if((OrderOpenPrice()-Ask)>(Point*Trailing))
               {
               if((OrderStopLoss()>(Ask+Point*Trailing)) || (OrderStopLoss()==0))
                  {
                  err = OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*Trailing,OrderTakeProfit(),0,Red);
                     if (err < 0)
                     {
                        Print("OrderModify Sell failed err#", GetLastError());
                        if (isTrace== true)
                           {
                        Print("Bid#",Bid);
                        Print("Pivot #",OrderOpenPrice());
                           }
                     return(0);
                     }
                  return;
                  }
                }
               }
          
         if ( SndMl == True && CloseOrd == True) 
         {
              sHeaderLetter = "Operation CLOSE SELL at" + Symbol()+"";
              sBodyLetter = "Close order Sell at "+ Symbol() + " for " + DoubleToStr(Ask,4)+ ", and finish this Trade";
              sndMessage(sHeaderLetter, sBodyLetter);
         }
         break;
        }
     }
//----
}  
//--------------------------------------------------------------------
// message sending function                                          |
//--------------------------------------------------------------------
void sndMessage(string HeaderLetter, string BodyLetter)
{
   int RetVal;
   SendMail( HeaderLetter, BodyLetter );
   RetVal = GetLastError();
   if (isTrace== true)
   {
   if (RetVal!= ERR_NO_MQLERROR) Print ("Error, message not sending: ", ErrorDescription(RetVal));
   }
}

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//---- 
   if (Period() != PERIOD_H1) 
   {
   Print("Error, Time Frame is not 1 hour!");
   return ; 
   }

   if(Bars<25 || IsTradeAllowed()==false) return;

   isCalsPivot();

     
   if(CalculateCurrentOrders(Symbol())==0 ) 
      CheckForOpen();
   else
     CheckForClose();
   return(0);
  }
//+------------------------------------------------------------------+