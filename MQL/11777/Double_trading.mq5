//+------------------------------------------------------------------+
//|                                               Double trading.mq5 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

//--- input parameters
input string Commentation1= "";//��������� ������:
input double    Lot1=1;// ���� ��� 1-�� �������
input double    Lot2=1.3;// ���� ��� 2-�� �������
input double    StopLoss=5000;// Stop Loss
input double    TakeProfit=5000;// Take Profit
input double    Profit=20;// ��������� ������� � ������ ��������
enum comm 
  {
   S,     // ������� ������ (������� ����� �����������)
   };
input comm MoneyN1= S;// ������ � 1
input string Money2= "USDCHF";// ������ � 2
input string Money1_SELL_or_BUY="Auto";// ������ � 1: ��������� (SELL), �������� (BUY) ��� ����� (Auto)?
input string Money2_SELL_or_BUY="Auto";// ������ � 2: ��������� (SELL), �������� (BUY) ��� ����� (Auto)?
enum Tr 
  {
   AutoD = 0,     // ������
   AutoM = 1,     // ����������
   };
input Tr Auto= AutoM;// ����� � ��� ������� (��� ���������������� ��������)
input int    Try=10;// ������� ��� �������� ������� �����
input string Commentation2= "";//������ �� ����������:
input bool   Monday=true;// ����������� (True - ��������, False - ��������)
input bool   Tuesday=true;// ������� (True - ��������, False - ��������)
input bool   Wednesday=true;// ����� (True - ��������, False - ��������)
input bool   Thursday=true;// ������� (True - ��������, False - ��������)
input bool   Friday=false;// ������� (True - ��������, False - ��������)
input string Commentation3= "";//��������� ���������� Correlation:
input double Open_Value=-0.98;// ������ ���������� ��� �������� �������
input double Demis_Value=0.003;// ����������� ������ (+/-)
input int Depth=50;// ������ ��������
input ENUM_APPLIED_PRICE AppliedPrice=PRICE_WEIGHTED;// ����

int  iWeek=0,Monday1=0,Tuesday2=0,Wednesday3=0,Thursday4=0,Friday5=0;
int  i=0,Correlation1=0,Deals=0;
long  TimePosition1=0,TimePosition2=0;
bool AutoF=false ,start=true, CorrelationF=0, TrendUP=0, TrendDown=0, TrendUP2=0, TrendDown2=0;
double order1=0, order2=0,ticket1=0, ticket2=0,CorrelationBuffer[1],OrderProfit1=0,OrderProfit2=0,bid=0,ask=0,point=0;
string Money1_SELLorBUY="",Money2_SELLorBUY="",Money1=Symbol(),nTypeEntry="";
datetime starttime=0;

//-----OrderSend:
double MyOrderSend(
   string   symbol,               // ������
   ENUM_ORDER_TYPE cmd,           // �������� ��������
   double   volume,               // ���������� �����
   double   price,                // ����
   int      slippage,             // ���������������
   double   stoploss,             // stop loss
   double   takeprofit,           // take profit
   string   comment=NULL,         // �����������
   int      magic=0,              // �������������
   datetime expiration=0,         // ���� ��������� ������
   color    arrow_color=clrNONE  // ����
               )
  {
  //--- ������� ������
   MqlTradeRequest request={0};
   request.action=TRADE_ACTION_DEAL;         // ��������� ����������� ������
   request.magic=magic;                         // ORDER_MAGIC
   request.symbol=symbol;                       // ����������
   request.volume=volume;                       // ����� � �����
   request.sl=stoploss;                         // Stop Loss
   request.tp=takeprofit;                       // Take Profit   
   request.type=cmd;                            // ���  ������
   request.price=price;                         // ���� ��� ��������
   request.deviation=slippage;                  // ���������������
   request.comment=comment;                     // �����������
   request.expiration=expiration;               // ���� ��������� ������
//--- �������� �������� ������
   MqlTradeResult result={0};
   if (OrderSend(request,result))
   return(result.price);
   else
   return(0);
  }
//-----OrderSend


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
  {
//----
Money1_SELLorBUY = Money1_SELL_or_BUY;
Money2_SELLorBUY = Money2_SELL_or_BUY;
StringTrimLeft(Money1_SELLorBUY);
StringTrimLeft(Money2_SELLorBUY);
StringTrimRight(Money1_SELLorBUY);
StringTrimRight(Money2_SELLorBUY);
CorrelationBuffer[0]=0;

if (Monday==true)
{Monday1=1;
iWeek=1;}
if (Tuesday==true)
{Tuesday2=2;
iWeek=2;}
if (Wednesday==true)
{Wednesday3=3;
iWeek=3;}
if (Thursday==true)
{Thursday4=4;
iWeek=4;}
if (Friday==true)
{Friday5=5;
iWeek=5;}

if (StringSubstr(Money1_SELLorBUY,0,1)=="a" || StringSubstr(Money1_SELLorBUY,0,1)=="A" || StringSubstr(Money2_SELLorBUY,0,1)=="a" || StringSubstr(Money2_SELLorBUY,0,1)=="A")
AutoF = true;
//----
   //return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
void OnTick()
  {
  //----Function,ext:
  //Server Time:
  datetime Time1=TimeGMT();
  MqlDateTime strTime;
  TimeToStruct(Time1,strTime);
  //---

//----Robot:
// Int indicator (CorrelationBuffer[0],etc):
Correlation1=iCustom(Money1,0,"Correlation",Money2,Depth,AppliedPrice);
CopyBuffer(
   Correlation1,     // handle ����������
   0,                // ����� ������ ����������
   0,                // ������ ������ 
   1,                // ������� ��������
   CorrelationBuffer // ������, ���� ����� ����������� ������
   );   
// Int indicator (CorrelationBuffer[0],etc)

///Correlation signal(CorrelationF):
if ((ticket1==0 && order1!=0) || start == true)
{
if ((ticket2==0 && order2!=0) || start == true)
{

if (CorrelationBuffer[0]<=Open_Value+Demis_Value && CorrelationBuffer[0]>=Open_Value-Demis_Value)
   CorrelationF=true;
///Correlation signal

///TRand (for Auto trading)
if (AutoF)
{
TrendUP=0;
TrendDown=0;
TrendUP2=0;
TrendDown2=0;
   if (((strTime.hour >= 19 && strTime.min >= 00) && (strTime.hour <= 23 && strTime.min <= 59)) || ((strTime.hour >= 00 && strTime.min >= 00) && (strTime.hour <= 5 && strTime.min <= 59))) // 19:00 - 6:00 (������������� ������ AUD, NZD)
      {if (StringFind(Money1,"AUD")==0)
         TrendUP = true; 
      if (StringFind(Money1,"AUD")==3)
         TrendDown = true; 
      if (StringFind(Money1,"NZD")==0)
         TrendUP = true; 
      if (StringFind(Money1,"NZD")==3)
         TrendDown = true; 
      if (StringFind(Money2,"AUD")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"AUD")==3)
         TrendDown2 = true; 
      if (StringFind(Money2,"NZD")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"NZD")==3)
         TrendDown2 = true;} 
   if (((strTime.hour >= 6 && strTime.min >= 00) && (strTime.hour <= 8 && strTime.min <= 59))) // 6:00 - 9:00 (��������� ������ JPY)
      {if (StringFind(Money1,"JPY")==0)
         TrendUP = true; 
      if (StringFind(Money1,"JPY")==3)
         TrendDown = true;          
      if (StringFind(Money2,"JPY")==0)
         TrendUP2 = true;    
      if (StringFind(Money2,"JPY")==3)
         TrendDown2 = true;}         
   if ((strTime.hour >= 9 && strTime.min >= 00) && (strTime.hour <= 15 && strTime.min <= 59)) // 9:00 - 16:00 (����������� ������ EUR, CHF, GBP)
      {if (StringFind(Money1,"EUR")==0)
         TrendUP = true; 
      if (StringFind(Money1,"EUR")==3)
         TrendDown = true; 
      if (StringFind(Money1,"CHF")==0)
         TrendUP = true; 
      if (StringFind(Money1,"CHF")==3)
         TrendDown = true; 
      if (StringFind(Money1,"GBP")==0)
         TrendUP = true; 
      if (StringFind(Money1,"GBP")==3)
         TrendDown = true;           
      if (StringFind(Money2,"EUR")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"EUR")==3)
         TrendDown2 = true; 
      if (StringFind(Money2,"CHF")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"CHF")==3)
         TrendDown2 = true;            
      if (StringFind(Money2,"GBP")==0)
         TrendUP2 = true;    
      if (StringFind(Money2,"GBP")==3)
         TrendDown2 = true;}           
   if ((strTime.hour >= 16 && strTime.min >= 00) && (strTime.hour <= 21 && strTime.min <= 59)) // 16:00 - 22:00 (������������ ������ USD, CAD)
      {if (StringFind(Money1,"USD")==0)
         TrendUP = true;
      if (StringFind(Money1,"USD")==3)
         TrendDown = true; 
      if (StringFind(Money1,"CAD")==0)
         TrendUP = true; 
      if (StringFind(Money1,"CAD")==3)
         TrendDown = true; 
      if (StringFind(Money2,"USD")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"USD")==3)
         TrendDown2 = true; 
      if (StringFind(Money2,"CAD")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"CAD")==3)
         TrendDown2 = true;}   
   
   if (Auto==0) //������ ��������
   { 
       if((TrendUP && TrendDown) || (!TrendUP && !TrendDown)) //��������� ���������� �� ������(�) � �������� ������ �� 1-�� �������
       {
         if(TrendUP2) // ����������� ������ �2
         {
         Money2_SELLorBUY="Buy";
         Money1_SELLorBUY="Sell";
         }
         if(TrendDown2) // ����������� ������ �2
         {
         Money2_SELLorBUY="Sell";
         Money1_SELLorBUY="Buy";
         } 
       } 
       else
       {
         if(TrendUP) // ����������� ������ �1
         {
         Money1_SELLorBUY="Buy";
         Money2_SELLorBUY="Sell";
         }
         if(TrendDown) // ����������� ������ �1
         {
         Money1_SELLorBUY="Sell";
         Money2_SELLorBUY="Buy";
         }
       }  
     }

   if (Auto==1) //���������� ��������
   { 
       if((TrendUP && TrendDown) || (!TrendUP && !TrendDown)) //��������� ���������� �� ������(�) � �������� ������ �� 1-�� �������
       {
         if(TrendUP2) // ����������� ������ �2
         {
         Money2_SELLorBUY="Buy";
         Money1_SELLorBUY="Buy";
         }
         if(TrendDown2) // ����������� ������ �2
         {
         Money2_SELLorBUY="Sell";
         Money1_SELLorBUY="Sell";
         } 
       } 
       else
       {
         if(TrendUP) // ����������� ������ �1
         {
         Money1_SELLorBUY="Buy";
         Money2_SELLorBUY="Buy";
         }
         if(TrendDown) // ����������� ������ �1
         {
         Money1_SELLorBUY="Sell";
         Money2_SELLorBUY="Sell";
         }
       }  
     }
if(!TrendUP && !TrendDown && !TrendUP2 && !TrendDown2)
CorrelationF=false;
}
///TRand (for Auto trading)
}}

//OpenOrder:
if (CorrelationF==true && (Monday1==strTime.day_of_week||Tuesday2==strTime.day_of_week||Wednesday3==strTime.day_of_week||Thursday4==strTime.day_of_week||Friday5==strTime.day_of_week))
{
if (ticket1==0 && ticket2==0)
{starttime = TimeCurrent(); //����� �������� �������
HistorySelect(starttime,TimeCurrent()); //������ ������� � ��� ������ �� ���� ��� ������� �������
Deals = HistoryDealsTotal();} //������ ������� � ��� ������ �� ���� ��� ������� �������
i=0;
  while (ticket1==0 && Try>i)
   {
   bid   =SymbolInfoDouble(Money1,SYMBOL_BID); // ������ �������� Bid
   ask   =SymbolInfoDouble(Money1,SYMBOL_ASK); // ������ �������� Ask
   point =SymbolInfoDouble(Money1,SYMBOL_POINT);//������ Point
   i =i+1;
   if (StringLen(Money1_SELLorBUY)==3)
   ticket1=MyOrderSend(Money1,ORDER_TYPE_BUY,Lot1,ask,3,bid-StopLoss*point,ask+TakeProfit*point,"",0,0,Blue);
   else
   ticket1=MyOrderSend(Money1,ORDER_TYPE_SELL,Lot1,bid,3,ask+StopLoss*point,bid-TakeProfit*point,"",0,0,Red);
   }
i=0;
   while (ticket2==0 && Try>i)
   {
   bid   =SymbolInfoDouble(Money2,SYMBOL_BID); // ������ �������� Bid
   ask   =SymbolInfoDouble(Money2,SYMBOL_ASK); // ������ �������� Ask
   point =SymbolInfoDouble(Money2,SYMBOL_POINT);//������ Point
   i =i+1;
   if (StringLen(Money2_SELLorBUY)==3)
   ticket2=MyOrderSend(Money2,ORDER_TYPE_BUY,Lot2,ask,3,bid-StopLoss*point,ask+TakeProfit*point,"",0,0,Blue);
   else
   ticket2=MyOrderSend(Money2,ORDER_TYPE_SELL,Lot2,bid,3,ask+StopLoss*point,bid-TakeProfit*point,"",0,0,Red);
   }
//}
//}

if (ticket1!=0 && ticket2!=0)
{
CorrelationF=false;
start=false;
order1=0;
order2=0;

//���� �������� �������(�)
int NeedDeals = Deals+2; //�������� - ������� ����� �������
while (Deals<NeedDeals)
{
Sleep(100);
HistorySelect(starttime,TimeCurrent());
Deals = HistoryDealsTotal();
}
//���� �������� �������(�)

   do //���� �������
   {
   Sleep(10);
   bid   =SymbolInfoDouble(Money1,SYMBOL_BID); // ������ �������� Bid
   ask   =SymbolInfoDouble(Money1,SYMBOL_ASK); // ������ �������� Ask
   if (StringLen(Money1_SELLorBUY)==3)
    {if (!OrderCalcProfit(ORDER_TYPE_BUY,Money1,Lot1,ticket1,bid,OrderProfit1))
    OrderProfit1=0;}
   else
    {if (!OrderCalcProfit(ORDER_TYPE_SELL,Money1,Lot1,ticket1,ask,OrderProfit1))
    OrderProfit1=0;}
   
   bid   =SymbolInfoDouble(Money2,SYMBOL_BID); // ������ �������� Bid
   ask   =SymbolInfoDouble(Money2,SYMBOL_ASK); // ������ �������� Ask
   if (StringLen(Money2_SELLorBUY)==3)
    {if (!OrderCalcProfit(ORDER_TYPE_BUY,Money2,Lot2,ticket2,bid,OrderProfit2))
    OrderProfit2=0;}
   else
    {if (!OrderCalcProfit(ORDER_TYPE_SELL,Money2,Lot2,ticket2,ask,OrderProfit2))
    OrderProfit2=0;}

      //����������� TakeProfit ��� StopLoss. ���� ����� ���� ���� �������� ������ �������.
      HistorySelect(starttime,TimeCurrent());
      ulong last_deal=HistoryDealGetTicket(HistoryDealsTotal()-1);
      string nSymbol;
      if (HistoryDealGetString(last_deal,DEAL_COMMENT,nTypeEntry) && HistoryDealGetString(last_deal,DEAL_SYMBOL,nSymbol))
         {if ((StringFind(nTypeEntry,"sl") !=-1 || StringFind(nTypeEntry,"tp") != -1) && nSymbol == Money1)
         {
            i=0;
            while (1)
            {
            Sleep(100);
            i=i+1;
            HistorySelect(starttime,TimeCurrent());
            if (MathMod(i,2)==0)
            last_deal=HistoryDealGetTicket(HistoryDealsTotal()-1);
            else
            last_deal=HistoryDealGetTicket(HistoryDealsTotal()-2);
            if (HistoryDealGetString(last_deal,DEAL_COMMENT,nTypeEntry) && HistoryDealGetString(last_deal,DEAL_SYMBOL,nSymbol))
            if ((StringFind(nTypeEntry,"sl") !=-1 || StringFind(nTypeEntry,"tp") != -1) && nSymbol == Money2)
            {order1 = true;
            order2 = true;
            break;}
            }
         break;
         }
         if ((StringFind(nTypeEntry,"sl") !=-1 || StringFind(nTypeEntry,"tp") != -1) && nSymbol == Money2)
         {
            i=0;
            while (1)
            {
            Sleep(100);
            i=i+1;
            HistorySelect(starttime,TimeCurrent());
            if (MathMod(i,2)==0)
            last_deal=HistoryDealGetTicket(HistoryDealsTotal()-1);
            else
            last_deal=HistoryDealGetTicket(HistoryDealsTotal()-2);
            if (HistoryDealGetString(last_deal,DEAL_COMMENT,nTypeEntry) && HistoryDealGetString(last_deal,DEAL_SYMBOL,nSymbol))
            if ((StringFind(nTypeEntry,"sl") !=-1 || StringFind(nTypeEntry,"tp") != -1) && nSymbol == Money1)
            {order1 = true;
            order2 = true;
            break;}
            }
         break;
         }}
      //����������� TakeProfit ��� StopLoss. ���� ����� ���� ���� �������� ������ �������.
    
   }
   while ((OrderProfit2+OrderProfit1)<Profit); //���� �������

ticket1=0;
ticket2=0;
if (order2==0 && order1==0)
Print(" > Double trading. �������: ", NormalizeDouble((OrderProfit2+OrderProfit1),2));
}
}

//TakeProfit:
i=0;
if ((OrderProfit2+OrderProfit1)>=Profit)
{
   while (order2==0 && Try>i)
   {
   bid   =SymbolInfoDouble(Money2,SYMBOL_BID); // ������ �������� Bid
   ask   =SymbolInfoDouble(Money2,SYMBOL_ASK); // ������ �������� Ask
   point =SymbolInfoDouble(Money2,SYMBOL_POINT);//������ Point
   if (StringLen(Money2_SELLorBUY)==3)
   order2=MyOrderSend(Money2,ORDER_TYPE_SELL,Lot2,bid,3,ask+StopLoss*point,bid-TakeProfit*point,"",0,0,Red);
   else
   order2=MyOrderSend(Money2,ORDER_TYPE_BUY,Lot2,ask,3,bid-StopLoss*point,ask+TakeProfit*point,"",0,0,Blue);
   i=i+1;
   }
 i=0;
   while (order1==0 && Try>i)
   {
   bid   =SymbolInfoDouble(Money1,SYMBOL_BID); // ������ �������� Bid
   ask   =SymbolInfoDouble(Money1,SYMBOL_ASK); // ������ �������� Ask
   point =SymbolInfoDouble(Money1,SYMBOL_POINT);//������ Point
   if (StringLen(Money1_SELLorBUY)==3)
   order1=MyOrderSend(Money1,ORDER_TYPE_SELL,Lot1,bid,3,ask+StopLoss*point,bid-TakeProfit*point,"",0,0,Red);
   else
   order1=MyOrderSend(Money1,ORDER_TYPE_BUY,Lot1,ask,3,bid-StopLoss*point,ask+TakeProfit*point,"",0,0,Blue);
   i=i+1;
   }   
}
//----
  }
//+------------------------------------------------------------------+