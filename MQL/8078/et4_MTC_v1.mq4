//+------------------------------------------------------------------+ 
//| et4_MTC_v1.mq4
//| goldenlion@ukr.net
//| http://GlobeInvestFund.com/
//+------------------------------------------------------------------+ 
#property copyright "Copyright c 2005, goldenlion@ukr.net" 
#property link "http://GlobeInvestFund.com/"  

#include <stdlib.mqh>  

extern double TakeProfit = 150; 
extern double Lots = -10; 
extern double StopLoss = 50; 

extern double Slippage = 3; 

extern double LogOn = 0; 

///////////////////////////////////////////////  

int newCandel = 0; //������� ����� �����  
int prev_candel_time = 0; 
int err; 
int ticket; 
int LastTradeTime; 

//+------------------------------------------------------------------+ 
//| ������ �����������, ������������ ����� ������� | 
//+------------------------------------------------------------------+ 

bool MOrderDelete( int ticket )  
  {  
  LastTradeTime = CurTime(); 
  return ( OrderDelete( ticket ) ); 
  }  

bool MOrderClose( int ticket, double lots, double price, int slippage, color Color=CLR_NONE)  
  {  
  LastTradeTime = CurTime(); 
  price = MathRound(price*10000)/10000; 
  return ( OrderClose( ticket, lots, price, slippage, Color) ); 
  }  

bool MOrderModify( int ticket, double price, double stoploss, double takeprofit, datetime expiration, color arrow_color=CLR_NONE)  
  {  
  LastTradeTime = CurTime(); 
  price = MathRound(price*10000)/10000; 
  stoploss = MathRound(stoploss*10000)/10000; 
  takeprofit = MathRound(takeprofit*10000)/10000; 
  return ( OrderModify( ticket, price, stoploss, takeprofit, expiration, arrow_color) ); 
  }  

int MOrderSend( string symbol, int cmd, double volume, double price, int slippage, double stoploss, double takeprofit, string comment="", int magic=0, datetime expiration=0, color arrow_color=CLR_NONE)  
  {  
  LastTradeTime = CurTime(); 
  price = MathRound(price*10000)/10000; 
  stoploss = MathRound(stoploss*10000)/10000; 
  takeprofit = MathRound(takeprofit*10000)/10000; 
  return ( OrderSend( symbol, cmd, volume, price, slippage, stoploss, takeprofit, comment, magic, expiration, arrow_color ) ); 
  }  


//+------------------------------------------------------------------+ 
//| ���������� ���
//+------------------------------------------------------------------+ 
double LotsCalc()  
  {  
  double Lots2 = Lots; 

  if( Lots2 < 0 ) Lots2 = ( MathFloor( ( AccountBalance() /1000*(-Lots2))/10 )/10 ); 

  if ( Lots2 < 0.1 ) return (0.1); 

  return (Lots2); 
  }  

//+------------------------------------------------------------------+ 
//| ���������� ������
//+------------------------------------------------------------------+ 
int CheckLevels()  
  {  
  //������� ����� �����  
  if( prev_candel_time == Time[0] ) newCandel = 0; 
  else  
    {  
    newCandel = 1; 
    prev_candel_time = Time[0]; 
    }  

//��� ���  

  return(0); 
  }  

//+------------------------------------------------------------------+ 
//| ��������� �������
//+------------------------------------------------------------------+ 
int OpenPos()  
  {  
  //��� ���  

  return(0); 
  }  

//+------------------------------------------------------------------+ 
//| ������������
//+------------------------------------------------------------------+ 
int MovePos()  
  {  
//��� ���  

  return(0); 
  }  

//+------------------------------------------------------------------+ 
//| ��������� ������� | 
//+------------------------------------------------------------------+ 
int ClosePos()  
  {  
//��� ���  
  return(0); 
  }  

//+------------------------------------------------------------------+ 
//| expert initialization function | 
//+------------------------------------------------------------------+ 
int init()  
  {  
//��� ���  
  return(0); 
  }  

//+------------------------------------------------------------------+ 
//| expert deinitialization function | 
//+------------------------------------------------------------------+ 
int deinit()  
  {  
//��� ���  
  return(0); 
  }  

//+------------------------------------------------------------------+ 
//| expert start function | 
//+------------------------------------------------------------------+ 
int start()  
  {  
  if( CurTime() - LastTradeTime < 30 ) return (0); //�������� �� 30 ������ ���� ���� �������  

  CheckLevels(); //������������� �������� �� ������� �����������  

//���� � ��������� ���������� �������� � �������� �� ����������� ����� ����� ���� �������� 
//��������� �� ��������� return(1) ��� ���� ��� �� ���������� ���������� ������ ��������� 
//� ������� ����� ���� ���������� �����  

  if ( OpenPos() == 1 ) return(0); //������ �������� �������  
  if ( MovePos() == 1 ) return(0); //������ ������������� �������  
  if ( ClosePos() == 1 ) return(0); //������ �������� � �������� �������  

  return(0); 
  } 

// the end.
