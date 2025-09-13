//+------------------------------------------------------------------+
//|                   Strategy of Regularities of Exchange Rates.mq4 |
//|                       Copyright � 2008, ����, yuriy@fortrader.ru |
//|     http://www.ForTrader.ru, ������������� ������ ��� ���������. |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2008, ����, yuriy@fortrader.ru"
#property link      "http://www.ForTrader.ru, ������������� ������ ��� ���������."

extern int optime=9; //�����
extern int cltime=2; //�����
extern int point=20;//����������
extern double Lots=0.1;//����������
extern int TakeProfit=20;//����������
extern int StopLoss=500;//����������

//��������� �������� ��� ������ �� ���������

int bars;
int start()
  {
  Comment("FORTRADER.RU - ������ ��� ������������");
  if(IsDemo()==FALSE && IsTesting()==FALSE){Print("FORTRADER.RU -version only testing");return(0);}
 
 PosManager();
 
  //���� ������ ������ �������� �� �������
 if(Period()>60){Print("Period must be < hour");return(0);}
  
 if(bars!=Bars)
 {bars=Bars;
 
 TimePattern();
 }
   return(0);
  }


int TimePattern()
{
if(Hour() ==optime)
{
//���� ���� ������ ������� ����� �� ������� ���������� ����� � ������ �����
OrderSend(Symbol(),OP_SELLSTOP,Lots,NormalizeDouble(Ask-point*Point,Digits),3,NormalizeDouble(Ask+StopLoss*Point,Digits),0,"FORTRADER.RU",0,0,Red);
OrderSend(Symbol(),OP_BUYSTOP,Lots,NormalizeDouble(Bid+point*Point,Digits),3,NormalizeDouble(Bid-StopLoss*Point,Digits),0,"FORTRADER.RU",0,0,Red);
}

return(0);
}

int deletebstop()
{
   for( int i=1; i<=OrdersTotal(); i++)          
   {
    if(OrderSelect(i-1,SELECT_BY_POS)==true) 
    {                                       
     if(OrderType()==OP_BUYSTOP && OrderSymbol()==Symbol())
     {
      OrderDelete(OrderTicket()); 
     }//if
    }//if
   }
   return(0);
}

int deletesstop()
{
   for( int i=1; i<=OrdersTotal(); i++)          
   {
    if(OrderSelect(i-1,SELECT_BY_POS)==true) 
    {                                       
     if(OrderType()==OP_SELLSTOP && OrderSymbol()==Symbol())
     {
      OrderDelete(OrderTicket()); 
     }//if
    }//if
   }
   return(0);
}

int PosManager()
{int i,z;

if(Hour() ==cltime){deletebstop();deletesstop();}

for(  i=1; i<=OrdersTotal(); i++)          
   {
    if(OrderSelect(i-1,SELECT_BY_POS)==true) 
    {                                       
     if(OrderType()==OP_SELL && ((OrderOpenPrice()-Ask)>=(TakeProfit)*Point || Hour()==cltime))
     {
     OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);   
     }//if
    }//if
   }
   
   
   for(i=1; i<=OrdersTotal(); i++)          
   {
    if(OrderSelect(i-1,SELECT_BY_POS)==true) 
    {                      
     if(OrderType()==OP_BUY && ((Bid-OrderOpenPrice())>=(TakeProfit)*Point || Hour()==cltime))
     {OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet);         
     }//if
    }//if
   }


return(0);
}