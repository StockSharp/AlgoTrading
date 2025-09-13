//+------------------------------------------------------------------+
//|                                                     FX-CHAOS.mq4 |
//|                                      http://www.fx-chaos.by.ru// |
//|                                                      version 1.0 |
//+------------------------------------------------------------------+

#property copyright "www.fx-chaos.by.ru"
#property link      "http://www.fx-chaos.by.ru"

#define MIN_STOPLOSS_POINT 10 
#define MIN_TAKEPROFIT_POINT 10 
#define MAGIC 26626

double TrailingStopPoint;


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
 int numorder,k;
 
  double dbStopLoss;
 double dsStopLoss;
 double dbTakeProfit;
 double dsTakeProfit;
int DELTA=3000;
int MAX_Lots;
double dLots1=0.1;
double dLots2=0.5;
double dLots3=0.4;
double dLots4=0.3;
double dLots5=0.2;

static int prevtime = 0;

/////////////////////
void deinit() {
   Comment("");
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start(){

 int n = Bars;
   

for ( int z=0;z<HistoryTotal();z++)
{
if (OrderSelect (z,SELECT_BY_POS, MODE_HISTORY))
datetime oct=OrderCloseTime();
}
if(CurTime()-oct<14400) return(0);




 
  // Сколько позиций у нас открыто?
   int total = OrdersTotal();



 

//лотс//////////
 
 if (AccountBalance()<3000+1*DELTA)
   MAX_Lots = 3.0;
  if (AccountBalance()>=3000+1*DELTA && AccountBalance()<3000+2*DELTA)
   MAX_Lots = 4.5;
  if (AccountBalance()>=3000+2*DELTA && AccountBalance()<3000+3*DELTA)
   MAX_Lots = 6.0;
     if (AccountBalance()>=3000+3*DELTA && AccountBalance()<3000+4*DELTA)
   MAX_Lots = 7.5;
     if (AccountBalance()>=3000+4*DELTA && AccountBalance()<3000+5*DELTA)
   MAX_Lots = 9.0;
     if (AccountBalance()>=3000+5*DELTA && AccountBalance()<3000+6*DELTA)
   MAX_Lots = 10.5;
     if (AccountBalance()>=3000+6*DELTA && AccountBalance()<3000+7*DELTA)
   MAX_Lots = 12.0;
     if (AccountBalance()>=3000+7*DELTA && AccountBalance()<3000+8*DELTA)
   MAX_Lots = 13.5;
     if (AccountBalance()>=3000+8*DELTA && AccountBalance()<3000+9*DELTA)
   MAX_Lots = 15.0;
   if (AccountBalance()>=3000+9*DELTA)
   MAX_Lots = 15.0;
   
   
 if (MAX_Lots >1 && MAX_Lots <2 ){
   
   dLots1 = 0.1;
   dLots2 = 0.5;
   dLots3 = 0.4;
   dLots4 = 0.3;
   dLots5 = 0.2;
}

 if (MAX_Lots >2 && MAX_Lots <4){
   
   dLots1 = 0.2;
   dLots2 = 1.0;
   dLots3 = 0.8;
   dLots4 = 0.6;
   dLots5 = 0.4;
}

 if (MAX_Lots >4 && MAX_Lots <5){
   
   dLots1 = 0.3;
   dLots2 = 1.5;
   dLots3 = 1.2;
   dLots4 = 0.9;
   dLots5 = 0.6;
}

 if (MAX_Lots >5 && MAX_Lots <7){
   
   dLots1 = 0.4;
   dLots2 = 2.0;
   dLots3 = 1.6;
   dLots4 = 1.2;
   dLots5 = 0.8;
}

 if (MAX_Lots >7 && MAX_Lots <8){
   
   dLots1 = 0.5;
   dLots2 = 2.5;
   dLots3 = 2.0;
   dLots4 = 1.5;
   dLots5 = 1.0;
}

 if (MAX_Lots >8 && MAX_Lots <10){
   
   dLots1 = 0.6;
   dLots2 = 3.0;
   dLots3 = 2.4;
   dLots4 = 1.8;
   dLots5 = 1.2;
}

 if (MAX_Lots >10 && MAX_Lots <11){
   
   dLots1 = 0.7;
   dLots2 = 3.5;
   dLots3 = 2.8;
   dLots4 = 2.1;
   dLots5 = 1.4;
}

 if (MAX_Lots >11 && MAX_Lots <13){
   
   dLots1 = 0.8;
   dLots2 = 4.0;
   dLots3 = 3.2;
   dLots4 = 2.4;
   dLots5 = 1.6;
}

 if (MAX_Lots >13 && MAX_Lots <14){
   
   dLots1 = 0.9;
   dLots2 = 4.5;
   dLots3 = 3.6;
   dLots4 = 2.7;
   dLots5 = 1.8;
}


 if (MAX_Lots >14 && MAX_Lots <16){
   
   dLots1 = 1.0;
   dLots2 = 5.0;
   dLots3 = 4.0;
   dLots4 = 3.0;
   dLots5 = 2.0;
}


///////////////


 




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
 
   double ZZF1440=iCustom(NULL, 1440, "zzf",0,1);
   
      double FXAO=iCustom(NULL, 240, "FX-AO",0,0);
   
      double   CLOSE =  iClose(NULL,0,0);
      double   OPEN =  iOpen(NULL,240,0); 
    double   OPEN0 =  iOpen(NULL,1440,0); 
   
      
   double ZZF240=iCustom(NULL, 240, "zzf",0,1);
  

     double   HIGH0 =  iHigh(NULL,1440,1)+2*Point+MarketInfo(Symbol(),MODE_SPREAD);
     double   LOW0 =  iLow(NULL,1440,1)-2*Point;
     
     double   HIGH1 =  iHigh(NULL,240,1)+2*Point+MarketInfo(Symbol(),MODE_SPREAD);
     double   LOW1 =  iLow(NULL,240,1)-2*Point;
     

      
        
   bool lFlagBuyOpen = false, lFlagSellOpen = false, lFlagBuyClose = false, lFlagSellClose = false, lFlagBuyOpen2 = false, lFlagSellOpen2 = false, lFlagBuyOpen3 = false, lFlagSellOpen3 = false, lFlagBuyOpen4 = false, lFlagSellOpen4 = false;

lFlagBuyOpen =(OPEN0<HIGH0 && CLOSE>HIGH0 && CLOSE<ZZF1440 && FXAO<0);

  lFlagSellOpen = (OPEN0>LOW0 && CLOSE<LOW0 && CLOSE>ZZF1440 && FXAO>0);

lFlagBuyOpen2 =(OPEN<HIGH1 && CLOSE>HIGH1 && CLOSE<ZZF240);

  lFlagSellOpen2 = (OPEN>LOW1 && CLOSE<LOW1 && CLOSE>ZZF240);






///////////////////////////////////////////первый ордер

if((lFlagBuyOpen || lFlagSellOpen) && (!GlobalVariableCheck("") || GlobalVariableGet("Symbol()"+"")!=Time[0] && total<1))
     {
      if(lFlagBuyOpen)
        {  
 numorder = OrderSend(Symbol(), OP_BUY, dLots1, Ask, nSlippage, dbStopLoss, dbTakeProfit, sNameExpert, MAGIC, 0, colorOpenBuy); 
           
         
           if(numorder>0)
           {
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening BUY order : ",GetLastError());return(0); 
        }
               
      if(lFlagSellOpen)
        { 
 numorder = OrderSend(Symbol(),OP_SELL, dLots1, Bid, nSlippage, dsStopLoss, dsTakeProfit, sNameExpert, MAGIC, 0, colorOpenSell); 
        
     
          if(numorder>0)
           { 
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening SELL order : ",GetLastError());return(0);
        }
   }

   
/////////////////////////////////////////второй ордер      
   
 
if((lFlagBuyOpen2 || lFlagSellOpen2) && (!GlobalVariableCheck("") || GlobalVariableGet("Symbol()"+"")!=Time[0] && total>0 && total<2&& AccountEquity()>AccountBalance()))//
     {
      if(lFlagBuyOpen2 && OrderType()==OP_BUY)
        {  
 numorder = OrderSend(Symbol(), OP_BUY, dLots2, Ask, nSlippage, dbStopLoss, dbTakeProfit, sNameExpert, MAGIC, 0, colorOpenBuy); 
           if(numorder>0)
           {
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening BUY order : ",GetLastError());return(0); 
        }
               
      if(lFlagSellOpen2 && OrderType()==OP_SELL)
        { 
 numorder = OrderSend(Symbol(),OP_SELL, dLots2, Bid, nSlippage, dsStopLoss, dsTakeProfit, sNameExpert, MAGIC, 0, colorOpenSell); 
          if(numorder>0)
           {
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening SELL order : ",GetLastError());return(0);
        }
   }

   
////////////////////////////////////////////третий ордер

 
if((lFlagBuyOpen2 || lFlagSellOpen2) && (!GlobalVariableCheck("") || GlobalVariableGet("Symbol()"+"")!=Time[0] && total>1 && total<3&& AccountEquity()>AccountBalance()))
     {
      if(lFlagBuyOpen2 && OrderType()==OP_BUY)
        {  
 numorder = OrderSend(Symbol(), OP_BUY, dLots3, Ask, nSlippage, dbStopLoss, dbTakeProfit, sNameExpert, MAGIC, 0, colorOpenBuy); 
           if(numorder>0)
           {
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening BUY order : ",GetLastError());return(0); 
        }
               
      if(lFlagSellOpen2 && OrderType()==OP_SELL)
        { 
 numorder = OrderSend(Symbol(),OP_SELL, dLots3, Bid, nSlippage, dsStopLoss, dsTakeProfit, sNameExpert, MAGIC, 0, colorOpenSell); 
          if(numorder>0)
           {
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening SELL order : ",GetLastError());return(0);
        }
   }


////////////////////////////////////////////4 ордер

 
if((lFlagBuyOpen2 || lFlagSellOpen2) && (!GlobalVariableCheck("") || GlobalVariableGet("Symbol()"+"")!=Time[0] && total>2 && total<4&& AccountEquity()>AccountBalance()))
     {
      if(lFlagBuyOpen2 && OrderType()==OP_BUY)
        {  
 numorder = OrderSend(Symbol(), OP_BUY, dLots4, Ask, nSlippage, dbStopLoss, dbTakeProfit, sNameExpert, MAGIC, 0, colorOpenBuy); 
           if(numorder>0)
           {
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening BUY order : ",GetLastError());return(0); 
        }
               
      if(lFlagSellOpen2 && OrderType()==OP_SELL)
        { 
 numorder = OrderSend(Symbol(),OP_SELL, dLots4, Bid, nSlippage, dsStopLoss, dsTakeProfit, sNameExpert, MAGIC, 0, colorOpenSell); 
          if(numorder>0)
           {
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening SELL order : ",GetLastError());return(0);
        }
   }


////////////////////////////////////////////5 ордер

 
if((lFlagBuyOpen2 || lFlagSellOpen2) && (!GlobalVariableCheck("") || GlobalVariableGet("Symbol()"+"")!=Time[0] && total>3 && total<5&& AccountEquity()>AccountBalance()))//&& AccountEquity()>AccountBalance()
     {
      if(lFlagBuyOpen2 && OrderType()==OP_BUY)
        {  
 numorder = OrderSend(Symbol(), OP_BUY, dLots5, Ask, nSlippage, dbStopLoss, dbTakeProfit, sNameExpert, MAGIC, 0, colorOpenBuy); 
           if(numorder>0)
           {
            if(OrderSelect(numorder,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
            GlobalVariableSet("Symbol()"+"MAGICMA"+"TimeBuySell",Time[0]);
           }
         else Print("Error opening BUY order : ",GetLastError());return(0); 
        }
               
      if(lFlagSellOpen2 && OrderType()==OP_SELL)
        { 
 numorder = OrderSend(Symbol(),OP_SELL, dLots5, Bid, nSlippage, dsStopLoss, dsTakeProfit, sNameExpert, MAGIC, 0, colorOpenSell); 
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

   
   if (total>1 && AccountEquity()>AccountBalance()+(3000*dLots1)){
      
      for (int i=0; i<OrdersTotal(); i++) { 
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) { 
            bool lMagic = true;
            if (MAGIC > 0 && OrderMagicNumber() != MAGIC)
               lMagic = false;
            
            if (OrderSymbol()==Symbol() && lMagic) { 
               if (OrderType()==OP_BUY && 27 > 0) { 
                 
                     if (OrderStopLoss()<Bid-27*Point || OrderStopLoss()==0) 
                        ModifyStopLoss(Bid-27*Point); 
                  
               } 
               if (OrderType()==OP_SELL) { 
              
                     if (OrderStopLoss()>Ask+27*Point || OrderStopLoss()==0)  
                        ModifyStopLoss(Ask+27*Point); 
                   
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


