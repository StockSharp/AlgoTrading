/***

Project name: AMA TRADER
Coder: Kalenzo
Link: http://www.fxservice.eu
Author: Josef Strauss		

***/

#property copyright "Kalenzo, bartlomiej.gorski@gmail.com"
#property link      "http://www.fxservice.eu"
#include <stdlib.mqh>

#define MODE_LONG 1
#define MODE_SHORT -1
#define MODE_NONE 0

extern int       periodAMA=9;
extern int       nfast=2;
extern int       nslow=30;
extern double    G=2.0;
extern double    dK=2.0; 

extern int 	magic = 123456,	
		 	   SL = 50,
			   TP = 100,
			   trailing = 30;
			   
extern bool    AccountIsMini = false;      // Change to true if trading mini account
extern bool    MoneyManagement = true; // Change to false to shutdown money management controls.
                                       // Lots = 1 will be in effect and only 1 lot will be open regardless of equity.
extern double  TradeSizePercent = 5;  // Change to whatever percent of equity you wish to risk.
extern double  Lots = 0.1;             // standard lot size, 0.1 for mini, if mini = false and you will put 0.1 here ea will trade 1 lot 
extern double  PartialClose = 70;//in %
extern double  MaxLots = 100;

extern string Note0 = "Heken Ashi Setup";
extern int MaMetod  = 2;
extern int MaPeriod = 6;
extern int MaMetod2  = 3;
extern int MaPeriod2 = 2;
extern string Note1 = "RSI Setup";
extern int RsiPeriod = 14;
extern int RsiPrice = 0;
extern bool debug = true;

bool upSignal = false, 
     dnSignal = false;
     
double stoploss, takeprofit;     

string ver = "AMA TRADER - v2";
int Slippage = 3;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   Print(ver);
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


 

   upSignal = getSignal(MODE_LONG);
	dnSignal = getSignal(MODE_SHORT);
	
		if(upSignal && isOrderAllowed())
		{
			if(debug)
			Print("OPENING FIRST BUY");
         
			openOrder(MODE_LONG);
		}
      
		if(dnSignal && isOrderAllowed())
		{
			if(debug)
			Print("OPENING FIRST SELL");
         
			openOrder(MODE_SHORT);
		}  
	
   int total = OrdersTotal();
   for(int cnt = 0 ;cnt<=total;cnt++)
   {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
     
     if(OrderMagicNumber() == getMagic()) 
     {
		
       
		if(OrderType() == OP_BUY)
		{			
		    //move traling 
			if((Ask > OrderOpenPrice() + (trailing*Point) ) && ( OrderStopLoss() <   Bid - (trailing*Point)   ) && trailing >0)
			{
				if(debug)
				Print ("Traling Stop ",OrderStopLoss()," NS ",(Bid - (trailing*Point)));
				moveSL(OrderTicket(),Bid - trailing*Point);  
			}
			
			if(getPartialCloseExitSignal(MODE_LONG))
			{  Print("PC_L");
			   closeOrder(OrderTicket(),PartialClose);   
			  // return(0);
			}
			
			if(dnSignal || getExitSignal(MODE_LONG))
			{
			     closeOrder(OrderTicket());  
			}    
			
	 	}
		else if(OrderType() == OP_SELL)
		{
			//move traling  
			if( (Ask < OrderOpenPrice() - (trailing*Point) )&& (OrderStopLoss() >  Bid + (trailing*Point)  )&& trailing >0 )
			{
				if(debug)
				Print("MOVING SL OP SELL");
				moveSL(OrderTicket(),Bid + trailing*Point);
			}		
			
			if(getPartialCloseExitSignal(MODE_SHORT))
			{Print("PC_S");
			   closeOrder(OrderTicket(),PartialClose);   
			  // return(0);
			}
			
			
			if(upSignal || getExitSignal(MODE_SHORT))
			{
			    closeOrder(OrderTicket());
			} 
		}
	}
	}
//----
   return(0);
  }
//+------------------------------------------------------------------+
bool getPartialCloseExitSignal(int mode)
{
   double prsi = iRSI(NULL, 0, RsiPeriod, RsiPrice, 2);
   double rsi = iRSI(NULL, 0, RsiPeriod, RsiPrice, 1);
   
   if(mode == MODE_LONG && rsi > 70 && prsi <=70) return(true); 
   if(mode == MODE_SHORT && rsi < 30 && prsi >=30) return(true); 
   
   return(false);

}
bool getExitSignal(int mode)
{
   double prsi = iRSI(NULL, 0, RsiPeriod, RsiPrice, 2);
   double rsi = iRSI(NULL, 0, RsiPeriod, RsiPrice, 1);
   
   if(mode == MODE_LONG && rsi > 70 && prsi <= 70) return(true); 
   if(mode == MODE_SHORT && rsi < 30 && prsi >= 30) return(true); 
   
   return(false);

}
bool getSignal(int mode)
{

   double prsi = iRSI(NULL, 0, RsiPeriod, RsiPrice, 2);
   double rsi = iRSI(NULL, 0, RsiPeriod, RsiPrice, 1);

   double h0 = iCustom(NULL,0,"Heiken_Ashi_Smoothed",MaMetod, MaPeriod, MaMetod2, MaPeriod2,0,1); 
   double h1 = iCustom(NULL,0,"Heiken_Ashi_Smoothed",MaMetod, MaPeriod, MaMetod2, MaPeriod2,1,1); 

   bool filterShort = mode == MODE_SHORT && h0>h1 && rsi >= 30 && prsi<rsi;
   bool filterLong  = mode == MODE_LONG && h0<h1 && rsi <= 70 && prsi>rsi;
   
   double ama1 = iCustom(Symbol(),0,"AMA",periodAMA,nfast,nslow,G,dK,1,1);//blue
   double ama2 = iCustom(Symbol(),0,"AMA",periodAMA,nfast,nslow,G,dK,2,1);//gold

      if(mode == MODE_LONG && ama1!=0 && filterLong)
      return(true);
   
      if(mode == MODE_SHORT && ama2!=0 && filterShort)
      return(true);
   
   return(0);	
}
//+------------------------------------------------------------------+
double GetLots()
{
   double lot;
   
   if(MoneyManagement)
   {
     lot = LotsOptimized();
   }
   else
   {
     lot = Lots;
   }
   
   if(AccountIsMini)
   {
     if (lot < 0.1) lot = 0.1;
   }
   else
   {
     if (lot >= 1.0) lot = MathFloor(lot); else lot = 1.0;
   }
   if (lot > MaxLots) lot = MaxLots;
   
   return(lot);
}

double LotsOptimized()
{
   double lot=Lots;
   
   lot=NormalizeDouble(MathFloor(AccountFreeMargin()*TradeSizePercent/10000)/10,1);
  
   if(AccountIsMini)
   {
     lot = MathFloor(lot*10)/10;
    
   }
   return(lot);

}

bool isCloseAllowed()
{
    int totalH = OrdersHistoryTotal();
    
    for(int cntH = 0 ; cntH<=totalH; cntH++)
    {
		OrderSelect(cntH, SELECT_BY_POS, MODE_HISTORY);

		if(OrderMagicNumber() == getMagic()) 
		{
			int orderCShiftH = iBarShift(Symbol(),0,OrderCloseTime(),false);
			
			if(orderCShiftH == 0)
			return(false);	
			
		}	
    }
	return(true);
}

bool closeOrder(int ticket,double PartialClose = 0)
{
   
   if(!IsTradeAllowed() || !isCloseAllowed())
   return (false);
   
   int MAXRETRIES = 5;
   int retries = 0;
   OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES);
   
   double lots = OrderLots();
   
   if(PartialClose>0)
   {
      double tmpLots;
      double lotStep = MarketInfo(Symbol(),MODE_LOTSTEP);
      
      if(lotStep == 0.01)
      tmpLots = NormalizeDouble(lots*(PartialClose/100),2);
      else
      tmpLots = NormalizeDouble(lots*(PartialClose/100),1);
      
      lots = tmpLots;
      
      if(tmpLots<lotStep) lots = lotStep;
   }
   
   int error;
   if(OrderType() == OP_BUY)
   {
		while(!OrderClose(ticket, lots, Bid, Slippage, Red))
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
        while(!OrderClose(ticket, lots, Ask, Slippage, Red))
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
 

int getMagic() 
{
   return(magic);
}

bool openOrder(int type )
  {
   if(!IsTradeAllowed())
   {
      return (-1);
   }
   
  int error = 0;
  int ticket = 0;
  
   if( type == MODE_SHORT )
   {
         
      while(true)
      {
         
        
        if(MathAbs(Bid-stoploss)/Point < MarketInfo(Symbol(),MODE_STOPLEVEL)) 
        {
         Print("SL too close");
         return(0);
        }
        
        if(TP>0) takeprofit = Bid - TP*Point;
		
		if(SL>0) stoploss = Ask + SL*Point;		
		
        ticket = OrderSend(Symbol(),OP_SELL,GetLots(),Bid,Slippage,stoploss, takeprofit,ver,getMagic(),0,Green);
                 
        if(ticket<=0)
        {
            error=GetLastError();
            
            if(error>1)
            Print("SELL ORDER ERROR:", ErrorDescription(error)," BID ",Bid," SL ",stoploss," TP ",takeprofit);
            
      
            
          if(error==134) break;            // not enough money
          if(error==135) RefreshRates();   // prices changed
          break;
        }
        else { 
        
       
        OrderPrint(); 
        break; 
        
        }
        //---- 1 second wait
        Sleep(1000);
      } 
   }
   else if( type == MODE_LONG )
   {
      while(true)
      {
         
        
        if(MathAbs(Ask-stoploss)/Point < MarketInfo(Symbol(),MODE_STOPLEVEL)) 
        {
         Print("SL too close");
         return(0);
        }
        
		if(TP>0) takeprofit = Ask + TP*Point; 
		if(SL>0) stoploss = Bid - SL*Point;		
		
        ticket = OrderSend(Symbol(),OP_BUY,GetLots(),Ask,Slippage,stoploss,takeprofit,ver,getMagic(),0,Green);
        
        if(ticket<=0)
        {
            error=GetLastError();
            
            if(error>1)
            Print("BUY ORDER ERROR:", ErrorDescription(error)," Ask ",Ask," SL ",stoploss, " TP ",takeprofit);
            
      
            
          if(error==134) break;            // not enough money
          if(error==135) RefreshRates();   // prices changed
          break;
        }
        else { 
         
       
         
         OrderPrint(); 
         break; 
         
         }
        //---- 1 second wait
        Sleep(1000);
      }
   }
    
   return (true);
  }
  
bool isOrderAllowed()
{
	//if allowed = return true

	bool allowed = true;

    int total = OrdersTotal();
    for(int cnt = 0 ;cnt<=total;cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  
      int ibsc = iBarShift( Symbol(), 0, OrderOpenTime()) ;
		
		if(OrderMagicNumber() ==  getMagic() && ibsc == 0)
		{	
			allowed = false;
		}
    } 
	
	int Htotal = HistoryTotal();
    for(int Hcnt = 0 ;Hcnt<=Htotal;Hcnt++)
    {
		OrderSelect(Hcnt, SELECT_BY_POS, MODE_HISTORY);
		
		int ibs = iBarShift( Symbol(), 0, OrderOpenTime()) ;
		
		if(OrderMagicNumber() ==  getMagic() && ibs == 0)
		{	
			allowed = false;
		}
    } 
	
    return(allowed); 
}