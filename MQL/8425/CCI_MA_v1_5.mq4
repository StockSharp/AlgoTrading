//+------------------------------------------------------------------+
//|                                                       RSI_MA.mq4 |
//|                   Copyright � 2008,  AEliseev k800elik@gmail.com |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2008,  Andrey E. k800elik@gmail.com"
#property link      "http://www.metaquotes.net"

//---- input parameters
extern int       CCI_Per=14;
extern int       MA_Per=9;
extern int       CCI_close_Per=14;
extern double    TakeProfit=50;
extern double    StopLoss=40;
extern double    Lots=0.1;
extern int       Deposit=1000;
extern bool      MM=false;  
extern int       _MagicNumber = 13131313; 
extern int       _Sleep=5000;     

int LotA=1, Err=0;
int j=0, var2,var1;
int counter=0;
int ticket;
     bool var3;

bool order=false;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   Comment("CCI_MA v1.5 � 2008,  Andrey E. k800elik@gmail.com");
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
/*  int i, a1, a2;
//----
  if (OrdersTotal()>0)
      {
       for (i=0; i==OrdersTotal(); i++) 
         {
          OrderSelect(i,SELECT_BY_POS);
          if (OrderMagicNumber()==131313)
             { 
              a1=OrderClose(LotA*Lots,Ask,5,Red);
              //a2=OrderClose(var1,LotA*Lots,Ask,5,Red);
             }
         }
      } */
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   if (isNewBar()) EveryBar(); 
   EveryTick();
 //----
   return(0);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|  �������� �� ��������� ������ ����                                           |
//+------------------------------------------------------------------+
bool isNewBar()
   {  static datetime TimeBar=0;
      bool flag=false;
      if(TimeBar!=Time[0])
         {
          TimeBar=Time[0];
          flag=true;
         } 
      return (flag);
   }
   
//+------------------------------------------------------------------+
//| ��������� �������� ��� ��������� ������ ����                                            |
//+------------------------------------------------------------------+
void EveryBar()
   {
    //  Print("������ ����� ���!");
      double MA[100], CCI[100], CCIclose[100], StandDevi[100];
      int    i,limit=ArraySize(CCI);
      double SL, TP;
 
     Sleep(_Sleep);
     
      SL = NormalizeDouble(Ask - StopLoss*Point, Digits);
      TP = NormalizeDouble(Ask + TakeProfit*Point, Digits);
      
     
     for(i=0; i<limit; i++)
         {      
          CCI[i]=iCCI(NULL,0,CCI_Per,PRICE_CLOSE,i);
          CCIclose[i]=iCCI(NULL,0,CCI_close_Per,PRICE_CLOSE,i);
          MA[i]=iMAOnArray(CCI,0,MA_Per,0,MODE_SMA,i);     
         }
   
// ���������, ������� �� ����� ��� �������� ������ ������
      if(AccountFreeMargin()<(1000*Lots))        
         {
           Print("We have no money. Free Margin = ", AccountFreeMargin());         
           return(0);          
         }
            
//=================================================================================
    int _GetLastError = 0, _OrdersTotal = OrdersTotal();
    //---- ���������� ��� �������� �������
    for ( int z = _OrdersTotal - 1; z >= 0; z -- )
    {
  //---- ���� ��� ������ ������� �������� ������, ��������� � ���������
        if ( !OrderSelect( z, SELECT_BY_POS ) )
        {
            _GetLastError = GetLastError();
            Print( "OrderSelect( " ,z, ", SELECT_BY_POS ) - Error #", _GetLastError);
            continue;
        }
 
// ���� ������� ������� �� �� �������� �����������, ���������� �
        if ( OrderSymbol() != Symbol() ) continue;
 
// ���� MagicNumber �� ����� _MagicNumber, ���������� 
// ��� �������
        if ( OrderMagicNumber() != _MagicNumber ) continue;
 
        //---- ���� ������� ���-�������,
        if ( OrderType() == OP_BUY )
        {
            //---- ���� ��I ���������� ����� 100 ����
            if (((CCIclose[2]>100) && (CCIclose[1]<=100)) || (CCI[1]<MA[limit-MA_Per-1]) && (CCI[2]>=MA[limit-MA_Per-2]))
            {
                //---- ��������� �������
                if ( !OrderClose( OrderTicket(), OrderLots(), Bid, 10, Green ) )
                {
                    _GetLastError = GetLastError();
                    Print( "������ OrderClose � ", _GetLastError );
                    Err=1;
                    return(-1);
                }
            }
// ���� ������ �� ���������, ������� - ���� ���� ��������� 
// ����� �������
            else
            { return(0); }
        }
        //---- ���� ������� ����-�������,
        if ( OrderType() == OP_SELL )
        {
            //---- ���� ��I ���������� ����� -100 �����,
            if (((CCIclose[2]<-100) && (CCIclose[1]>=-100)) || ((CCI[1]>MA[limit-MA_Per-1]) && (CCI[2]<=MA[limit-MA_Per-2])))
            {
                //---- ��������� �������
                if(!OrderClose( OrderTicket(), OrderLots(), Ask, 10, Red ) )
                {
                    _GetLastError = GetLastError(); 
                    Print( "������ OrderClose � ", _GetLastError );
                    Err=2;
                    return(-1);
                }
            }
// ���� ������ �� ���������, ������� - ���� ���� ��������� 
// ����� �������
            else return(0);
        }
    }
 
//+------------------------------------------------------------------+
//| ���� ���������� ����� �� ����� �����, ������ �������� ������� ���|
//| ���������, ���� �� ����������� ������� �������                   |
//+------------------------------------------------------------------+
 
 Sleep(_Sleep);
 
    //---- ���� ��I ������ MACD ����� ����� ,
   SL = NormalizeDouble(Bid - StopLoss*Point, Digits);
   TP = NormalizeDouble(Bid + TakeProfit*Point, Digits);
   if ((CCI[1]>MA[limit-MA_Per-1]) && (CCI[2]<MA[limit-MA_Per-2]))
    {
        //---- ��������� ��� �������
        if(OrderSend( Symbol(), OP_BUY, LotA*Lots, Ask, 5, SL, TP, "CCI_test_buy", _MagicNumber, 0, Green ) < 0 )
        {
            _GetLastError = GetLastError();
            Print( "������ OrderSend OP_BUY � ", _GetLastError );
            Err=3;
            return(-1);
        }
        return(0);
    }
    //---- ���� ��I ������ MACD ������ ����,
    SL = NormalizeDouble(Ask + StopLoss*Point, Digits);
    TP = NormalizeDouble(Ask - TakeProfit*Point, Digits); 
    
   if ((CCI[1]<MA[limit-MA_Per-1]) && (CCI[2]>MA[limit-MA_Per-2]))
    {
        //---- ��������� ���� �������
        if(OrderSend( Symbol(), OP_SELL, LotA*Lots, Bid, 5, SL, TP, "CCI_test_close", _MagicNumber, 0, Red ) < 0 )
        {
            _GetLastError = GetLastError();
            Print( "������ OrderSend OP_SELL � ", _GetLastError );
            Err=4;
            return(-1);
        }
        return(0);
    }
 }  
//+------------------------------------------------------------------+
//| ��������� ���������� ��� ������ ��������� ����                                           |
//+------------------------------------------------------------------+   
void EveryTick()
     {
      int i;
      if (MM==true)
         {
            if (AccountBalance()>(2*Deposit)) LotA=2; 
            if (AccountBalance()>(3*Deposit)) LotA=3; 
            if (AccountBalance()>(4*Deposit)) LotA=4; 
            if (AccountBalance()>(5*Deposit)) LotA=5; 
            if (AccountBalance()>(6*Deposit)) LotA=6; 
            if (AccountBalance()>(7*Deposit)) LotA=7; 
            if (AccountBalance()>(8*Deposit)) LotA=8; 
            if (AccountBalance()>(9*Deposit)) LotA=9; 
            if (AccountBalance()>(10*Deposit)) LotA=10; 
            if (AccountBalance()>(11*Deposit)) LotA=11; 
            if (AccountBalance()>(12*Deposit)) LotA=12; 
            if (AccountBalance()>(13*Deposit)) LotA=13; 
            if (AccountBalance()>(14*Deposit)) LotA=14; 
            if (AccountBalance()>(15*Deposit)) LotA=15; 
            if (AccountBalance()>(16*Deposit)) LotA=16; 
            if (AccountBalance()>(17*Deposit)) LotA=17; 
            if (AccountBalance()>(18*Deposit)) LotA=18; 
            if (AccountBalance()>(19*Deposit)) LotA=19;
            if (AccountBalance()>(20*Deposit)) LotA=20;
          }  
          
   int _GetLastError = 0, _OrdersTotal = OrdersTotal();
   int z;
   double SL, TP;
   double MA[100], CCI[100], CCIclose[100], StandDevi[100];
   int    limit=ArraySize(CCI);
   for(i=0; i<limit; i++)
     {      
          CCI[i]=iCCI(NULL,0,CCI_Per,PRICE_CLOSE,i);
          CCIclose[i]=iCCI(NULL,0,CCI_close_Per,PRICE_CLOSE,i);
          MA[i]=iMAOnArray(CCI,0,MA_Per,0,MODE_SMA,i);      
      }
   
   switch(Err)
   {
    case 1:  
      for ( z=_OrdersTotal - 1; z >= 0; z -- )
         {
          if ( !OrderSelect( z, SELECT_BY_POS ) )
            {
             _GetLastError = GetLastError();
             Print( "OrderSelect( " ,z, ", SELECT_BY_POS ) - Error #", _GetLastError);
             continue;
            }
         if ( OrderSymbol() != Symbol() ) continue;
         if ( OrderMagicNumber() != _MagicNumber ) continue;
         if ( OrderType() == OP_BUY )
            {
             if (((CCIclose[2]>100) && (CCIclose[1]<=100)) || (CCI[1]<MA[limit-MA_Per-1]) && (CCI[2]>=MA[limit-MA_Per-2]))
               {
                if ( !OrderClose( OrderTicket(), OrderLots(), Bid, 5, Green ) )
                  {
                   _GetLastError = GetLastError();
                   Print( "������ CASE OrderClose � ", _GetLastError );
                   Err=1;
                   return(-1);
                  }
                else Err=0; 
               } 
            }
         }   
      break;
 //=============================================================================     
    case 2:
      for ( z=_OrdersTotal - 1; z >= 0; z -- )
         {
          if ( !OrderSelect( z, SELECT_BY_POS ) )
             {
              _GetLastError = GetLastError();
              Print( "OrderSelect( " ,z, ", SELECT_BY_POS ) - Error #", _GetLastError);
              continue;
             }
         if ( OrderSymbol() != Symbol() ) continue;
         if ( OrderMagicNumber() != _MagicNumber ) continue;
         if ( OrderType() == OP_SELL )
            {
             if (((CCIclose[2]<-100) && (CCIclose[1]>=-100)) || ((CCI[1]>MA[limit-MA_Per-1]) && (CCI[2]<=MA[limit-MA_Per-2])))           
               {
                if(!OrderClose( OrderTicket(), OrderLots(), Ask, 5, Red ) )
                  {
                   _GetLastError = GetLastError(); 
                   Print( "������ CASE OrderClose � ", _GetLastError );
                   Err=2;
                   return(-1);
                  }
                else Err=0;   
               }
            }
         }  
      break; 
  //==============================================================================        
    case 3:
      SL = NormalizeDouble(Bid - StopLoss*Point, Digits);
      TP = NormalizeDouble(Bid + TakeProfit*Point, Digits);
      if(OrderSend( Symbol(), OP_BUY, LotA*Lots, Ask, 7, SL, TP, "AI_test_buy", _MagicNumber, 0, Green ) < 0 )
        {
         _GetLastError = GetLastError();
         Print( "������ CASE OrderSend OP_BUY � ", _GetLastError );
         Err=3;
         return(-1);
        }
      else Err=0;  
      break;
 //===================================================================================     
    case 4:
      SL = NormalizeDouble(Ask + StopLoss*Point, Digits);
      TP = NormalizeDouble(Ask - TakeProfit*Point, Digits);
      if(OrderSend( Symbol(), OP_SELL, LotA*Lots, Bid, 7, SL, TP, "AI_test_close", _MagicNumber, 0, Red ) < 0 )
        {
         _GetLastError = GetLastError();
         Print( "������ CASE OrderSend OP_SELL � ", _GetLastError );
         Err=4;
         return(-1);
        }
      else Err=0;  
      break;
   }         
 
      
   }