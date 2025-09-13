//+------------------------------------------------------------------+
//|                                                      AutoKdj.mq4 |
//|                                                        senlin ge |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "senlin ge"
#property link      "http://www.metaquotes.net"
#define MAGICKDJ    20080220
//---- input parameters
extern int whichmethod = 2;//1;//4;    //1: no S/L,no T/P ����ֹӮҲ����ֹ��
                                       //2: no S/L,has T/P ��ֹӮ��������ֹ��
                                       //3: has S/L,no T/P //����ֹӮ����ֹ��
                                       //4: has T/P has S/L,��ֹӮҲ��ֹ��

extern double    Lots=0.1;             //��ͱ�׼��
extern double    MaximumRisk=0.4;      //����ռ�����ʽ����
extern double    DecreaseFactor=0.3;   //���ֿ�������µ�����
extern  int      tp=200;               //���Ӯ�� ֹӮ���� 
extern  int      sl=100;               //�����ʧ ֹ�����
extern int       Leverage=100;         //���ױ���1:100
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

//---- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false) return;
//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//----


//----
   return(0);
  }
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
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICKDJ)
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

//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   //double ma;
   int    res;
   double point =MarketInfo(Symbol(),MODE_POINT);
 Print("point=",point);
//---- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
  
//---- get Moving Average 
   double valKDCCurrent=iCustom(NULL, 0, "kdj",5,0);   //��ǰһ���ڵ�KDCֵ 
   double valKDCPrevious=iCustom(NULL, 0, "kdj",5,1);  //ǰһ���ڵ�KDCֵ 
   double valKCurrent=iCustom(NULL, 0, "kdj",2,0);     //��ǰ���ڵ�Kֵ
   double valKPrevious=iCustom(NULL, 0, "kdj",2,1);    //ǰһ���ڵ�Kֵ
   double valDCurrent=iCustom(NULL, 0, "kdj",3,0);     //��ǰ���ڵ�Dֵ
   double valDPrevious=iCustom(NULL, 0, "kdj",3,1);    //ǰһ���ڵ�Dֵ 
   Print("valKCurrent=",valKCurrent);
   Print("valDCurrent=",valDCurrent);
   Print("valKPrevious=",valKPrevious);
   Print("valDPrevious=",valDPrevious);
//---- sell conditions
    if(valKDCPrevious>0 && valKDCCurrent<0|| (valKDCCurrent<0 && valKPrevious-valKCurrent>0))  //k�´�D K<d do short 
     {
      switch (whichmethod)
         {
         case 1:   res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,0,"���ղ�",MAGICKDJ,0,Red); break;
         case 2:   res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,Bid+sl*point,0,"���ղ�",MAGICKDJ,0,Red);     break;
         case 3:   res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,Bid-tp*point,"���ղ�",MAGICKDJ,0,Red); break;
         case 4:   res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,0,Bid+sl*point,Bid-tp*point,"���ղ�",MAGICKDJ,0,Red); break;
         default : res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,0,"",MAGICKDJ,0,Red); break;
          }
        if (res <=0)
          {
          int error=GetLastError();
          if(error==134) Print("Received 134 Error after OrderSend() !! ");         // not enough money
          if(error==135) RefreshRates();                                            // prices have changed
          if(error==131) Print("Received 131 Error after OrderSend() !! ");         // not enough money

         }
   //Sleep(5000);
      return;
     }
//---- buy conditions
   if(valKDCPrevious<0 && valKDCCurrent>0|| valKDCCurrent>0&& valKPrevious-valKCurrent<0)//k�ϴ�D  k>d ����
     {
      switch (whichmethod)
       {
         case 1:   res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,0,"�����",MAGICKDJ,0,Blue);break;
         case 2:   res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,Ask-sl*point,0,"�����",MAGICKDJ,0,Blue);   break;
         case 3:   res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,Ask+tp*point,"�����",MAGICKDJ,0,Blue);break;
         case 4:   res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,0,Ask-sl*point,Ask+tp*point,"�����",MAGICKDJ,0,Blue);break;
         default : res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,0,"�����",MAGICKDJ,0,Blue);break;
    }
       
       if (res <=0)
      {
       error=GetLastError();
       if(error==134) Print("Received 134 Error after OrderSend() !! ");         // not enough money
       if(error==135) RefreshRates();   // prices have changed
       }
       Sleep(5000);
      return;
     }
//----
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
   double ma;
//---- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
//---- get K[1],k[0],d[1],d[0] 
   double valKDCCurrent=iCustom(NULL, 0, "kdj",5,0);  //��ǰһ���ڵ�KDCֵ 
   double valKDCPrevious=iCustom(NULL, 0, "kdj",5,1); //ǰһ���ڵ�KDCֵ 
   double valKCurrent=iCustom(NULL, 0, "kdj",2,0);    //��ǰ���ڵ�Kֵ
   double valKPrevious=iCustom(NULL, 0, "kdj",2,1);   //ǰһ���ڵ�Kֵ
   double valDCurrent=iCustom(NULL, 0, "kdj",3,0);    //��ǰ���ڵ�Dֵ
   double valDPrevious=iCustom(NULL, 0, "kdj",3,1);   //ǰһ���ڵ�Dֵ 
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICKDJ || OrderSymbol()!=Symbol()) continue;
      //---- check order type 
      if(OrderType()==OP_BUY)                          //�Զ෽��ƽ�� 
        {
         if(valKDCPrevious>0 && valKDCCurrent<0|| valKPrevious>valKCurrent)  //k�´�D ������K ��D������ K<d do short 
         OrderClose(OrderTicket(),OrderLots(),Bid,3,White); Sleep(5000);
         break;
        }
      if(OrderType()==OP_SELL)                         //�Կշ���ƽ�� 
         {
         if(valKDCPrevious<0 && valKDCCurrent>0||valKPrevious<valKCurrent)//k�ϴ�D or k>d ����
         OrderClose(OrderTicket(),OrderLots(),Ask,3,White); Sleep(5000);
         break;
        }
     }
//----
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
  lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk*Leverage/100000.0,1);
//---- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false)
          { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) lot=NormalizeDouble(lot-lot*losses*DecreaseFactor,1);
     }
//---- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
  }
//+------------------------------------------------------------------+

