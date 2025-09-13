//+------------------------------------------------------------------+
//|                                                 MTF  rsi_sar.mq4 |
//|                       Copyright © 2011, StarLimit Software Corp. |
//|                                            starlimit03@yahoo.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, StarLimit Software Corp."
#property link      "starlimit03@yahoo.com"
//+-------------------------------------------------
//----   AS AT 17TH MAY 2011

extern bool Long_Short= true,Long_Only=false,Short_Only=false;
extern bool Auto_Disable_EA=true;
extern bool use_daily_target=false;
extern double TakeProfit = 130;
extern bool use_bb=true,use_rsi = true,use_sar=true;
extern int rsi_per=14;
extern int PRICE_TYPE=1;
extern double step=0.02,maximum=0.2;
extern double Lots = 0.01;
extern double InitialStop = 0;
extern double TrailingStop = 100;
extern int MaxTrades = 15;
extern int Pips = 100;
extern int SecureProfit = 10;
int AccountProtection = 1;
extern int OrderstoProtect = 6;
int ReverseCondition = 0;
extern double EURUSDPipValue = 10;
extern double GBPUSDPipValue = 10;
extern double USDCHFPipValue = 10;
extern double USDJPYPipValue = 9.715;
int magic = 222;
extern int       bb_period=20;
extern int       bb_deviation=2;
extern int       bb_shift=0;
extern int StartHour = 10;
extern int EndHour = 17;
int mm = 0;
int risk = 12;
int AccountisNormal = 0;
//----
int  OpenOrders = 0, cnt = 0;
int  slippage = 50;
double sl = 0, tp = 0;
double BuyPrice = 0, SellPrice = 0;
double lotsi = 0, mylotsi = 0;
int mode = 0, myOrderType = 0;
bool ContinueOpening = True;
double LastPrice = 0;
int  PreviousOpenOrders = 0;
double Profit = 0;
int LastTicket = 0, LastType = 0,res;
double LastClosePrice = 0, LastLots = 0;
double Pivot = 0;
double PipValue = 0;
string text = "", text2 = "";
int ActiveOrders=0;
double AllTP=0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----  To assign a unique Magic number for each Pair.
 MathSrand(MathCeil(Ask*100));
 magic= MathRand();

   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//---- 
 
		         lotsi = Lots; 
//----
   if(lotsi > 100)
       lotsi = 100; 

   
   OpenOrders = 0;
//----
   for(cnt = 0; cnt < OrdersTotal(); cnt++)   
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	      //----
       if(OrderSymbol() == Symbol())
	  	       OpenOrders++;
     }       
//----
   if(OpenOrders < 1) 
     {
               ActiveOrders=0;              
              
	      if(TimeHour(TimeCurrent()) < StartHour) 
	          return(0);  
	      //----
       if(TimeHour(TimeCurrent())  > EndHour) 
	          return(0); 
	          
     }
//----
   if(Symbol() == "EURUSD") 
       PipValue = EURUSDPipValue; 
//----
   if(Symbol() == "GBPUSD") 
       PipValue = GBPUSDPipValue; 
//----
   if(Symbol() == "USDJPY") 
       PipValue = USDJPYPipValue; 
//----
   if(Symbol() == "USDCHF") 
       PipValue = USDCHFPipValue; 
//----
   if(PipValue == 0) 
     { 
       PipValue = 5; 
     }
//----
   if(PreviousOpenOrders > OpenOrders) 
	      for(cnt = OrdersTotal(); cnt >= 0; cnt--)
	        {
	          OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  	       mode = OrderType();
		         //----
           if(OrderSymbol() == Symbol()) 
		           {
			            if(mode == OP_BUY) 
			                OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), slippage, Green); 
			            //----
               if(mode == OP_SELL) 
			                OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), slippage, Green); 
			            return(0);
		           }
	        }
   PreviousOpenOrders = OpenOrders;
//----
   if(OpenOrders >= MaxTrades) 
	      ContinueOpening = False;
   else 
	      ContinueOpening = True;
//----
   if(LastPrice == 0) 
	      for(cnt = 0; cnt < OrdersTotal(); cnt++)
	        {	
	          OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
		         mode = OrderType();	
		         //----
           if(OrderSymbol() == Symbol()) 
		           {
			            LastPrice = OrderOpenPrice();
			            //----
               if(mode == OP_BUY) 
			                myOrderType = 2; 
			            //----
               if(mode == OP_SELL) 
			                myOrderType = 1;	
		           }
	        }
//----
if(OpenOrders < 1) 
  {
      myOrderType = 3;
      //----
	
  double upBB=iBands(Symbol(),5,bb_period,bb_deviation,0,PRICE_OPEN,MODE_UPPER,bb_shift);
  double loBB=iBands(Symbol(),5,bb_period,bb_deviation,0,PRICE_OPEN,MODE_LOWER,bb_shift);
  
     int bar1=iBars(Symbol(),5);
     int bar2=iBars(Symbol(),15);
     int bar3=iBars(Symbol(),30);
     int bar4=iBars(Symbol(),60);
         
         datetime time=iTime(Symbol(),5,0);
         int shift1=iBarShift(Symbol(),5,time,true);
         int shift2=iBarShift(Symbol(),15,time,true);
         int shift3=iBarShift(Symbol(),30,time,true);
         int shift4=iBarShift(Symbol(),60,time,true);
     
   double   RSI1 =iRSI(Symbol(),5,rsi_per,PRICE_TYPE,shift1);
   double   RSI2 =iRSI(Symbol(),15,rsi_per,PRICE_TYPE,shift2);
   double   RSI3 =iRSI(Symbol(),30,rsi_per,PRICE_TYPE,shift3);
   double   RSI4 =iRSI(Symbol(),60,rsi_per,PRICE_TYPE,shift4);
   
   
    double  mabela = iSAR(Symbol(), 5,step,maximum,shift1); 
    double  mabelb = iSAR(Symbol(), 15,step,maximum,shift2); 
    double   mabelc = iSAR(Symbol(), 30,step,maximum,shift3); 
   
  //use all three signals 
  if(use_rsi && use_bb && use_sar)                 // use all three signals
  {
   if(RSI1>50 && RSI2>50 && RSI3>50 && RSI4>50 && High[bb_shift]>=upBB && mabela< Low[0] && mabelb < Low[0] && mabelc < Low[0] ) myOrderType = 2;
   if(RSI1<50 && RSI2<50 && RSI3<50 && RSI4<50 && Low[bb_shift]<=loBB  && mabela> High[0]&& mabelb > High[0]&& mabelc > High[0]) myOrderType = 1;
  }
  if(!use_rsi && use_bb && use_sar)                // use both bba and sar
  {
   if( High[bb_shift]>=upBB && mabela< Low[0] && mabelb < Low[0] && mabelc < Low[0] ) myOrderType = 2;
   if( Low[bb_shift]<=loBB  && mabela> High[0]&& mabelb > High[0]&& mabelc > High[0]) myOrderType = 1;
  }
  if(use_rsi && !use_bb && use_sar)                // use both rsi and sar
  {
   if(RSI1>50 && RSI2>50 && RSI3>50 && RSI4>50 && mabela< Low[0] && mabelb < Low[0] && mabelc < Low[0] ) myOrderType = 2;
   if(RSI1<50 && RSI2<50 && RSI3<50 && RSI4<50 && mabela> High[0]&& mabelb > High[0]&& mabelc > High[0]) myOrderType = 1;
  } 
  if(use_rsi && use_bb && !use_sar)                // use both rsi and bb
  {
   if(RSI1>50 && RSI2>50 && RSI3>50 && RSI4>50 && High[bb_shift]>=upBB  ) myOrderType = 2;
   if(RSI1<50 && RSI2<50 && RSI3<50 && RSI4<50 && Low[bb_shift]<=loBB   ) myOrderType = 1;
  } 	      
  else if(use_rsi && !use_bb && !use_sar)          // use only rsi
  {
   if(RSI1>50 && RSI2>50 && RSI3>50 && RSI4>50) myOrderType = 2;
   if(RSI1<50 && RSI2<50 && RSI3<50 && RSI4<50) myOrderType = 1;
  }
  else if(!use_rsi && use_bb &&  !use_sar)         // use only bb
  {
   if(High[bb_shift]>=upBB )myOrderType = 2; // return(buy);
   if(Low[bb_shift]<=loBB ) myOrderType = 1;// return(sell);
  }
  else if(!use_rsi && !use_bb && use_sar)          // use only sar
  {
  if( mabela< Low[0] && mabelb < Low[0] && mabelc < Low[0] ) myOrderType = 2;
  if( mabela> High[0]&& mabelb > High[0]&& mabelc > High[0]) myOrderType = 1;
  }
  
 } // end of if...
 
  
   Profit = 0;
   LastTicket = 0;
   LastType = 0;
	  LastClosePrice = 0;
	  LastLots = 0;	
//----
	  for(cnt = 0; cnt < OrdersTotal(); cnt++)
	    {
	      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	      //----
       if(OrderSymbol() == Symbol()) 
	        {			
	  	       LastTicket = OrderTicket();
			        //----
           if(OrderType() == OP_BUY) 
			            LastType = OP_BUY; 
			        //----
           if(OrderType() == OP_SELL) 
			            LastType = OP_SELL; 
			        LastClosePrice = OrderClosePrice();
			        LastLots = OrderLots();
			        //----
           if(LastType == OP_BUY) 
			          {			
				           if(OrderClosePrice() < OrderOpenPrice()) 
					              Profit = Profit - (OrderOpenPrice() - OrderClosePrice())*OrderLots() / 
					                       Point; 
				           //----
               if(OrderClosePrice() > OrderOpenPrice()) 
					              Profit = Profit + (OrderClosePrice() - OrderOpenPrice())*OrderLots() / 
					                       Point; 
			          }
			        //----
           if(LastType==OP_SELL) 
			          {
				           if(OrderClosePrice() > OrderOpenPrice()) 
					              Profit = Profit - (OrderClosePrice() - OrderOpenPrice())*OrderLots() / 
					                       Point; 
				           //----
               if(OrderClosePrice() < OrderOpenPrice()) 
					              Profit = Profit + (OrderOpenPrice() - OrderClosePrice())*OrderLots() / 
					                       Point;
			          }
	        }
     }
	  Profit = Profit*PipValue;
	  text2 = "Profit: $" + DoubleToStr(Profit,2) + " +/-";
//----
 //  if(OpenOrders >= (MaxTrades - OrderstoProtect) && AccountProtection == 1) 
	      if(Profit >= SecureProfit) 
	        {
	          OrderClose(LastTicket, LastLots, LastClosePrice, slippage, Yellow);		 
	          ContinueOpening = False;
	          return(0);
	        }

//----
   if(myOrderType == 1 && ContinueOpening && (Long_Short|| Short_Only)) 
     {	
       if((Bid - LastPrice) >= Pips*Point || OpenOrders < 1) 
         {		
           SellPrice = Bid;				
           LastPrice = 0;
           //----
           if(TakeProfit == 0) 
               tp = 0; 
           else 
               tp = SellPrice - TakeProfit*Point; 
           //----
           if(InitialStop == 0) 
               sl = 0; 
           else 
               sl = SellPrice + InitialStop*Point;  
           //----
           if(OpenOrders >=2) 
             {
               mylotsi = 2*mylotsi;			
               //----
		       } 
		         else 
		             mylotsi=lotsi; 
		         //----
        
		       res=OrderSend(Symbol(), OP_SELL, mylotsi, SellPrice, slippage, sl, tp,"MTF EA Sell",magic, 0, Red);
		     if (res < 0)
         mylotsi=mylotsi/2;	    		    // i.e if opening of order failed.
          else 
           {
               ActiveOrders++;              // increament open orders.
              if(ActiveOrders==1) AllTP=tp;
            else
               ModifyTP(tp);
           
           }
		         return(0);
	        }
     }     
   if(myOrderType == 2 && ContinueOpening&& (Long_Short|| Long_Only)) 
     {
       if((LastPrice-Ask) >= Pips*Point || OpenOrders < 1) 
         {		
           BuyPrice = Ask;
           LastPrice = 0;
           //----
           if(TakeProfit == 0) 
               tp = 0; 
           else 
               tp = BuyPrice + TakeProfit*Point; 
           //----
           if(InitialStop==0)  
               sl = 0; 
           else 
               sl = BuyPrice - InitialStop*Point; 
           //----
           if(OpenOrders >=2) 
             {
               mylotsi = 2*mylotsi;
             } 
           else 
               mylotsi = lotsi; 
           //----
         //  if(mylotsi > 100) 
         //      mylotsi = 100; 
         res=OrderSend(Symbol(), OP_BUY, mylotsi, BuyPrice, slippage, sl, tp, "MTF EA Buy", magic, 0,Blue);
         if (res < 0)
         mylotsi=mylotsi/2;	    		    // i.e if opening of order failed.
          else 
           {
               ActiveOrders++;              // increament open orders.
              if(ActiveOrders==1) AllTP=tp;
              else ModifyTP(tp);
         
           }
                  
            return(0);
         }
     }                                
//----                                                                
   return(0);                                                                                 
  }
  
 void ModifyTP(double tp) 
  {
   int cnt;
    // if we have opened positions we take care of them
   for(cnt =0;cnt <=OrdersTotal() ;  cnt++)
     {
       OrderSelect(cnt, SELECT_BY_POS,MODE_TRADES);
	      //----
       if(OrderSymbol() == Symbol() && OrderMagicNumber()==magic) 
	         {	
	  	       if(OrderType() == OP_SELL) 
	  	  	      if( OrderModify(OrderTicket(),OrderOpenPrice(), OrderStopLoss(),tp, 1000, Purple)) Comment("\n\n\n\n MODIFYING ORDER........");
	  					                //   return(0);	  					
	  		   }
	  	       if(OrderType() == OP_BUY)
	  	      {
	  		      if( OrderModify(OrderTicket(), OrderOpenPrice(),OrderStopLoss(), tp, 1000, Purple))Comment("\n\n\n\n MODIFYING ORDER........");
                                //   return(0);
				}
   	     
     } 
  }// 
 
  
double dailyprofit()
{
  int day=Day(); double res1=0,res2=0;
  
  for(int i=0; i<OrdersHistoryTotal(); i++)
  {   
      OrderSelect(i,SELECT_BY_POS,MODE_HISTORY);
      if(OrderMagicNumber()!=magic) continue;
      if(TimeDay(OrderOpenTime())==day) res1+=OrderProfit();
  }
  
 for(i=0; i<OrdersTotal(); i++)
  {
      OrderSelect(i,SELECT_BY_POS,MODE_TRADES);   
      if(OrderMagicNumber()!=magic) continue;
      if(TimeDay(OrderOpenTime())==day) res2+=OrderProfit();
  }
  
  return(res1+res2);
}
//+------------------------------------------------------------------+