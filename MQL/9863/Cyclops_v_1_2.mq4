//+------------------------------------------------------------------+
//|                                                          Cyclops |
//|                                                          JoForex |
//|                                          contact@pipdetector.com |
//+------------------------------------------------------------------+
#property copyright "JoForex"
#property link      "contact@pipdetector"

#include <stdlib.mqh>

 
extern int        Magic                         = 8001;
extern double     BaseLot                       = 0.1;
extern double     MaxLotSize                    = 50; 
extern double     MinLotSize                    = 0.1;
extern bool       UseMoneyManagement            = true;
extern bool       IsMicroAcc                    = false;
extern double     EquityPRC                     = 2;

extern int        TakeProfit                    = 0;
extern int        StopLoss                      = 0;

extern bool       UseExitSignal                 = true;

extern bool       UseTimeRestriction            = false;
extern int        DayEnd                        = 5;
extern int        HourEnd                       = 12; 

extern int        BreakEvenTrigger              = 0;//if 0 then is not used

extern int        TrailingStopTrigger           = 0;//if 0 then is not used
extern int        TrailingStopPips              = 0;

extern bool       EmailOn                       = true;
extern bool       AlertOn                       = true;
extern bool       ScreenshotOn                  = true;
extern string     _n                            = "INDICATOR";
extern int        shift                         = 0;//0 current bar, 1 closed
extern int        PriceActionFilter             = 1;
extern int        Length                        = 3;
extern int        MajorCycleStrength            = 4;
extern bool       UseCycleFilter                = false;
extern int        UseFilterSMAorRSI             = 1;
extern int        FilterStrengthSMA             = 12;
extern int        FilterStrengthRSI             = 21;
extern bool       UseMomentumFilter             = true;
extern int        MOMENTUM_PERIOD               = 14,
                  MOMENTUM_PRICE                = PRICE_CLOSE;
extern double     MomentumTriggerLong           = 100,
                  MomentumTriggerShort          = 90;                  

string Version;
int bm;
int barCheck = 0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   Version = "Cyclops_MoM v 1.2";
   bm = 1;
   if(MarketInfo(Symbol(),MODE_DIGITS) == 5 ||  MarketInfo(Symbol(),MODE_DIGITS) == 3)       bm = 10;
   if(TakeProfit>0)                 TakeProfit                 =  TakeProfit                 * bm;   
   if(TrailingStopTrigger>0)        TrailingStopTrigger        =  TrailingStopTrigger        * bm;
   if(TrailingStopPips>0)           TrailingStopPips           =  TrailingStopPips           * bm;
   if(StopLoss>0)                   StopLoss                   =  StopLoss                   * bm;
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
   
   bool timeCheck = true;
   if(UseTimeRestriction) 
   {
      timeCheck = TimeDayOfWeek(TimeCurrent()) < DayEnd;
      if(timeCheck)
      {
         if(TimeDayOfWeek(TimeCurrent()) == DayEnd)
         timeCheck = TimeHour(TimeCurrent())<=HourEnd;
      }         
   }
   double tp = 0;      
   double sl = 0;     
   
   
   int orders = orderCount(-1);
   if(orders != 0)
   {
       
      if(timeCheck)
      {
         for(int pos=0;pos<OrdersTotal();pos++)
         {
            
            if(OrderSelect(pos,SELECT_BY_POS)==false) continue;
            if(OrderMagicNumber()!=getMagic())continue;
             
            //EXIT 
            if(UseExitSignal && getExitSignal(OrderType()) && profitCount()>0)
            {
               closeAllPoisitions(OrderType());
               break;
            } 
            
            //REVERSAL
            if(OrderType() == OP_SELL && getSignal(OP_BUY)) 
            {
               closeAllPoisitions(OP_SELL);
               break;
            }
            else if(OrderType() == OP_BUY && getSignal(OP_SELL)) 
            {
               closeAllPoisitions(OP_BUY);
               break;
            } 
            
            //BreakEven
            if(BreakEvenTrigger>0)
            {
               if(OrderType() == OP_BUY)
               {			
                  if(OrderStopLoss() < NormalizeDouble(OrderOpenPrice()+(1*bm*Point),Digits) && Ask > ((OrderOpenPrice()+(1*bm*Point))+BreakEvenTrigger*Point))
                  {
                      if(NormalizeDouble(OrderStopLoss(),Digits)!=NormalizeDouble(OrderOpenPrice()+(1*bm*Point),Digits))
                      moveSL(OrderTicket(),OrderOpenPrice()+(1*bm*Point));  
                  }
               }
               else if(OrderType() == OP_SELL)
               {
                  if(OrderStopLoss() > NormalizeDouble(OrderOpenPrice()-(1*bm*Point),Digits) && Bid < ((OrderOpenPrice()-(1*bm*Point))-BreakEvenTrigger*Point))
                  {
                      if(NormalizeDouble(OrderStopLoss(),Digits)!=NormalizeDouble(OrderOpenPrice()-(1*bm*Point),Digits))
                      moveSL(OrderTicket(),OrderOpenPrice()-(1*bm*Point));  
                  }
               } 
            }
            
            
            //TRAILING
            if(TrailingStopTrigger > 0)
            {
               if(OrderType() == OP_BUY)
               {			
                  if(OrderStopLoss() <   Ask - (TrailingStopPips*Point)&& Ask > (OrderOpenPrice()+TrailingStopTrigger*Point))
                  {
                     if(NormalizeDouble(OrderStopLoss(),Digits)!=NormalizeDouble(Ask - (TrailingStopPips*Point),Digits))
                     moveSL(OrderTicket(),NormalizeDouble(Ask - (TrailingStopPips*Point),Digits));  
                  } 
               }
               else if(OrderType() == OP_SELL)
               {
                  if(OrderStopLoss() >  Bid + (TrailingStopPips*Point) && Bid < (OrderOpenPrice()-TrailingStopTrigger*Point))
                  {
                     if(NormalizeDouble(OrderStopLoss(),Digits)!=NormalizeDouble(Bid + TrailingStopPips*Point,Digits))
                     moveSL(OrderTicket(),NormalizeDouble(Bid + TrailingStopPips*Point,Digits));
                  }	 
               }
            }
                       
         }
      }  
   }      
   
   
   //OPEN Order
   
    
   if(timeCheck && isOrderAllowed()/*one order per bar*/ )
   {  
      if(getSignal(OP_BUY)) 
      {
         if(TakeProfit>0) tp =  Ask + TakeProfit * Point;
         
         if(StopLoss>0)
         {
            sl = Ask - StopLoss*Point;
         }
         
         openOrder(LotSize(OP_BUY),OP_BUY, sl,tp,"START");
         if(EmailOn) SendMail("Message from "+Version,"LONG TRADE TAKEN AT "+Symbol()+" PRICE "+Ask);
         if(AlertOn) Alert("LONG TRADE TAKEN AT "+Symbol()+" PRICE "+Ask);
      }
      
      if(getSignal(OP_SELL)) 
      {
         if(TakeProfit>0) tp =  Bid - TakeProfit * Point;
         
         if(StopLoss>0)
         {
            sl = Bid + StopLoss * Point;
         }
         
         openOrder(LotSize(OP_SELL),OP_SELL, sl,tp,"START");
         if(EmailOn) SendMail("Message from "+Version,"SHORT TRADE TAKEN AT "+Symbol()+" PRICE "+Bid);
         if(AlertOn) Alert("SHORT TRADE TAKEN AT "+Symbol()+" PRICE "+Ask); 
      }            
   }      
    
//----
   
   return(0);
  }
//+------------------------------------------------------------------+
bool momentum(int mode)
{
   if(!UseMomentumFilter) return(true);
   double m = iMomentum(Symbol(),0,MOMENTUM_PERIOD,MOMENTUM_PRICE,shift);
   if(mode == OP_BUY && bm < MomentumTriggerLong) return(true);
   else if(mode == OP_SELL && bm > MomentumTriggerShort) return(true);
   else return(false);
}
bool getSignal(int mode)
{
   double _1 = 0,_2 = 0;
   if(mode == OP_BUY && momentum(OP_BUY))
   {
      _1 = iCustom(Symbol(),0,"CycleIdentifier",PriceActionFilter,Length,MajorCycleStrength,UseCycleFilter,UseFilterSMAorRSI,FilterStrengthSMA,FilterStrengthRSI,1,shift+1);  
      if(_1 == -1) return(true);
   }
   else if(mode == OP_SELL && momentum(OP_SELL))
   {
       
      _2 = iCustom(Symbol(),0,"CycleIdentifier",PriceActionFilter,Length,MajorCycleStrength,UseCycleFilter,UseFilterSMAorRSI,FilterStrengthSMA,FilterStrengthRSI,2,shift+1);  
      if(_2 == 1) return(true);
   }  
    
   return(false); 
}
//----
bool getExitSignal(int mode)
{
 
   double _1 = 0,_2 = 0;
   double _1L = 0,_2L = 0;
   if(mode == OP_SELL)
   {
      _1 = iCustom(Symbol(),0,"CycleIdentifier",PriceActionFilter,Length,MajorCycleStrength,UseCycleFilter,UseFilterSMAorRSI,FilterStrengthSMA,FilterStrengthRSI,3,shift+1);
      _1L = iCustom(Symbol(),0,"CycleIdentifier",PriceActionFilter,Length,MajorCycleStrength,UseCycleFilter,UseFilterSMAorRSI,FilterStrengthSMA,FilterStrengthRSI,1,shift+1);  
      
          
      if(_1==-1 && _1L!=-1) return(true);
      else return(false);   
   }
   
   if(mode == OP_BUY)
   {
      _2 = iCustom(Symbol(),0,"CycleIdentifier",PriceActionFilter,Length,MajorCycleStrength,UseCycleFilter,UseFilterSMAorRSI,FilterStrengthSMA,FilterStrengthRSI,4,shift+1);  
      _2L = iCustom(Symbol(),0,"CycleIdentifier",PriceActionFilter,Length,MajorCycleStrength,UseCycleFilter,UseFilterSMAorRSI,FilterStrengthSMA,FilterStrengthRSI,2,shift+1);  
           
      if(_2 == 1 && _2L != 1) return(true);
      else return(false);   
   }  
      
   return(false); 
}
//----
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
       
      Sleep(10000);
            
      RefreshRates();
            
      if(retries >= MAXRETRIES) 
      return(false);
      else
      retries++;
   }
   
} 
//----
int orderCount(int type)
{
    int total = OrdersTotal();
    int oc = 0;
    for(int cnt = 0 ;cnt<=total;cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);

      if(OrderMagicNumber() == getMagic() && (OrderType() == type || type == -1)) 
      oc+=1;
    } 
    return(oc);
}
//----
double LotSize(int type)
{
   if(!UseMoneyManagement) return(BaseLot);

   double lots = BaseLot;
   
   if(IsMicroAcc)
      lots = NormalizeDouble(BaseLot*(orderCount(type)+1),2);
   else
      lots = NormalizeDouble(BaseLot*(orderCount(type)+1),1);

   return(MaxMinLot(lots));
}

double MaxMinLot (double lots)
{
   if(lots>MaxLotSize)
   lots = MaxLotSize;
    
   if(lots<MinLotSize)
   lots = MinLotSize;
   
   return(lots);
}
//----
int getMagic()
{  
   return(Magic);
}
//----
bool isOrderAllowed()
{
    int totalH = OrdersHistoryTotal();
    
    for(int cntH = 0 ; cntH<=totalH; cntH++)
    {
		OrderSelect(cntH, SELECT_BY_POS, MODE_HISTORY);
      if((OrderMagicNumber() == getMagic())   ) 
		{
			int orderOShiftH = iBarShift(Symbol(),0,OrderOpenTime(),false);
		 	
			if(orderOShiftH == 0 ) 
			return(false);	
		}	
    }
    
    
    int total  = OrdersTotal();
    
    for(int cnt = 0 ; cnt <= total; cnt++)
    {
		OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
		
      if((OrderMagicNumber() == getMagic())  ) 
		{
			int orderOShift = iBarShift(Symbol(),0,OrderOpenTime(),false);
		 	
			if(orderOShift == 0 ) 
			return(false);	
		}	
    }
    
    
	return(true);
}
//----
bool openOrder(double LTS,int type, double sl,double tp, string description = "" )
{
   if(!IsTradeAllowed())
	{
		return (-1);
	}
   
	int error = 0;
	int ticket = 0;
  
   
   	
	if( type == OP_SELL )
	{
		while(true)
		{
		   RefreshRates();
		    
		 	ticket = OrderSend(Symbol(),OP_SELL,LTS,MarketInfo(Symbol(),MODE_BID),0,0,0,StringConcatenate(Version," ",description),getMagic(),0,Pink);
         
			if(ticket<=0)
			{
				error=GetLastError();
            Print("SELL ORDER ERROR:", ErrorDescription(error));
            
            if(!ErrorBlock(error,LTS)) break;
            
			}
			else
			{
			   if(sl>0 || tp>0)
			   {
			      OrderSelect(ticket,SELECT_BY_TICKET);
               OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green);
            }
            if(ScreenshotOn)doScreenshot();
 			   OrderPrint(); 
			   break;
			}
		} 	
   }
   else if( type == OP_BUY )
   {
		
		while(true)
		{
		   RefreshRates();
		   
			ticket = OrderSend(Symbol(),OP_BUY,LTS,MarketInfo(Symbol(),MODE_ASK),0,0,0,StringConcatenate(Version," ",description),getMagic(),0,Lime);
         
			if(ticket<=0)
			{
				error=GetLastError();
            Print("BUY ORDER ERROR:", ErrorDescription(error));
            
            if(!ErrorBlock(error,LTS)) break;
            
			}
			else
			{
			   if(sl>0 || tp>0)
			   {
			      OrderSelect(ticket,SELECT_BY_TICKET);
               OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green);
			   }
			   if(ScreenshotOn)doScreenshot();
			   OrderPrint(); 
			   break;
			}
			
		}
   }
    
   return (0);
}
//+------------------------------------------------------------------+
bool ErrorBlock(int error = 0,double lot = 0)
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
            Print("Unhandeled exception code:",error," stoplevel ",MarketInfo( Symbol(), MODE_STOPLEVEL) ," spread ",MarketInfo( Symbol(), MODE_SPREAD)+" LOTS:"+ lot);
            return(false);
         }
     }
   
  }
  void doScreenshot()
  {
      string dateMarker = TimeYear(TimeCurrent())+"_"+TimeMonth(TimeCurrent())+"_"+TimeDay(TimeCurrent())+"___"+TimeHour(TimeCurrent())+"_"+TimeMinute(TimeCurrent())+"_"+TimeSeconds(TimeCurrent());
      WindowScreenShot(getMagic()+" "+Version+" "+dateMarker+".gif", 800, 600, 0, 1, 0);
  }
void closeAllPoisitions(int type)
{
   //close all
   int oc = orderCount(type);
   while(oc > 0)
   {
      int total = OrdersTotal();
      RefreshRates();
      for(int cnt = 0 ;cnt<=total;cnt++)
      {
        OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
        if(OrderMagicNumber() == getMagic()) 
        {
            if(OrderType() == OP_BUY && type == OP_BUY)
            {
               OrderClose(OrderTicket(),OrderLots(), Bid, 0, Yellow);
            }
            else if(OrderType() == OP_SELL && type == OP_SELL)
            {
               OrderClose(OrderTicket(),OrderLots(), Ask, 0, Yellow);
            }
                            
        } 
      }
      oc = orderCount(type); 
   }
}
double profitCount()
{
    double  oc = 0;
    for(int cnt = 0 ;cnt<OrdersTotal();cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);

      if(OrderMagicNumber() == getMagic()) 
      {
         oc+= OrderProfit()+OrderSwap()+OrderCommission();
      }
    } 
    return(oc);
}

