//+------------------------------------------------------------------+
//|                                                   Robot Danu.mq4 |
//|                      danusaputra © 2012, Ultimate Explorer Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "danusaputra © 2012, Ultimate Explorer Corp."
#property link      "http://www.metaquotes.net"

#define MAGICMA  20050610

extern double Lots               = 0.5;
extern double MaximumRisk        = 0.02;
extern double DecreaseFactor     = 3;
extern double Period1            = 28;
extern double Period2            = 56;
extern double Period3            = 112;
extern string Dev_Step_1         ="3,9";
extern string Dev_Step_2         ="24,15";
extern string Dev_Step_3         ="63,36";
extern int Symbol_1_Kod          =140;
extern int Symbol_2_Kod          =141;
extern int Symbol_3_Kod          =142;

//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//---- return orders volume
   if(buys>0) return(buys);
   else       return(-sells);
  }
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);
//---- calcuulate number of losses orders without a break
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
//---- return lot size
   if(lot<10) lot=10;
   return(lot);
  }
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   double ZZ_1, ZZ_2;
   int    res,res1,res2,res3,res4;

//---- go trading only for first tiks of new bar
   if(Volume[0]>1) return;

//---- get 3 Level ZZ Semafor
   
   ZZ_1=iCustom(Symbol(),0,"3_Level_ZZ_Semafor",Period1,Period2,Period3,Dev_Step_1,Dev_Step_2,Dev_Step_3,Symbol_1_Kod,Symbol_2_Kod,Symbol_3_Kod,5,3);
   ZZ_2=iCustom(Symbol(),0,"3_Level_ZZ_Semafor",Period1,Period2,Period3,Dev_Step_1,Dev_Step_2,Dev_Step_3,Symbol_1_Kod,Symbol_2_Kod,Symbol_3_Kod,4,3);
   

//---- sell conditions
   
   if(ZZ_1 > ZZ_2 && OP_SELL <= 3)  
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,0,"Robot Danu",MAGICMA,0,Red);
      
      return;
     }
     
//---- buy conditions
   if(ZZ_1 < ZZ_2 && OP_BUY <= 3)  
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,0,"Robot Danu",MAGICMA,0,Blue);
      
      return;
     }

//----
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
   double ZZ_1, ZZ_2;
//---- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
//---- get Moving Average 
   ZZ_1=iCustom(Symbol(),0,"3_Level_ZZ_Semafor",Period1,Period2,Period3,Dev_Step_1,Dev_Step_2,Dev_Step_3,Symbol_1_Kod,Symbol_2_Kod,Symbol_3_Kod,5,3);
   ZZ_2=iCustom(Symbol(),0,"3_Level_ZZ_Semafor",Period1,Period2,Period3,Dev_Step_1,Dev_Step_2,Dev_Step_3,Symbol_1_Kod,Symbol_2_Kod,Symbol_3_Kod,4,3);
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //---- check order type 
      if(OrderType()==OP_BUY)
        {
         if(ZZ_1 > ZZ_2) OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
         break;
        }
            
      if(OrderType()==OP_SELL)
        {
         if(ZZ_1 < ZZ_2) OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
         break;
        }
        
     }
//----
  }
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
//---- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false) return;
//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//----
  }
//+------------------------------------------------------------------+