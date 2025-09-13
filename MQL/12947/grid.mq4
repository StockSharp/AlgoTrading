//+------------------------------------------------------------------+
//|                                                         grid.mq4 |
//|                                              ����� ������������� |
//|                                           roman_machanow@mail.ru |
//+------------------------------------------------------------------+
#property copyright "����� �������������"
#property link      "roman_machanow@mail.ru"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| ���������� ����������                                            |
//+------------------------------------------------------------------+
input int Hag = 10;        //��� ������������
input double Lot  = 0.01;  //��� ������
input int tp= 200;         //���� ������
input double Profit_S=1;   //������� ��� ������ ����������� ������
input bool Martin=true; //��������� ���������� ����������� (false=����)
//---
bool Poisc_Po;          //����� �������� ������� �� �������
bool Poisc_Pr;          //����� �������� ������� �� �������
//---
double
PriceAsk,               //���� ����� �� �������
PriceBid,               //���� ����� �� �������
Sredstva,               //��������� ��������
LotSell,                //��� �� �������
LotBuy,                 //��� �� �������
MaxLotBay,              //������������ ��� �� �������
MaxLotSell;             //������������ ��� �� �������
//---
int
TotalOrder,//����� ����� �������
TotalBuy,
TotalSell,
TotalBuyStop,
TotalSellStop,
BuySell,
Perezapusk;
//---
string
textLots;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   TotalOrder=OrdersTotal();

   for(int i=0; i<=TotalOrder;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         int OrderTupe=OrderType();
         if(OrderTupe==OP_SELL) {TotalSell++;}
         if(OrderTupe==OP_BUY ) {TotalBuy++;}
        }
      BuySell=TotalSell+TotalBuy;
     }
//---
   Sredstva=AccountFreeMargin();
//---
   LotBuy = Lot;
   LotSell=Lot;
//---
   Perezapusk=0;
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   RefreshRates();
   ComentInform();//����� �������� ������
   ModDelOtlSdelok();//������ ������������ ����������� ������ � �������� ������������
   ProfitReturn();//������ ������������ ����������� ������ � �������� ������������
   LotSellBuy();//������ ������ ����
//--- ������ ����
   PriceBid=MathFloor(Bid/(Hag*Point))*(Hag*Point);//���� ����� �� �������            
   PriceAsk=MathFloor(Ask/(Hag*Point))*(Hag*Point)+(Hag*Point);//���� ����� �� �������
//---
   Poisc_Po = true;                                //���������� �������
   Poisc_Pr = true;                                //���������� �������
//--- ����������� �� ���������� ������
   for(int i=0; i<=OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         double PriceOrder=OrderOpenPrice();
         int OrderTupe=OrderType();
         if(NormalizeDouble(PriceAsk,Digits)==NormalizeDouble(PriceOrder,Digits))
           {
            if((OrderTupe==OP_BUYSTOP) || (OrderTupe==OP_BUY) || (OrderTupe==OP_SELLSTOP) || (OrderTupe==OP_SELL))
              {
               Poisc_Po=false;
               continue;
              }
           }
        }
     }
//---
   for(int i=0; i<=OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         double PriceOrder=OrderOpenPrice();
         int OrderTupe=OrderType();
         if(NormalizeDouble(PriceBid,Digits)==NormalizeDouble(PriceOrder,Digits))
           {
            if((OrderTupe==OP_SELLSTOP) || (OrderTupe==OP_SELL) || (OrderTupe==OP_BUYSTOP) || (OrderTupe==OP_BUY))
              {
               Poisc_Pr=false;
               continue;
              }
           }
        }
     }
//--- ���������� ������
   if(Poisc_Pr)
     {
      int Error_SELLSTOP=OrderSend(Symbol(),OP_SELLSTOP,LotSell,PriceBid,3,0,PriceBid-tp*Point);
      if(Error_SELLSTOP<0)
        {
         Print("������ SELLSTOP ",GetLastError());
        }
     }
//---
   if(Poisc_Po)
     {
      int Error_BUYSTOP=OrderSend(Symbol(),OP_BUYSTOP,LotBuy,PriceAsk,3,0,PriceAsk+tp*Point);
      if(Error_BUYSTOP<0)
        {
         Print("������ BUYSTOP ",GetLastError());
        }
     }
  }
//+------------------------------------------------------------------+
//| ����� �������� ������                                            |
//+------------------------------------------------------------------+
int ComentInform()
  {
   SvodInform();
//---
   Comment("����� �������   : "+IntegerToString(TotalOrder)+
           "\n ������������: "+IntegerToString(Perezapusk)+
           "\n SELL        : "+IntegerToString(TotalSell)+
           "\n BUY         : "+IntegerToString(TotalBuy)+
           "\n SELLSTOP    : "+IntegerToString(TotalSellStop)+
           "\n BUYSTOP     : "+IntegerToString(TotalBuyStop)+
           "\n �������     : "+DoubleToStr(AccountProfit(),1)+
           "\n ��������    : "+DoubleToStr(Sredstva,0)+textLots);
//AccountFreeMargin()
// Print("�������� ����� = ",AccountEquity());
   return(0);
  }
//--- ���������
int SvodInform()
  {
   TotalOrder=OrdersTotal();
   TotalSell=0;
   TotalBuy=0;
   TotalSellStop=0;
   TotalBuyStop=0;
//---
   for(int i=0; i<=TotalOrder;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         int OrderTupe=OrderType();
         if(OrderTupe==OP_SELL) {TotalSell++;}
         if(OrderTupe==OP_BUY ) {TotalBuy++;}
         if(OrderTupe==OP_SELLSTOP) {TotalSellStop++;}
         if(OrderTupe==OP_BUYSTOP) {TotalBuyStop++;}
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| ������ ������������ ����������� ������ � �������� ������������   |
//+------------------------------------------------------------------+
int ModDelOtlSdelok()
  {
   for(int i=0; i<=TotalOrder;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         int OrderTupe=OrderType();
         if(OrderTupe==OP_SELL) {TotalSell++;}
         if(OrderTupe==OP_BUY ) {TotalBuy++;}
        }
     }
   if(BuySell!=(TotalSell+TotalBuy))
     {
      for(int i=0; i<=TotalOrder;i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            int OrderTupe = OrderType();
            if(OrderType()==OP_BUYSTOP)
              {
               bool ord_close=OrderDelete(OrderTicket(),clrNONE);
               if(!ord_close) i--;
              }
            if(OrderType()==OP_SELLSTOP)
              {
               bool ord_close=OrderDelete(OrderTicket(),clrNONE);
               if(!ord_close) i--;
              }
           }
        }
      BuySell=TotalSell+TotalBuy;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| ������ ����������� ������ ����� ���������� ������ PROFIT         |
//+------------------------------------------------------------------+
int ProfitReturn()
  {
   if(AccountProfit()>=Profit_S)
     {
      for(int i=0; i<=OrdersTotal();i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            int OrderTupe= OrderType();
            if((OrderTupe==OP_SELLSTOP)||(OrderTupe==OP_BUYSTOP))
              {
               bool ord_close=OrderDelete(OrderTicket(),clrNONE);
               if(!ord_close) i--;
              }
            if(OrderTupe==OP_SELL)
              {
               bool ord_close=OrderClose(OrderTicket(),OrderLots(),Ask,5);
               if(!ord_close) i--;
              }
            if(OrderTupe==OP_BUY)
              {
               bool ord_close=OrderClose(OrderTicket(),OrderLots(),Bid,5);
               if(!ord_close) i--;
              }
           }
        }
      Perezapusk++;
      Sredstva=AccountFreeMargin();
      LotSell=0.01;
      LotBuy=0.01;
     }
//---
   return(0);
  }
//+-------------------------------------------------------------------------+
//| ������ ����������� ������ ����� ���������� ������ PROFIT_������ ������� |
//+-------------------------------------------------------------------------+
int ProfitReturn_1()
  {
   if(AccountProfit()>=Profit_S)
     {
      for(int i=0; i<=OrdersTotal();i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            int OrderTupe= OrderType();
            if((OrderTupe==OP_SELLSTOP)||(OrderTupe==OP_BUYSTOP))
              {
               bool ord_close=OrderDelete(OrderTicket(),clrNONE);
               if(!ord_close) i--;
              }
            if(OrderTupe==OP_SELL)
              {
               bool ord_close=OrderClose(OrderTicket(),OrderLots(),Ask,5);
               if(!ord_close) i--;
              }
            if(OrderTupe==OP_BUY)
              {
               bool ord_close=OrderClose(OrderTicket(),OrderLots(),Bid,5);
               if(!ord_close) i--;
              }
           }
        }
      Sredstva=AccountFreeMargin();
      LotSell=0.01;
      LotBuy=0.01;
     }
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| ������ ������ ����                                               |
//+------------------------------------------------------------------+
int LotSellBuy()
  {
   if(Martin==true)
     {
      double
      ToAllLotSell=0,
      ToAllLotBuy=0;
      //---
      for(int i=0; i<=OrdersTotal();i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            double LotOrder=OrderLots();
            int OrderTupe = OrderType();
            if(OrderType()==OP_SELL) {ToAllLotSell=ToAllLotSell+LotOrder;}
            if(OrderType()==OP_BUY) {ToAllLotBuy=ToAllLotBuy+LotOrder;}
           }
        }
      //---
      if(ToAllLotSell>ToAllLotBuy){LotBuy=ToAllLotSell+Lot;}else{LotSell=ToAllLotBuy+Lot;}
      //---
      if(ToAllLotBuy>MaxLotBay){MaxLotBay=ToAllLotBuy;}
      if(ToAllLotSell>MaxLotSell){MaxLotSell=ToAllLotSell;}
      //---
      textLots=("\n����� ����� ������� : "+DoubleToStr(ToAllLotBuy,2)+
                "\n����� ����� ������� : "+DoubleToStr(ToAllLotSell,2)+
                "\n������������ ��� �� ������� : "+DoubleToStr(MaxLotBay,2)+
                "\n������������ ��� �� ������� : "+DoubleToStr(MaxLotSell,2));
     }
   else
     {
      textLots="";
     }
//---
   return(0);
  }
//+------------------------------------------------------------------+
