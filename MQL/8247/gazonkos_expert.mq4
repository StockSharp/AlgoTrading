//+------------------------------------------------------------------+
//|                                             gazonkos expert.mq4  |
//|                                                    1H   EUR/USD  |
//|                                                    Smirnov Pavel |
//|                                                 www.autoforex.ru |
//+------------------------------------------------------------------+

#property copyright "Smirnov Pavel"
#property link      "www.autoforex.ru"

extern int magic = 12345;
extern int TakeProfit = 16; // ������� ���������� � �������
extern int Otkat = 16;// �������� ������ � �������
extern int StopLoss = 40; // ������� �������� � �������

extern int t1=3;
extern int t2=2;
extern int delta=40;

extern double lot = 0.1;// ������ �������
extern int active_trades=1;//������������ ���������� ������������ �������� �������

int STATE=0;
int Trade=0;
double maxprice=0.0;
double minprice=10000.0;
int ticket;
bool cantrade=true;
int LastTradeTime=0;
int LastSignalTime=0;

int OpenLong(double volume=0.1)
{
  int slippage=10;
  string comment="gazonkos expert (Long)";
  color arrow_color=Blue;

  ticket=OrderSend(Symbol(),OP_BUY,volume,Ask,slippage,Ask-StopLoss*Point,
                      Ask+TakeProfit*Point,comment,magic,0,arrow_color);
  if(ticket>0)
  {
    if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
    {
      Print("Buy order opened : ",OrderOpenPrice());
      return(0);
    }  
  }
  else 
  {
    Print("Error opening Buy order : ",GetLastError()); 
    return(-1);
  }
}
  
int OpenShort(double volume=0.1)
{
  int slippage=10;
  string comment="gazonkos expert (Short)";
  color arrow_color=Red;

  ticket=OrderSend(Symbol(),OP_SELL,volume,Bid,slippage,Bid+StopLoss*Point,
                      Bid-TakeProfit*Point,comment,magic,0,arrow_color);
  if(ticket>0)
  {
    if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
      {
        Print("Sell order opened : ",OrderOpenPrice());
        return(0);
      }  
  }
  else 
  {
    Print("Error opening Sell order : ",GetLastError()); 
    return(-1);
  }
}

int OrdersTotalMagic(int MagicValue)//������� ���������� ���������� �������� ������� � magic = MagicValue
{
   int j=0;
   int i;
   for (i=0;i<OrdersTotal();i++)//���������� �������� ����� ���� �������� �������
   {
     if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))//�������� ��-������� ������
     {
        if (OrderMagicNumber()==MagicValue) j++; //������������ ������ �� � ������� ������ magic   
     }
     else 
     {    
         Print("gazonkos expert: OrderSelect() � OrdersTotalMagic() ������ ������ - ",GetLastError());    
         return(-1);
     }
   }   
   return(j);//���������� ���������� ������������ ������� � magic = MagicValue.  
}

int init()
{
  return(0);
}

int deinit()
{   
  return(0);
}

int start()
{

// STATE = 0  ���� ������� � ������ ������ ���������  ------------------------------------------------------------

   if (STATE==0)
   {
      bool cantrade=true;
      if(TimeHour(TimeCurrent())==LastTradeTime) cantrade=false;//��������� ��������� ���� �� �������� ����� ��� ����� ��������� 
                                                                //�������� ������ (����� �������� ������������� ���������� ������ �� ����� � ��� �� ������� ����)     
      if(OrdersTotalMagic(magic)>=active_trades) cantrade=false;// ��������� �� ���������� ���������� �������� �������
      if(cantrade) // ���� �� ���� �� ������ ������� �� �������� ������, �� ��������� � �������� �������� ������� �� �������� �������
         STATE=1;
   }

// STATE = 1  ���� �������� (��������) ���� ----------------------------------------------------------------------

   if (STATE==1)
   {
      if((Close[t2]-Close[t1])>delta*Point)// ������ ��� ����� � ������� �������
      {
         Trade = 1; //������������� �������, ��� ������� ������� ������ �� ��������  "-1" - �������� �������, "1"-�������
         maxprice=Bid;// ���������� ������� ��������� ���� (���������� ��� ����������� ������ � STATE=2)
         LastSignalTime=TimeHour(TimeCurrent());//���������� ����� ��������� �������
         STATE = 2; // ������� � ��������� ���������
      }
      
      if((Close[t1]-Close[t2])>delta*Point)// ������ ��� ����� � �������� �������
      {
         Trade = -1; // ������������� �������, ��� ������� ������� ������ �� ��������  "-1" - �������� �������, "1"-�������
         minprice=Bid;// ���������� ������� ��������� ���� (���������� ��� ����������� ������ � STATE=2)
         LastSignalTime=TimeHour(TimeCurrent());//���������� ����� ��������� �������
         STATE = 2; // ������� � ��������� ���������
      }
   }
   
// STATE = 2 - ���� ������ ����   -------------------------------------------------------------------------------- 

   if (STATE==2)
   {
      if(LastSignalTime!=TimeHour(TimeCurrent()))//���� �� ���� �� ������� ������� ������ �� ��������� ������,�� ��������� � ��������� STATE=0
      {   
         STATE=0;
         return(0);         
      }
      if(Trade==1)// ������� ������ ��� ������� �������
      {
         if(Bid>maxprice) maxprice=Bid;//���� ���� ����� ��� ����, �� ������ �������� maxprice �� ������� �������� ����
         if(Bid<(maxprice-Otkat*Point))// ��������� ������� ������ ���� ����� �������� 
            STATE=3;//���� ��������� ����� �� �������� Otkat, �� ��������� � ��������� �������� ������� �������
      }
      
      if(Trade==-1)// ������� ������ ��� �������� �������
      {
         if(Bid<minprice) minprice=Bid;//���� ���� ����� ��� ����, �� ������ �������� minprice �� ������� �������� ����
         if(Bid>(minprice+Otkat*Point))// ��������� ������� ������ ���� ����� ��������
            STATE=3;//���� ��������� ����� �� �������� Otkat, �� ��������� � ��������� �������� �������� �������
      }
   }  

// STATE = 3 - ��������� ������� �������� ���������� Trade ("-1" - ��������, "1" - �������)   -------------------- 
  
   if(STATE==3)
   {
      if(Trade==1)// ��������� ������� �������
      {
         OpenLong(lot);// ��������� ������� �������
         LastTradeTime=TimeHour(TimeCurrent());//���������� ����� ���������� ��������� ������
         STATE=0; //��������� � ��������� ��������
      }
      if(Trade==-1)// ��������� �������� �������
      {
         OpenShort(lot);// ��������� �������� �������  
         LastTradeTime=TimeHour(TimeCurrent());//���������� ����� ���������� ��������� ������
         STATE=0; //��������� � ��������� ��������
      }   
   }  
  return(0);
}