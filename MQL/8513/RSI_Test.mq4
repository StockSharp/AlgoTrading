//+------------------------------------------------------------------+
//|                                                     RSI_Test.mq4 |
//|                      Copyright � 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2008, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"


extern double RiskPercentage = 10;     // ����
extern int    TrailingStop   = 50;     // �������� ����
extern int    MaxOrders      =  1;     // ������������ ���������� �������, ���� 0 - �� ��������������
extern int    BuyOp          = 12;     // ������ �� �������
extern int    SellOp         = 88;     // ������ �� �������
extern int    magicnumber    = 777;
extern int    Test           = 14;     // ������ RSI
int expertBars;
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
extern int SetHour   = 00;             //��� ������ ����������� 
extern int SetMinute = 05;             //������ ������ ����������� 
int    TestDay     = 3;                      //���������� ���� ��� ����������� 
int    TimeOut     = 12;                     //����� �������� ��������� ����������� � �������
string NameMTS     = "rsi_test";        //��� ���������
string NameFileSet = "rsi_test.set";             //��� Set ����� � �����������
string PuthTester  = "D:\Forex\Metatrader1";//���� � �������
//--- ������������������ ����������
int    Gross_Profit   = 1;                   //���������� �� ������������ �������
int    Profit_Factor  = 2;                   //���������� �� ������������ ������������
int    Expected_Payoff= 3;                   //���������� �� ������������� �����������
//--����� ���������� ��� �����������
string Per1 = "BuyOp";
string Per2 = "SellOp";
string Per3 = "Test";
string Per4 = "";
bool StartTest=false;
datetime TimeStart;
//--- ����������� ���������� ����������������
#include <auto_optimization.mqh>

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


   double margin = MarketInfo(Symbol(), MODE_MARGINREQUIRED);
   double minLot = MarketInfo(Symbol(), MODE_MINLOT);
   double maxLot = MarketInfo(Symbol(), MODE_MAXLOT);
   double step =   MarketInfo(Symbol(), MODE_LOTSTEP);
   double account = AccountFreeMargin();
   
   double percentage = account*RiskPercentage/100;
   
   double Lots = MathRound(percentage/margin/step)*step;
   
   if(Lots < minLot)
   {
      Lots = minLot;
   }
   
   if(Lots > maxLot)
   {
      Lots = maxLot;
   }
   
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   if(!IsTesting() && !IsOptimization()){                //��� ������������ � ����������� �� ���������
      if(TimeHour(TimeCurrent())==SetHour){                //��������� �������� ���� � ������������� ��� �������
        if(!StartTest){                                 //������ �� ���������� �������
            if(TimeMinute(TimeCurrent())>SetMinute-1){     //��������� ��������� ����� � ������������� ��� ������� �������
               if(TimeMinute(TimeCurrent())<SetMinute+1){  //�������� ����� � ������ ���� �� �����-�� �������� ����� ��� ������ ����
                  TimeStart   =TimeLocal();
                  StartTest   =true;                     //���� ������� �������
                  Tester(TestDay,NameMTS,NameFileSet,PuthTester,TimeOut,Gross_Profit,Profit_Factor,Expected_Payoff,Per1,Per2,Per3,Per4);
   }}}}
   BuyOp     =GlobalVariableGet(Per1);
   SellOp    =GlobalVariableGet(Per2);
   Test      =GlobalVariableGet(Per3);
//   TrailingStop=GlobalVariableGet(Per4);
   }
   if(StartTest){                                        //���� ���� ������� ������� ����������
       if(TimeLocal()-TimeStart > TimeOut*60){            //���� � ������� ������� ������ ������ �������������� ������� �������� ������������
       StartTest = false;                                //������� ����
   }}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 
    if(!StartTest){Comment("BuyOp ",BuyOp,"  | SellOp ",SellOp,"  | Test ",Test);}   
   
   
   int i=0;  
   int total = OrdersTotal();   
   for(i = 0; i <= total; i++) 
     {
      if(TrailingStop>0)  
       {                 
       OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() == magicnumber) 
         {
         TrailingStairs(OrderTicket(),TrailingStop,TrailingStop);
         }
       }
      }

   if ((total < MaxOrders || MaxOrders == 0)) 
     {
      if ((iRSI(NULL,0,Test,PRICE_CLOSE,0) < BuyOp) && (iRSI(NULL,0,Test,PRICE_CLOSE,0) > iRSI(NULL,0,Test,PRICE_CLOSE,1)))
       {
        if (Open[0]>Open[1])
          {OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,"RSI_Buy",magicnumber,0,Green);}
       }
      if ((iRSI(NULL,0,Test,PRICE_CLOSE,0) > SellOp) && (iRSI(NULL,0,Test,PRICE_CLOSE,0) < iRSI(NULL,0,Test,PRICE_CLOSE,1)))
       {
        if (Open[0]<Open[1])
          {OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,"RSI_Sell",magicnumber,0,Red);}
       }
     } 
//----
   return(0);
  }
//+------------------------------------------------------------------+

void TrailingStairs(int ticket,int trldistance,int trlstep)
   { 
   
   double nextstair; // ��������� �������� �����, ��� ������� ����� ������ ��������
 
   // ��������� ���������� ��������
   if ((trldistance<MarketInfo(Symbol(),MODE_STOPLEVEL)) || (trlstep<1) || (trldistance<trlstep) || (ticket==0) || (!OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)))
      {
      Print("�������� �������� TrailingStairs() ���������� ��-�� �������������� �������� ���������� �� ����������.");
      return(0);
      } 
   
   // ���� ������� ������� (OP_BUY)
   if (OrderType()==OP_BUY)
      {
      // �����������, ��� ����� �������� ����� ������� ��������������� ��������
      // ���� �������� ���� �������� ��� ����� 0 (�� ���������), �� ��������� ������� = ���� �������� + trldistance + �����
      if ((OrderStopLoss()==0) || (OrderStopLoss()<OrderOpenPrice()))
      nextstair = OrderOpenPrice() + trldistance*Point;
         
      // ����� ��������� ������� = ������� �������� + trldistance + trlstep + �����
      else
      nextstair = OrderStopLoss() + trldistance*Point;
 
      // ���� ������� ���� (Bid) >= nextstair � ����� �������� ����� ����� ��������, ������������ ���������
      if (Bid>=nextstair)
         {
         if ((OrderStopLoss()==0) || (OrderStopLoss()<OrderOpenPrice()) && (OrderOpenPrice() + trlstep*Point<Bid-MarketInfo(Symbol(),MODE_STOPLEVEL)*Point)) 
            {
            if (!OrderModify(ticket,OrderOpenPrice(),OrderOpenPrice() + trlstep*Point,OrderTakeProfit(),OrderExpiration()))
            Print("�� ������� �������������� �������� ������ �",OrderTicket(),". ������: ",GetLastError());
            }
         }
 /*     else
         {
         if (!OrderModify(ticket,OrderOpenPrice(),OrderStopLoss() + trlstep*Point,OrderTakeProfit(),OrderExpiration()))
         Print("�� ������� �������������� �������� ������ �",OrderTicket(),". ������: ",GetLastError());
         }*/
      }
      
   // ���� �������� ������� (OP_SELL)
   if (OrderType()==OP_SELL)
      { 
      // �����������, ��� ����� �������� ����� ������� ��������������� ��������
      // ���� �������� ���� �������� ��� ����� 0 (�� ���������), �� ��������� ������� = ���� �������� + trldistance + �����
      if ((OrderStopLoss()==0) || (OrderStopLoss()>OrderOpenPrice()))
      nextstair = OrderOpenPrice() - (trldistance + MarketInfo(Symbol(),MODE_SPREAD))*Point;
      
      // ����� ��������� ������� = ������� �������� + trldistance + trlstep + �����
      else
      nextstair = OrderStopLoss() - (trldistance + MarketInfo(Symbol(),MODE_SPREAD))*Point;
       
      // ���� ������� ���� (���) >= nextstair � ����� �������� ����� ����� ��������, ������������ ���������
      if (Ask<=nextstair)
         {
         if ((OrderStopLoss()==0) || (OrderStopLoss()>OrderOpenPrice()) && (OrderOpenPrice() - (trlstep + MarketInfo(Symbol(),MODE_SPREAD))*Point>Ask+MarketInfo(Symbol(),MODE_STOPLEVEL)*Point))
            {
            if (!OrderModify(ticket,OrderOpenPrice(),OrderOpenPrice() - (trlstep + MarketInfo(Symbol(),MODE_SPREAD))*Point,OrderTakeProfit(),OrderExpiration()))
            Print("�� ������� �������������� �������� ������ �",OrderTicket(),". ������: ",GetLastError());
            }
         }
 /*     else
         {
         if (!OrderModify(ticket,OrderOpenPrice(),OrderStopLoss()- (trlstep + MarketInfo(Symbol(),MODE_SPREAD))*Point,OrderTakeProfit(),OrderExpiration()))
         Print("�� ������� �������������� �������� ������ �",OrderTicket(),". ������: ",GetLastError());
         }*/
      }      
   }

