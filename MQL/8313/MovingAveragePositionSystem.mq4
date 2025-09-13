//+------------------------------------------------------------------+
//|                                  MovingAveragePositionSystem.mq4 |
//|                                                     FORTRADER.RU |
//|                                              http://FORTRADER.RU |
//|��������� � ������ http://forum.fortrader.ru/index.php?topic=25.0 |
//|������������ � 25 ������ ������� http://www.fortrader.ru/arhiv.php|
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"

extern string x ="��� �������. 0-SMA, 1-EMA, 2-SMMA, 3-LWMA";
extern int TypeMA=3;
extern int PeriodMA=240;
extern int SdvigMA=0;
extern string x1 ="��������� ���������� ���������";
extern double Lots=0.1; //��������� ���
extern double StarLots=0.1; //� ����� ��������� ��� ��������� ������� �� ���������� ����
extern double MaxLots=10; //� ����� ��������� ��� ��������� ������� �� ���������� ����
extern int LossPips=90; //������ � ������ ����� �������� ����������� ���
extern int ProfitPips=170; //������� ����� ������� ���������� ���
extern int TakeProfit=1000; //������� ����� ������� ���������� ���
extern int MM=1;

double lastlot;
datetime Bar;
int start()
  {

   if(Lots>MaxLots){Lots=Lots/2;Print("��������� ������������ ����� �� ����� ������");} 
   if(AccountFreeMargin()<(1000*Lots)){Print("��������� �����, ������� �������� ������� � ��� ���� �������"); Lots=Lots/2;}
   MaPosManager();
   Lots=positionmartin(Lots);
   if(Bar!=iTime(NULL,0,0))
   {
      Bar=iTime(NULL,0,0);
      OpenMaPattern(Lots);
   }
   return(0);
  }

int flaglot, i,buy,sell;
int OpenMaPattern(double lotss)
   {
      buy=0;sell=0;
      for(int  i=0;i<OrdersTotal();i++)
      {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()==OP_BUY ){buy=1;}
      if(OrderType()==OP_SELL){sell=1;}
      }  
   
      double diMA=iMA(NULL,0,PeriodMA,SdvigMA,TypeMA,PRICE_CLOSE,1);
   
      if(Close[1]>diMA && Close[2]<diMA && buy==0){flaglot=0;OrderSend(Symbol(),OP_BUY,NormalizeDouble(lotss,2),Ask,3,0,Ask+TakeProfit*Point,"FORTRADER.RU",16385,0,Red);}
      if(Close[1]<diMA && Close[2]>diMA && sell==0){flaglot=0;OrderSend(Symbol(),OP_SELL,NormalizeDouble(lotss,2),Bid,3,0,Bid-TakeProfit*Point,"FORTRADER.RU",16385,0,Red);}
   }

int MaPosManager()
   {
   double diMA=iMA(NULL,0,PeriodMA,SdvigMA,TypeMA,PRICE_CLOSE,1);

 
   for( i=1; i<=OrdersTotal(); i++)          
     {
      if(OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
              if( Close[1]<diMA && OrderType()==OP_BUY)
              { 
                 OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); 
                 return(0);
              }
         }
       
             
       if(OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
               if(Close[1]>diMA  && OrderType()==OP_SELL)
               {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);
               return(0); 
               }
         }
             
      }
   
   }
     double result,lastresult;int OrNumber=0,mn;
   
     double  positionmartin(double lastlot)
  {

      if(MM==1)
   {  
   result=0;
    for( i=0; i<=OrdersHistoryTotal(); i++)
     {
        if(true==OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
        {
            if(OrderLots()==Lots && i>=OrNumber)
            {
             if(OrderType()==OP_BUY){result=result+(OrderClosePrice()-OrderOpenPrice());}
             if(OrderType()==OP_SELL){result=result+(OrderOpenPrice()-OrderClosePrice());}
            }
         }
      }
      
     for( i=1; i<=OrdersTotal(); i++)          
     {
         if(OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
              if( OrderType()==OP_BUY && OrderProfit()<0){result=result+(OrderClosePrice()-OrderOpenPrice());}
              if( OrderType()==OP_SELL && OrderProfit()<0){result=result+(OrderOpenPrice()-OrderClosePrice());}
         }
      }
      
      
      if(MarketInfo(Symbol(),MODE_DIGITS)==4){mn=10000;}
      if(MarketInfo(Symbol(),MODE_DIGITS)==2){mn=100;}
      
      result=result*mn;
      Print("������� ��������� � ����� ",Lots," = ",result, " ������� ");
      
      if(result<-LossPips)
      {
      Lots=Lots*2;
      Print("������� ������������� ��������� �������� 100 �, ����������� ��� � 2 ���� = ",Lots);
      Print("��������� �������� ������� � ������� ����� �.� ��� ��� ������ �������");
      for( i=1; i<=OrdersTotal(); i++)          
      {
         if(OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
              if( OrderType()==OP_BUY){OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); }
              if( OrderType()==OP_SELL){ OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);}
         }
      }
      return(Lots);
      }
      
       if(result>ProfitPips)
      {
      Lots=StarLots;
      Print("������� ������ �������� 100 �, ���������� ��� �� ���������� 0,1= ",Lots);
      Print("��������� �������� ������� � ������� ����� �.� ��� ��� ������ �������");
      for( i=1; i<=OrdersTotal(); i++)          
      {
         if(OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
              if( OrderType()==OP_BUY){OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); }
              if( OrderType()==OP_SELL){ OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);}
         }
      }
       OrNumber=OrdersHistoryTotal();
      Print("��������� ����� ������� � ������� ������� ���  ",OrNumber);
        
      return(Lots);
      }
   }

   return(Lots);
 }
 
 

