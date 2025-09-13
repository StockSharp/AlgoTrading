//+------------------------------------------------------------------+
//|                                                     UP_bot_1.mq4 |
//|                                         Copyright � 2014, sova75 |
//+------------------------------------------------------------------+
#property copyright   "sova75"
#property link        "html://www.mql4.com"
#property version     "1.00"
#property description "�������� �� ������� - ���� �� ������ ����"
//--- ���������� ��������
#define BUY 0                    // �������� ��������������� ���������� �� ��������� 0
#define SEL 1                    // �������� ��������������� ���������� �� ��������� 1
//--- input parameters
extern string  separator1        ="------ start trade settings ------";
extern int     TakeProfit        =30;        // ������� TakeProfit
extern int     StopLoss          =0;         // ������� StopLoss (if 0 then auto)
extern double  HLdivergence      =0.1;       // �����. ���������� ���� �������� ����� ��� ����� 
extern double  SpanPrice         =6;         // ������ �� ���������� ������ ��� �������� ������
extern double  Lots              =0.01;      // ������ ������
extern int     MaxTrades         =1;         // ����. ���-�� ������������ �������� �������
extern int     Slippage          =5;         // ���������������
extern int     MagicNumber       =140804;    // ����� ���������
extern string  separator2        ="------ output settings ------";
extern bool    TrailingStop      =false;     // ���� ������ �������� �������
extern int     TrailStopLoss     =20;        // ������� ����� StopLoss
extern bool    ZeroTrailingStop  =false;     // ���� ������ �� ������ ��
extern double  StepTrailing      =0.5;       // ��� ����� ������
extern bool    OutputAtLower     =false;     // ����� ��� �������� ���� ����������� ����
extern bool    OutputAtRevers    =false;     // ����� ��� ���������� ������
extern double  SpanToRevers      =3;         // ������ �� ������������ ������ ��� �������� ������
//--- ���������� ����������
int expertBars;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit() {
//--- ��������� �����������
   if(Digits==3 || Digits==5) {
      TakeProfit    *=10;
      StopLoss      *=10;
      Slippage      *=10;
      SpanPrice     *=10;
      HLdivergence  *=10;
      TrailStopLoss *=10;
      StepTrailing  *=10;}
//--- ��������� �������� ���������� HLdivergence � ������� �����
//   if(Digits==2 || Digits==3) {HLdivergence/=1000; SpanPrice/=1000;}
//   else {HLdivergence/=100000; SpanPrice/=100000;}
//--- ��������� ����������� ������ TakeProfit, StopLoss
   if (TakeProfit<MarketInfo(_Symbol,MODE_STOPLEVEL) && TakeProfit!=0) {
      Comment("TakeProfit value too small, must be >= "+DoubleToStr(MarketInfo(_Symbol,MODE_STOPLEVEL),0));}
   if (StopLoss<MarketInfo(_Symbol,MODE_STOPLEVEL) && StopLoss!=0) {
      Comment("StopLoss value too small, must be >= "+DoubleToStr(MarketInfo(_Symbol,MODE_STOPLEVEL),0));}
//---
   return;}
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
   return;}
//+------------------------------------------------------------------+
//| expert ontick function                                           |
//+------------------------------------------------------------------+
void OnTick() {
//--- ���� ������� ���, ��������� ������� ��� �����
   if(CountOrders()<MaxTrades)
      if(NextTrade())
         if(StartTrade(BUY)) OpenOrders(BUY);
   if(CountOrders()<MaxTrades)
      if(NextTrade())
         if(StartTrade(SEL)) OpenOrders(SEL);
//--- ���� ������ ���� - ��������� ������� ������
   if(CountOrders()!=0) {
//--- ������ ����� �������� �������
      if(TrailingStop) Trailing();
//--- ������ ����� �������� ������� �� ������ ��
      if(ZeroTrailingStop) ZeroTrailing();
//--- ������� ��� �������� ���� ����������� ����
      if(OutputAtLower) OutputAL();
//--- ������� ��� �������� iHigh ����
      if(OutputAtRevers) OutputAR();}
   return;}
//+------------------------------------------------------------------+
//| ���������� ���������� �������� ������� �� ������                 |
//+------------------------------------------------------------------+
int CountOrders() {int count=0;
   for(int i=OrdersTotal()-1; i>=0; i--) {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
         if(OrderMagicNumber()==MagicNumber) {
            if(OrderType()==OP_BUY || OrderType()==OP_SELL) count++;}}
   return(count);}
//+------------------------------------------------------------------+
//| ��������� ��� ���������� ��������� ������                        |
//+------------------------------------------------------------------+
bool NextTrade() {int count=0;
   if(OrdersTotal()==0) return true;
   for(int i=OrdersTotal()-1; i >= 0; i--) {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) {
         if(OrderMagicNumber()==MagicNumber) {
            if(OrderType()==OP_BUY || OrderType()==OP_SELL) {
               if(OrderOpenTime()<Time[0])
                  return true;}}}}
   return false;}
//+------------------------------------------------------------------+
//| ��������� ������� ��� �����                                      |
//+------------------------------------------------------------------+
bool StartTrade(int typ) {
//--- ��������� ����������� �������
   if(typ==BUY) {
      if(MathAbs(Low[0]-Low[1])<Point*HLdivergence)
         if(Bid-Low[0]>Point*SpanPrice && Bid-Low[0]<Point*SpanPrice*1.5) return true;}
//--- ��������� ����������� �������
   if(typ==SEL) {
      if(MathAbs(High[0]-High[1])<Point*HLdivergence)
         if(High[0]-Bid>Point*SpanPrice && High[0]-Bid<Point*SpanPrice*1.5) return true;}
   return false;}
//+------------------------------------------------------------------+
//| ��������� ������ �� ������� ����                                 |
//+------------------------------------------------------------------+
bool OpenOrders(int typ) {
   double price=0,SL=0,TP=0,spread=0;           // ������� ���������� ��� ���� 
   int p=0,ticket=-1;                           // � ������� ������� �������� �������
   if(typ==BUY) {                               // ���� �� ����� ������� ����� �� �������
      price=NormalizeDouble(Ask,Digits);        // �������� ���� ��� ��� �������� � ����� �� �� ����������� ��� 4 ��� 5 ������ �������������
      if (StopLoss>0) SL=NormalizeDouble(Bid-Point*StopLoss,Digits); 
      else SL=NormalizeDouble(Low[0]-Point*HLdivergence,Digits);
      TP=NormalizeDouble(Ask+Point*TakeProfit,Digits);}
   if(typ==SEL) {                               // ���� �� ����� ������� ����� �� �������
      price=NormalizeDouble(Bid,Digits);        // �������� ���� ��� ��� �������� � ����� �� �� ����������� ��� 4 ��� 5 ������ �������������
      spread=MarketInfo(_Symbol,MODE_SPREAD);
      if (StopLoss>0) SL=NormalizeDouble(Ask+Point*StopLoss,Digits);
      else SL=NormalizeDouble(High[0]+Point*HLdivergence+Point*spread,Digits);
      TP=NormalizeDouble(Bid-Point*TakeProfit,Digits);}
   if(IsTradeAllowed())                         // ��������, �������� �� ����� ��������� � ����� �� �� ������� �����     
      while(p<5) {                              // �������� ���� ������� �������� ������ �� 5 �������
         ticket=OrderSend(Symbol(),typ,Lots,price,Slippage,SL,TP,WindowExpertName()+"  "+(string)MagicNumber,MagicNumber,0,clrBlack); 
         if(ticket>=0)                          // ���� ��� ����� ��������, �������� ��� ����� � ���������� ticket
            return true;                        // ������ �� ������� � �������
         else {                                 // ���� ������ �� ������ ��� �����
            p++;                                // �������� ������� �� 1
            Print("OrderSend ����������� � ������� #",GetLastError()); // ������� � ������ ��� ������� � ����� ������ 
            Sleep(500); RefreshRates();}}       // �������� ���������� � ������� ������
   return false;}                               // � ������ ���� �� 5 ������� ����� �� ��������, ������ �� ������� � ��������
//+------------------------------------------------------------------+
//| ������ ����� �������� �������                                    |
//+------------------------------------------------------------------+
void Trailing() {
   for(int i=0; i<OrdersTotal(); i++) {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) {
         if(OrderType()<=OP_SELL && OrderMagicNumber()==MagicNumber) {
            if(OrderType()==OP_BUY) {
               if(OrderStopLoss()<Bid-Point*TrailStopLoss) {
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailStopLoss,OrderTakeProfit(),0,clrBlue))
                     Print("������ ����������� Trailing. ��� ������=",GetLastError());}}
            else {
               if(OrderStopLoss()>Ask+Point*TrailStopLoss) {
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailStopLoss,OrderTakeProfit(),0,clrRed))
                     Print("������ ����������� Trailing. ��� ������=",GetLastError());}}}
      else Print("OrderSelect() ������ ������ - ",GetLastError());}}
   return;}
//+------------------------------------------------------------------+
//| ��������� � ������ �� �� ����������                              |
//+------------------------------------------------------------------+
void ZeroTrailing() {double SL;
   for(int i=0; i<OrdersTotal(); i++) {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) {
         if(OrderType()<=OP_SELL && OrderMagicNumber()==MagicNumber) {
            if(OrderType()==OP_BUY) {
               if(OrderStopLoss()<OrderOpenPrice()) {
                  if (StopLoss>0) SL=NormalizeDouble(Bid-Point*StopLoss,Digits);
                  else SL=NormalizeDouble(Bid-Point*(SpanPrice+HLdivergence),Digits);
                  if(OrderStopLoss()<SL && SL-OrderStopLoss()>Point*StepTrailing) {
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),SL,OrderTakeProfit(),0,clrBlue))
                        Print("������ ����������� ZeroTrailing. ��� ������=",GetLastError());}}}
            else {
               if(OrderStopLoss()>OrderOpenPrice()) {
                  if (StopLoss>0) SL=NormalizeDouble(Ask+Point*StopLoss,Digits);
                  else SL=NormalizeDouble(Ask+Point*(SpanPrice+HLdivergence),Digits);
                  if(OrderStopLoss()>SL && OrderStopLoss()-SL>Point*StepTrailing) {
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),SL,OrderTakeProfit(),0,clrRed))
                        Print("������ ����������� ZeroTrailing. ��� ������=",GetLastError());}}}}
      else Print("OrderSelect() ������ ������ - ",GetLastError());}}
   return;}
//+------------------------------------------------------------------+
//| ������� ��� �������� ���� ����������� ����                       |
//+------------------------------------------------------------------+
void OutputAL() {
   for(int i=0; i<OrdersTotal(); i++) {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) {
         if(OrderType()<=OP_SELL && OrderMagicNumber()==MagicNumber) {
            if(OrderType()==OP_BUY) {
               if(Bid<iLow(NULL,0,1)) {
                  if(!OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrBlue))
                     Print("������ ����������� OutputAtLower. ��� ������=",GetLastError());}}
            else {
               if(Bid>iHigh(NULL,0,1)) {
                  if(!OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrRed))
                     Print("������ ����������� OutputAtLower. ��� ������=",GetLastError());}}}
      else Print("OrderSelect() ������ ������ - ",GetLastError());}}
   return;}
//+------------------------------------------------------------------+
//| ������� ��� ���������� ������                                    |
//+------------------------------------------------------------------+
void OutputAR() {
   for(int i=0; i<OrdersTotal(); i++) {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) {
         if(OrderType()<=OP_SELL && OrderMagicNumber() == MagicNumber) {
            if(OrderType()==OP_BUY) {
               if(MathAbs(High[0]-High[1])<Point*HLdivergence) {
                  if(High[0]-Bid>Point*SpanToRevers && High[0]-Bid<Point*SpanToRevers*1.5) {
                     if(!OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrBlue))
                        Print("������ ����������� OutputAtRevers. ��� ������=",GetLastError());}}}
            else {
               if(MathAbs(Low[0]-Low[1])<Point*HLdivergence) {
                  if(Bid-Low[0]>Point*SpanToRevers && Bid-Low[0]<Point*SpanToRevers*1.5) {
                     if(!OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrRed))
                        Print("������ ����������� OutputAtRevers. ��� ������=",GetLastError());}}}}
      else Print("OrderSelect() ������ ������ - ",GetLastError());}}
   return;}
//+------------------------------------------------------------------+