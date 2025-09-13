//+------------------------------------------------------------------+
//|                                            up3x1_premium_v2M.mq4 |
//|                                  Copyright © 2006, Izhutov Pavel |
//|                        www.forexnow.narod.ru   izhutov@yandex.ru |
//+------------------------------------------------------------------+
#define MAGICMA  20050612

extern double Lots               = 0.05;
extern double MaximumRisk        = 0.1;
extern double DecreaseFactor     = 3;
extern double TakeProfit         = 150;
extern double StopLoss           = 100;
extern double TrailingStop       = 10;

double Points;

int init ()
  {
   Points = MarketInfo (Symbol(), MODE_POINT);

   return(0);
  }


void start()
  {

   if(Bars<100 || IsTradeAllowed()==false) return;
 


   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();

  }


int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;

   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }

   if(buys>0) return(buys);
   else       return(-sells);
  }

double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();    
   int    losses=0;                  
lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);


   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) lot=NormalizeDouble(lot-lot*losses/DecreaseFactor,1);
     }

   if(lot<0.1) lot=0.1;
   return(lot);
  }

void CheckForOpen()
  {

   double HI_1;
   double LO_1;
   double HI_2;
   double LO_2;
   double OP_1;
   double CL_1;
   double OP_2;
   double CL_2;
   double OPD;
   double CLD;   
   double ma24_2;
   double ma60_2;
   double ma24_1;
   double ma60_1;
   double OP_0;
   double CL_0;
   double sma;
   
   int    res;

   if(Volume[0]>1) return;

   HI_1=iHigh(NULL,0,1);
   LO_1=iLow(NULL,0,1);
   HI_2=iHigh(NULL,0,2);
   LO_2=iLow(NULL,0,2);
   OP_1=iOpen(NULL,0,1);
   CL_1=iClose(NULL,0,1);
   OP_2=iOpen(NULL,0,2);
   CL_2=iClose(NULL,0,2);
   OPD=iOpen(NULL,1440,1);
   CLD=iClose(NULL,1440,1);
   ma24_2=iMA(NULL,0,12,0,2,5,2);
   ma24_1=iMA(NULL,0,12,0,2,5,1);
   ma60_2=iMA(NULL,0,26,0,2,5,2);
   ma60_1=iMA(NULL,0,26,0,2,5,1);
   OP_0=iOpen(NULL,0,0);
   CL_0=iClose(NULL,0,0);
   sma = iMA(NULL,PERIOD_D1,10,0,MODE_SMA,PRICE_CLOSE,0);
   
   
//bay
if (((ma24_2<ma60_2 && ma60_1<ma24_1 && OP_2<OP_1) ||(HI_1-LO_1>0.0060 && OP_1<CL_1 && 
      CL_1-OP_1>0.0050))  || (Hour()==0 && (OPD>CLD && (OPD-CLD)>0.0060)) || (sma > Ask) || (sma < Ask) || (sma == Ask))

     {
     res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,Ask-StopLoss*Points,Ask+TakeProfit*Points,"",MAGICMA,0,Blue);
     return;
     }
//sell
if (((ma24_2>ma60_2 && ma60_1>ma24_1 && OP_2>OP_1) || (HI_1-LO_1>0.0060 && OP_1>CL_1 && 
      OP_1-CL_1>0.0050))  || (Hour()==0 && (OPD<CLD && (CLD-OPD)>0.0060)) )

     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,Bid+StopLoss*Points,Bid-TakeProfit*Points,"",MAGICMA,0,Red);
      return;
     }


  }

void CheckForClose()
  {
   double ma24_2;
   double ma60_2;
   double ma24_1;
   double ma60_1;
   if(Volume[0]>1) return;
   ma24_2=iMA(NULL,0,12,0,2,5,2);
   ma24_1=iMA(NULL,0,12,0,2,5,1);
   ma60_2=iMA(NULL,0,26,0,2,5,2);
   ma60_1=iMA(NULL,0,26,0,2,5,1);
   
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      
      if(OrderType()==OP_BUY)
        {
      if (ma24_1==ma60_1) OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
         break;
        }
if(TrailingStop>0)  
              {                
               if(Bid-OrderOpenPrice()>Points*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-Points*TrailingStop)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Points*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }        
        
        
      if(OrderType()==OP_SELL)
        {
      if (ma24_1==ma60_1) OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
   
   
         break;
        }
if(TrailingStop>0)  
              {              
               if((OrderOpenPrice()-Ask)>(Points*TrailingStop))
                 {
                  if(OrderStopLoss()==0.0 || 
                     OrderStopLoss()>(Ask+Points*TrailingStop))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Points*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }     
     }


  }  		


