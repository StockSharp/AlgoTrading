//+------------------------------------------------------------------+
//|                                                     FX-CHAOS.mq4 |
//|                                      http://www.fx-chaos.by.ru// |
//|                                                      version 1.0 |
//+------------------------------------------------------------------+

#property copyright "www.fx-chaos.by.ru"
#property link      "http://www.fx-chaos.by.ru"

#define MIN_STOPLOSS_POINT 10 
#define MIN_TAKEPROFIT_POINT 10 
#define MAGIC 0

double TrailingStopPoint;
extern double TakeProfit=13;

 string sNameExpert = "FX-CHAOS";
 int nAccount =0;
 int nSlippage = 0;
 bool lFlagUseHourTrade = False;
 int nFromHourTrade = False;
 int nToHourTrade = False;
 bool lFlagUseSound = False;
 string sSoundFileName = "alert.wav";
 color colorOpenBuy = Blue;
 color colorCloseBuy = Aqua;
 color colorOpenSell = Red;
 color colorCloseSell = Aqua;
 int numorder;

  double dbStopLoss;
 double dsStopLoss;
 double dbTakeProfit;
 double dsTakeProfit;
int DELTA=3000;
int MAX_Lots;
extern double dLots=0.01;





/////////////////////
void deinit() {
   Comment("");
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start(){

for ( int z=0;z<HistoryTotal();z++)
{
if (OrderSelect (z,SELECT_BY_POS, MODE_HISTORY))
datetime oct=OrderCloseTime();
}
if(CurTime()-oct<3600) return(0);





   
 
  // Сколько позиций у нас открыто?
   int total = OrdersTotal();

///////////////

 dbTakeProfit=Ask+TakeProfit*Point;
 dsTakeProfit=Bid-TakeProfit*Point;
 




   if (lFlagUseHourTrade){
      if (!(Hour()>=nFromHourTrade && Hour()<=nToHourTrade)) {
         Comment("Time for trade has not come else!");
         return(0);
      }
   }
   
   if(Bars < 100){
      Print("bars less than 100");
      return(0);
   }
   
   if (nAccount > 0 && nAccount != AccountNumber()){
      Comment("Trade on account :"+AccountNumber()+" FORBIDDEN!");
      return(0);
   }

   double ZZF1440=iCustom(NULL, 1440, "zzf",0,0);
      double   CLOSE =  iClose(NULL,0,0);
      double   OPEN =  iOpen(NULL,0,0); 
      
   double ZZF60=iCustom(NULL, 60, "zzf",0,2);
     double ZZF61=iCustom(NULL, 60, "FX-AO",0,0);


     double   HIGH1 =  iHigh(NULL,60,1)+2*Point+MarketInfo(Symbol(),MODE_SPREAD);
     double   LOW1 =  iLow(NULL,60,1)-2*Point;
      
  
  
      
   bool lFlagBuyOpen = false, lFlagSellOpen = false, lFlagBuyClose = false, lFlagSellClose = false, lFlagBuyOpen2 = false, lFlagSellOpen2 = false, lFlagBuyOpen3 = false, lFlagSellOpen3 = false, lFlagBuyOpen4 = false, lFlagSellOpen4 = false;

    
lFlagBuyOpen =(OPEN<HIGH1 && CLOSE>HIGH1 && CLOSE<ZZF60 && ZZF61<0 && CLOSE>ZZF1440);

  lFlagSellOpen = (OPEN>LOW1 && CLOSE<LOW1 && CLOSE>ZZF60 && ZZF61>0 && CLOSE<ZZF1440); 


///////////////////////////////////////////первый ордер

if((lFlagBuyOpen || lFlagSellOpen) && (!GlobalVariableCheck("") || GlobalVariableGet("Symbol()"+"")!=Time[0] && total<1))
     {
      if(lFlagBuyOpen)
        {  
 numorder = OrderSend(Symbol(), OP_BUY, dLots, Ask, nSlippage, dbStopLoss, dbTakeProfit, sNameExpert, MAGIC, 0, colorOpenBuy); 
       
         
           if(numorder>0)
           {
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening BUY order : ",GetLastError());return(0); 
        }
               
      if(lFlagSellOpen)
        { 
 numorder = OrderSend(Symbol(),OP_SELL, dLots, Bid, nSlippage, dsStopLoss, dsTakeProfit, sNameExpert, MAGIC, 0, colorOpenSell); 
        
     
          if(numorder>0)
           { 
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening SELL order : ",GetLastError());return(0);
        }
   }

 

   


///////////////////////////////////   
   
//закрытие ордера/////////////

    lFlagBuyClose =false;
   
  lFlagSellClose =false;

   
/////////////////////////////   
     
   
  
   if (ExistPositions()){
      if(OrderType()==OP_BUY){
         if (lFlagBuyClose){
            bool flagCloseBuy = OrderClose(OrderTicket(), OrderLots(), Bid, nSlippage, colorCloseBuy); 
            if (flagCloseBuy && lFlagUseSound) 
               PlaySound(sSoundFileName); 
            return(0);
         }
      }
      if(OrderType()==OP_SELL){
         if (lFlagSellClose){
            bool flagCloseSell = OrderClose(OrderTicket(), OrderLots(), Ask, nSlippage, colorCloseSell); 
            if (flagCloseSell && lFlagUseSound) 
               PlaySound(sSoundFileName); 
            return(0);
         }
      }
   }

   
   if (TrailingStopPoint > 0 ){
      
      for (int i=0; i<OrdersTotal(); i++) { 
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) { 
            bool lMagic = true;
            if (MAGIC > 0 && OrderMagicNumber() != MAGIC)
               lMagic = false;
            
            if (OrderSymbol()==Symbol() && lMagic) { 
               if (OrderType()==OP_BUY && TrailingStopPoint > 0) { 
                  if (Bid-OrderOpenPrice() > TrailingStopPoint*Point) { 
                     if (OrderStopLoss()<Bid-TrailingStopPoint*Point) 
                        ModifyStopLoss(Bid-TrailingStopPoint*Point); 
                  } 
               } 
               if (OrderType()==OP_SELL) { 
                  if (OrderOpenPrice()-Ask>TrailingStopPoint*Point) { 
                     if (OrderStopLoss()>Ask+TrailingStopPoint*Point || OrderStopLoss()==0)  
                        ModifyStopLoss(Ask+TrailingStopPoint*Point); 
                  } 
               } 
            } 
         } 
      } 
   }
   return (0);
}

                 
    
////////////////////////

bool ExistPositions() {
	for (int i=0; i<OrdersTotal(); i++) {
		if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
         bool lMagic = true;
         
         if (MAGIC > 0 && OrderMagicNumber() != MAGIC)
            lMagic = false;

			if (OrderSymbol()==Symbol() && lMagic) {
				return(True);
			}
		} 
	} 
	return(false);
}




void ModifyStopLoss(double ldStopLoss) { 
   bool lFlagModify = OrderModify(OrderTicket(), OrderOpenPrice(), ldStopLoss, OrderTakeProfit(), 0, CLR_NONE); 
   if (lFlagModify && lFlagUseSound) 
      PlaySound(sSoundFileName); 
} 


