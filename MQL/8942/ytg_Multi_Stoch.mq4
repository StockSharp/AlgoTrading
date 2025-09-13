//+------------------------------------------------------------------+
//|                                              ytg_Multi_Stoch.mq4 |
//|                                   Copyright � 2009, Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2009, Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

extern bool      Use_Symbol1   = true;
extern string    Symbol1       = "EURUSD";

extern bool      Use_Symbol2   = true;
extern string    Symbol2       = "USDCHF";

extern bool      Use_Symbol3   = true;
extern string    Symbol3       = "GBPUSD";

extern bool      Use_Symbol4   = true;
extern string    Symbol4       = "USDJPY";

extern int       StopLoss      = 50;
extern int       TakeProfit    = 10;

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
   if (Use_Symbol1==true) OpenSymbol(Symbol1); // ��� ���������������
   if (Use_Symbol2==true) OpenSymbol(Symbol2); // ��� ���������������
   if (Use_Symbol3==true) OpenSymbol(Symbol3); // ��� ���������������
   if (Use_Symbol4==true) OpenSymbol(Symbol4); // ��� ���������������   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//+----------------------------------------------------------------------------+
//|  �����    : ���� ������� ,  yuriytokman@gmail.com                          |
//+----------------------------------------------------------------------------+
//|  �������� : ���������� �������� ������                                     |
//+----------------------------------------------------------------------------+
 int GetSignal(string vSymbol)
   {
    double Stoch_Main_0 =iStochastic(vSymbol,0,5,3,3,MODE_SMA,0,MODE_MAIN,0);
    double Stoch_Main_1 =iStochastic(vSymbol,0,5,3,3,MODE_SMA,0,MODE_MAIN,1);
    double Stoch_Sign_0 =iStochastic(vSymbol,0,5,3,3,MODE_SMA,0,MODE_SIGNAL,0);    
    double Stoch_Sign_1 =iStochastic(vSymbol,0,5,3,3,MODE_SMA,0,MODE_SIGNAL,1);    
    
    int vSignal = 0;
    
    if (Stoch_Main_0<20&&Stoch_Main_1<Stoch_Sign_1&&Stoch_Main_0>Stoch_Sign_0)
    
    vSignal =+1;//up
 
    else 
    if (Stoch_Main_0>80&&Stoch_Main_1>Stoch_Sign_1&&Stoch_Main_0<Stoch_Sign_0)
    
    vSignal =-1;//down
          
    return (vSignal);
   }
//+----------------------------------------------------------------------------+
//|  �����    : ���� ������� ,  yuriytokman@gmail.com                          |
//+----------------------------------------------------------------------------+
//|  �������� : �������� �������                                               |
//+----------------------------------------------------------------------------+
 int OpenSymbol(string vSymbol)
   {
    double point = MarketInfo(vSymbol,MODE_POINT);   
    double bid   = MarketInfo(vSymbol,MODE_BID);
    double ask   = MarketInfo(vSymbol,MODE_ASK);
      
    if(!ExistPositions(vSymbol))
     {
   
      if(GetSignal(vSymbol)==1)
    
      OrderSend(vSymbol,OP_BUY,0.01,ask,3,bid-StopLoss*point,ask+TakeProfit*point,"yuriytokman@gmail.com",0,0,Green);
     
      if(GetSignal(vSymbol)==-1)
          
      OrderSend(vSymbol,OP_SELL,0.01,bid,3,ask+StopLoss*point,bid-TakeProfit*point,"yuriytokman@gmail.com",0,0,Red);   
     
     }   
   }
//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ���������� ���� ������������� �������                          |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    ot - ����� ��������             ( 0   - ����� ����� ��������)           |
//+----------------------------------------------------------------------------+
bool ExistPositions(string sy="", int op=-1, int mn=-1, datetime ot=0) {
  int i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (ot<=OrderOpenTime()) return(True);
            }
          }
        }
      }
    }
  }
  return(False);
}    