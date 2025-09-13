//+------------------------------------------------------------------+
//|                                                     alliheik.mq4 |
//|                                                          Kalenzo |
//|                                          http://www.fxservice.eu |
//+------------------------------------------------------------------+
/***

Project name: Alliheik TRADER
Coder: Kalenzo
Link: http://www.fxservice.eu
Author: Josef Strauss		

***/
#include <stdlib.mqh>

#define MODE_LONG   1
#define MODE_SHORT -1
#define MODE_NONE   0


extern string  _1          = "Indiacator Setup";
extern int     JawsPeriod  = 144,
               JawsMethod  = MODE_SMA,
               JawsPrice   = PRICE_CLOSE,
               JawsShift   = 8;
                  
extern int MaMetod  = 1;
extern int MaPeriod = 21;
extern int MaMetod2  = 3;
extern int MaPeriod2 = 1;               

extern string  n1                   =  "Ea Setup";
extern int     Magic                =  123;
               
extern int     StopLoss             =  0,
               TrailingStop         =  0,
               TakeProfit           =  225,
               Slippage             =  3;
                
extern double  FixedLots            =  0.1;      
               
string Version = "alliheik 1.0";      
bool closeAllowed = false;            
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
   bool upSignal = getSignal(MODE_LONG);
	bool dnSignal = getSignal(MODE_SHORT);
	
	Comment(upSignal+" "+dnSignal);
	
	if(!orderExists())
   {
      //signal 1 buy
      if(upSignal) 
      {
         openOrder(MODE_LONG,FixedLots);
         closeAllowed = false;
      }   
       
      if(dnSignal) 
      {
         openOrder(MODE_SHORT,FixedLots);
         closeAllowed = false;
      }   
   }
   else 
   {
      double jaws = iMA(Symbol(),0,JawsPeriod,JawsShift,JawsMethod,JawsPrice,0);
      OrderSelect(getOrderTicket(),SELECT_BY_TICKET,MODE_TRADES);
      
      if(OrderType() == OP_BUY)
      {
         if(Close[6] > jaws) closeAllowed = true;
         
         if(closeAllowed && MathAbs(Close[6] - jaws)/Point >= 8 && Close[6]<jaws) 
         {
            closeOrder(getOrderTicket());
            return(0);
         }
          
         if(Close[6]>jaws && NormalizeDouble(OrderStopLoss(),Digits) < NormalizeDouble((Close[6] - (TrailingStop*Point)),Digits) ) 
         {
            moveSL(getOrderTicket(),Close[6] - (TrailingStop*Point));
            return(0);
         }   
      }   
      else if(OrderType() == OP_SELL)
      {
         if(Close[6] < jaws) closeAllowed = true;
         
         if(closeAllowed && MathAbs(Close[6] - jaws)/Point >= 8 && Close[6]>jaws) 
         {
            closeOrder(getOrderTicket());
            return(0);
         }
          
         if(Close[6]<jaws && (NormalizeDouble(OrderStopLoss(),Digits) > NormalizeDouble((Close[6] + (TrailingStop*Point)),Digits) || NormalizeDouble(OrderStopLoss(),Digits) == 0) ) 
         {
            moveSL(getOrderTicket(),Close[6] + (TrailingStop*Point));
            return(0);
         }   
      }   
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+
bool getSignal(int mode)
{
   double val1 = iCustom(Symbol(),0,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,0,0);
   double val2 = iCustom(Symbol(),0,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,1,0);
   double val3 = iCustom(Symbol(),0,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,0,1);
   double val4 = iCustom(Symbol(),0,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,1,1);
   
   if(mode == MODE_SHORT && val1>val2 && val3<=val4) return(true);
   if(mode == MODE_LONG && val1<val2 && val3>=val4) return(true);
   return(false);

}
//+------------------------------------------------------------------+  
bool moveSL(int ticket,double stoploss)
{
   
   if(!IsTradeAllowed())
   return (false);
   
   if(MathAbs(Ask-stoploss)/Point < MarketInfo(Symbol(),MODE_STOPLEVEL)) 
   {
      Print("STOP LOSS too close ",Bid," SL ",stoploss);
      return(false);
   }
   
   int error;
   
   int MAXRETRIES = 5;
   int retries = 0;
   while(!OrderModify(ticket,OrderOpenPrice(), stoploss, OrderTakeProfit(), 0,CLR_NONE))
   {
      error = GetLastError();
      
      if(error>1)   
      Print("MoveSL failed with error #",ErrorDescription(error)," CurrentSL ",OrderStopLoss()," NewSL ",stoploss);
       
      Sleep(1000);
            
      RefreshRates();
            
      if(retries >= MAXRETRIES) 
      return(false);
      else
      retries++;
   }
   
}  
//+------------------------------------------------------------------+  
bool isOrderAllowed()
{
    int totalH = OrdersHistoryTotal();
    
    for(int cntH = 0 ; cntH<=totalH; cntH++)
    {
		OrderSelect(cntH, SELECT_BY_POS, MODE_HISTORY);

		if( OrderMagicNumber() == getMagic() || OrderMagicNumber() == getMagic()) 
		{
			int orderOShiftH = iBarShift(Symbol(),0,OrderOpenTime(),false);
			//int orderCShiftH = iBarShift(Symbol(),0,OrderCloseTime(),false);
			
			if(/*orderCShiftH == 0 || */orderOShiftH == 0)
			return(false);	
			
		}	
    }
	return(true);
}
//+------------------------------------------------------------------+
int getOrderTicket()
{
    int total = OrdersTotal();
    for(int cnt = 0 ;cnt<=total;cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderMagicNumber() == getMagic()) 
      return(OrderTicket());
    } 
   return(-1); 
}
//+------------------------------------------------------------------+

bool closeOrder(int ticket)
{
   if(!IsTradeAllowed())
   return (false);
   
   int MAXRETRIES = 5;
   int retries = 0;
   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   int error;
   if(OrderType() == OP_BUY)
   {
      while(!OrderClose(ticket,OrderLots(), Bid, Slippage, Red))
      {
      
            error = GetLastError();
            
            if(error>1)
            Print("OrderClose failed with error #",ErrorDescription(error));
            
            Sleep(1000);
            
            RefreshRates();
         
            if(retries >= MAXRETRIES) 
              return(false);
            else
              retries++;
        }

   }
   else
   {
         while(!OrderClose(ticket, OrderLots(), Ask, Slippage, Red))
         {
            error = GetLastError();
            
            if(error>1)
            Print("OrderClose failed with error #",ErrorDescription(error));
            
            Sleep(1000);
            
            RefreshRates();
         
            if(retries >= MAXRETRIES) 
              return(false);
            else
              retries++;
        }

   }
   
   return (true);
}
//+------------------------------------------------------------------+
bool orderExists()
{
    int total = OrdersTotal();
    for(int cnt = 0 ;cnt<=total;cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderMagicNumber() == getMagic()) 
      return(true);
    } 
    return(false); 
}
//+------------------------------------------------------------------+
int getMagic()
{  
   return(Magic);
      
}
//+------------------------------------------------------------------+
bool openOrder(int type,double lots, string description = "" )
{
   	
	if(!IsTradeAllowed())
	{
		return (-1);
	}
   
	int error = 0;
	int ticket = 0;
 
	double tp = 0, sl = 0;
  
	if( type == MODE_SHORT  && isOrderAllowed())
	{
		while(true)
		{
		   RefreshRates();
		 
			double bidPrice = MarketInfo(Symbol(),MODE_BID);
			
			if(TakeProfit>0)
			{
				tp = bidPrice-TakeProfit*Point; 
				
				if( (MathAbs(bidPrice-tp)/Point) < MarketInfo(Symbol(),MODE_STOPLEVEL)) 
				{
					Print(Symbol()+" SHORT TAKE PROFIT TOO CLOSE");
					return(0);
				}
			}
	  
			if(StopLoss>0)
			{
				sl = bidPrice+StopLoss*Point; 
				
				if( (MathAbs(bidPrice-sl)/Point) < MarketInfo(Symbol(),MODE_STOPLEVEL)) 
				{
					Print(Symbol()+" SHORT STOP LOSS TOO CLOSE");
					return(0);
				}
			}
		    
		   ticket = OrderSend(Symbol(),OP_SELL,lots,bidPrice,Slippage,sl,tp,StringConcatenate(Version," ",description),getMagic(),0,Green);
         
			if(ticket<=0)
			{
				error=GetLastError();
            Print("SELL ORDER ERROR:", ErrorDescription(error)," BID ",bidPrice," SL  ",sl, " tp  ",tp);
            
            if(!ErrorBlock(error)) break;
            
			}
			else
			{
			   OrderSelect(ticket,SELECT_BY_TICKET);
			   OrderPrint(); 
			   break;
			}
		} 	
   }
   else if( type == MODE_LONG && isOrderAllowed())
   {
		
		while(true)
		{
		   RefreshRates();
		    
			double askPrice = MarketInfo(Symbol(),MODE_ASK);
			
			if(TakeProfit>0)
			{
				tp = askPrice+TakeProfit*Point; 
				
				if( (MathAbs(askPrice-tp)/Point) < MarketInfo(Symbol(),MODE_STOPLEVEL)) 
				{
					Print(Symbol()+" LONG TAKE PROFIT TOO CLOSE");
					return(0);
				}
			}
	  
			if(StopLoss>0)
			{
				sl = askPrice-StopLoss*Point; 
				
				if( (MathAbs(askPrice-sl)/Point) < MarketInfo(Symbol(),MODE_STOPLEVEL)) 
				{
					Print(Symbol()+" LONG STOP LOSS TOO CLOSE");
					return(0);
				}
		 	}
			
			
		 	ticket = OrderSend(Symbol(),OP_BUY, lots,askPrice,Slippage,sl,tp,StringConcatenate(Version," ",description),getMagic(),0,Green);
          
			if(ticket<=0)
			{
				error=GetLastError();
            Print("BUY ORDER ERROR:", ErrorDescription(error)," ASK ",askPrice," SL  ",sl, " tp  ",tp);
            
            if(!ErrorBlock(error)) break;
            
			}
			else
			{
			   OrderSelect(ticket,SELECT_BY_TICKET);
			   OrderPrint(); 
			   break;
			}
			
		}
   }
    
   return (0);
}
//+------------------------------------------------------------------+
bool ErrorBlock(int error = 0)
{
   
   switch(error)
   {
       case 0: 
       {
         //no error - exit from loop
         Print("NO ERROR");
         return(false);
       }
       case 2:
       {
           Print("System failure. Reboot the computer/check the server");
           return(false);  
       }
       case 3:
       {
           Print("Error of the logic of the EA");
           return(false);  
       }
       case 4:
       {
           Print("Trading server is busy. Wait for 2 minutes.");
           Sleep(120000);
           return(true);   
       }
       case 6:
       { 
           bool connect = false;
           int iteration = 0;
           Print("Disconnect ");
           while((!connect) || (iteration > 60))
           {
               Sleep(10000);
               Print("Connection not restored", iteration*10,"  seconds passed");
               connect = IsConnected();
               if(connect)
               {
                   Print("Connection restored");
               }
               iteration++;
           }
           Print("Connection problems");
           return(false);  
       }
       case 8:
       {
           Print("Frequent requests");
           return(false);  
       }
       case 64:
       {
           Print("Account is blocked!");
           return(false);  
       }
       case 65:
       {
           Print("Wrong account number???");
           return(false);  
       }
       case 128:
       {//????
           Print("Waiting of transaction timed out");
           Sleep(10000);//10 seconds
           RefreshRates();
           return(false);  
       }
       case 129:
       {
           Print("Wrong price");
           RefreshRates();
           return(false);  
       }
       case 130:
       {
           Print("Wrong stop SLEVEL"+MarketInfo(Symbol(),MODE_STOPLEVEL)+" FZLVL "+MarketInfo(Symbol(),MODE_FREEZELEVEL)+" FZLVL "+MarketInfo(Symbol(),MODE_SPREAD));
           RefreshRates();
           return(false);   
       }
       case 131:
       {
           Print("Wrong calculation of trade volume");
           return(false);  
       }
       case 132:
       {
           Print("Market closed");
           return(false);  
       }
       case 134:
       {//NOT ENOUGH CASH?
           Print("Lack of margin for performing operation, margin: "+AccountFreeMargin());
           
           return(false);  
       }
       case 135:
         {
           Print("Prices changed");
           RefreshRates();
           return(true);  
         }
       case 136:
         {
           Print("No price!");
           return(false);  
         }
       case 138:
         {
           Print("Requote again!");
           RefreshRates();
           return(true);  
         }
       case 139:
         {
           Print("The order is in process. Program glitch");
           Sleep(10000);//10 seconds
           return(true);  
         }
       case 141:
         {
           Print("Too many requests");
           Sleep(10000);//10 seconds 
           return(true);  
         }
       case 148:
         {
           Print("Transaction volume too large");
           return(false);  
         }                                          
         default:
         {  
            Print("Unhandeled exception code:",error," stoplevel ",MarketInfo( Symbol(), MODE_STOPLEVEL) ," spread ",MarketInfo( Symbol(), MODE_SPREAD));
            return(false);
         }
     }
   
  }

