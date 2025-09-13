//+------------------------------------------------------------------+
//| fullTrading.mq4 | optimised setting on my website
//| Christopher |
//| http://expert-advisor.webs.com
//+------------------------------------------------------------------+
/*
modChrisLBX

v1.1
- Ajout de l'augmentation du SL lorsqu'un seuil défini est atteint pour diminuer le drawdown max.

v1.0
- Débuggage de l'EA original, il y avait des erreurs à la création, modification et fermeture des ordres.
- Ajout d'un filtre permettant de régler l'EA pour un sens de trading à la fois.
- Ajout du MaxSpread.
- Le magic number est mis en variable paramétrable.
*/

#property copyright "Christopher"
#property link "http://www.expert-advisor.webs.com"



extern string rem1="//---- Definition Trend et Breakout";
extern int BreakoutPeriode=1;
extern int TrendPeriode=3;
extern string rem2="//---- Order";
extern bool OnlyShort=false;
extern bool OnlyLong=false;
extern int MaxTradesPerBar=1;
extern int StopLoss=40;
extern int BreakEven=30;
extern int SeuilSL = 330;
extern int PartialTP1=550;
extern int PartialRatio1=30;
extern int PartialTP2=1100;
extern int PartialRatio2=70;
extern int TakeProfit=1500;
extern int Slippage=30;
extern int MaxSpread=22;
extern int Magic=12387;
extern string rem3="//---- MM: 1.Fixed 2.Geometrical 3.Proportional 4.Smart 5.TSSF";
extern int MMType=3;
extern double FixedLots=0.1;
extern int GeometricalFactor=3;
extern int ProportionalRisk=90;
extern int LastXTrades=10;
extern double DecreaseFactor=2;
extern double TSSFTrigger1=1;
extern int TSSFRatio1=50;
extern double TSSFTrigger2=2;
extern int TSSFRatio2=75;
extern double TSSFTrigger3=3;
extern int TSSFRatio3=100;
extern string rem4="//---- Debug";
extern bool DebugMode=false;

double Lots;
bool SignalBuy=false;
bool SignalSell=false;
double highrange, lowrange;
double trend, trend1, signaltrend;
datetime LastTradeTime=0;
int MaxTrades=false;
int FlagPartial=0;
int OrdersThisBar;
int orders;
int losses;
int nbDecimales;
int i;

int tradegagnant,tradeperdant;
double profit, perte, avgwin, avgloss, prcwin, tssf;

bool TradingRange;

//+------------------------------------------------------------------+
//| expert initialization function |
//+------------------------------------------------------------------+
int init()
{
//----
nbDecimales = 0;
double tempPoint = Point;
while(tempPoint < 1) {
tempPoint *= 10;
nbDecimales++;
}
//----
return(0);
}
//+------------------------------------------------------------------+
//| expert deinitialization function |
//+------------------------------------------------------------------+
int deinit()
{
//----

//----
return(0);
}
//+------------------------------------------------------------------+
//| expert start function |
//+------------------------------------------------------------------+
int start()
{
//----
if (OrdersTotal()>0) OrdersList();
if (OrdersTotal()<1) FlagPartial=0;

highrange=High[iHighest(NULL,0,MODE_HIGH,BreakoutPeriode,1)];
lowrange=Low[iLowest(NULL,0,MODE_LOW,BreakoutPeriode,1)];

trend=iMA(NULL,0,TrendPeriode,0,MODE_SMMA,PRICE_MEDIAN,0);
trend1=iMA(NULL,0,TrendPeriode,0,MODE_SMMA,PRICE_MEDIAN,1);
signaltrend=trend-trend1;

if (Time[0]==LastTradeTime&&OrdersThisBar>=MaxTradesPerBar)
{MaxTrades=true;}
else
{MaxTrades=false;}
if (Time[0]!=LastTradeTime)
{OrdersThisBar=0;}

if (Close[0]==High[0]
&&Close[0]>highrange
&&signaltrend>0
&&OrdersTotal()<1
&&MaxTrades==false
&&Ask-Bid<=MaxSpread*Point
&&!OnlyShort)
SignalBuy=true;
if (Close[0]==Low[0]
&&Close[0]<lowrange
&&signaltrend<0
&&OrdersTotal()<1
&&MaxTrades==false
&&Ask-Bid<=MaxSpread*Point
&&!OnlyLong)
SignalSell=true;


//+------------------------------------------------------------------+
//| Money Management |
//+------------------------------------------------------------------+

if (SignalBuy==true||SignalSell==true)
{
if (MMType==1&&OrdersTotal()<1)Lots=FixedLots;
if (MMType==2&&OrdersTotal()<1)Lots=0.1*MathSqrt(AccountBalance()/1000)*GeometricalFactor;
if (MMType==3&&OrdersTotal()<1)Lots=AccountEquity()/Close[0]/1000*ProportionalRisk/100;

if (MMType==4&&OrdersTotal()<1)
{orders=HistoryTotal(); // Historique des ordres
losses=0; // Nombre de trade perdants consécutifs
Lots=NormalizeDouble(AccountFreeMargin()*ProportionalRisk/10000,1);
for(int i=orders-1;i>=orders-LastXTrades;i--)
{
if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==False) 
{ 
Print("Erreur dans l historique!"); 
break; 
}
if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) 
continue;
//----
if(OrderProfit()>0) 
break;
if(OrderProfit()<0) 
losses++;
}
if(losses>1) 
Lots=NormalizeDouble(Lots-Lots*losses/DecreaseFactor,1);
}

if (MMType==5&&OrdersTotal()<1)
{
orders=HistoryTotal();
tradegagnant=0;
tradeperdant=0;
profit=0;
perte=0;
avgwin=0;
avgloss=0;
prcwin=0;
tssf=0;
for(int j=orders-1;j>=orders-LastXTrades;j--)
{
if(OrderSelect(j,SELECT_BY_POS,MODE_HISTORY)==False) 
{ 
Print("Erreur dans l historique!"); 
break; 
}

if(OrderProfit()>=0)
{
tradegagnant++;
profit=profit+OrderProfit();
}
else
{
tradeperdant++;
perte=perte+OrderProfit();
}


}
if (orders>LastXTrades)avgwin=profit/tradegagnant;
if (orders>LastXTrades)avgloss=perte/tradeperdant;
if (orders>LastXTrades)prcwin=tradegagnant/(tradegagnant+tradeperdant);
if (orders>LastXTrades)tssf=avgwin/avgloss*((1.1-prcwin)/(prcwin-0.1)+1);

if(tssf<=TSSFTrigger1)Lots=0.1;
if(tssf>TSSFTrigger1&&tssf<=TSSFTrigger2)Lots=NormalizeDouble(AccountFreeMargin()*ProportionalRisk/TSSFRatio1*100/100000,1);
if(tssf>TSSFTrigger2&&tssf<=TSSFTrigger3)Lots=NormalizeDouble(AccountFreeMargin()*ProportionalRisk/TSSFRatio2*100/100000,1);
if(tssf>TSSFTrigger3)Lots=NormalizeDouble(AccountFreeMargin()*ProportionalRisk/TSSFRatio3*100/100000,1);
}

Lots = arrondirLots(Lots);
double minLot = MarketInfo(Symbol(), MODE_MINLOT);
double maxLot = MarketInfo(Symbol(), MODE_MAXLOT);
if (Lots<minLot)Lots=minLot;
if (Lots>maxLot)Lots=maxLot;
}

//+------------------------------------------------------------------+
//| Passage des ordres |
//+------------------------------------------------------------------+
if (SignalBuy==true)
{
OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,Ask-StopLoss*Point,Ask+TakeProfit*Point,"MindFulNess",Magic,Blue);
SignalBuy=false;
LastTradeTime=Time[0];
OrdersThisBar++;
}
if (SignalSell==true)
{ 
OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,Bid+StopLoss*Point,Bid-TakeProfit*Point,"MindFulNess",Magic,Red);
SignalSell=false;
LastTradeTime=Time[0];
OrdersThisBar++;
}

if (DebugMode==true)
{
Comment (
"OboTrading, @2009 Christophe Sangouard",
"\nStopLoss="+StopLoss*Point,
"\nStopLevel="+DoubleToStr(MarketInfo(Symbol(), MODE_STOPLEVEL),0),
"\nSpread= "+DoubleToStr(MarketInfo(Symbol(),MODE_SPREAD),0),
"\nLastError="+GetLastError()
);
}

//----
return(0);
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| expert deinitialization function |
//+------------------------------------------------------------------+

void OrdersList()
{ 
for (int cnt=0;cnt<OrdersTotal();cnt++)
{ 
OrderSelect(cnt, SELECT_BY_POS); 
if (OrderSymbol()==Symbol()&&OrderMagicNumber()==Magic) 
{
if (OrderType()==OP_BUY)
{
//---- BreakEven
if (BreakEven>0
&&Bid-OrderOpenPrice()>Point*BreakEven
&&NormalizeDouble(OrderStopLoss(),nbDecimales)<NormalizeDouble(OrderOpenPrice(),nbDecimales)) 
{
OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,LightGreen);
return(0);
}
//---- Augmentation du SL au delà de zéro
if (SeuilSL>0
&&Bid-OrderOpenPrice()>Point*SeuilSL
&&NormalizeDouble(OrderStopLoss(),nbDecimales)<NormalizeDouble(OrderOpenPrice()+StopLoss*Point,nbDecimales)) 
{
OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()+StopLoss*Point,nbDecimales),OrderTakeProfit(),0,LightGreen);
return(0);
}
//---- PartialTP1
if (Bid-OrderOpenPrice()>Point*PartialTP1&&FlagPartial==0&&PartialTP1>0&&OrderLots()*PartialRatio1*PartialRatio2/10000>=0.1) 
{
OrderClose(OrderTicket(),arrondirLots(OrderLots()*PartialRatio1/100),Bid,Slippage,Red);
FlagPartial=1;
return(0);
}
//---- PartialTP2
if (Bid-OrderOpenPrice()>Point*PartialTP2&&FlagPartial==1&&PartialTP2>0) 
{
OrderClose(OrderTicket(),arrondirLots(OrderLots()*PartialRatio2/100),Bid,Slippage,Red);
FlagPartial=2;
return(0);
}
}
if (OrderType()==OP_SELL)
{
//---- BreakEven
if (BreakEven>0
&&OrderOpenPrice()-Ask>Point*BreakEven
&&NormalizeDouble(OrderStopLoss(),nbDecimales)>NormalizeDouble(OrderOpenPrice(),nbDecimales)) 
{
OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Yellow); 
return(0);
}
//---- Augmentation du SL au delà de zéro
if (SeuilSL>0
&&OrderOpenPrice()-Ask>Point*SeuilSL
&&NormalizeDouble(OrderStopLoss(),nbDecimales)>NormalizeDouble(OrderOpenPrice()-StopLoss*Point,nbDecimales)) 
{
OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()-StopLoss*Point,nbDecimales),OrderTakeProfit(),0,LightGreen);
return(0);
}
//---- PartialTP1
if (OrderOpenPrice()-Ask>Point*PartialTP1&&FlagPartial==0&&PartialTP1>0&&OrderLots()*PartialRatio1*PartialRatio2/10000>=0.1) 
{
OrderClose(OrderTicket(),arrondirLots(OrderLots()*PartialRatio1/100),Ask,Slippage,Red);
FlagPartial=1;
return(0);
}
//---- PartialTP2
if (OrderOpenPrice()-Ask>Point*PartialTP2&&FlagPartial==1&&PartialTP2>0) 
{
OrderClose(OrderTicket(),arrondirLots(OrderLots()*PartialRatio2/100),Ask,Slippage,Red);
FlagPartial=2;
return(0);
}
}
} 
} 
}

double arrondirLots(double nbLots) {
double lotStep = MarketInfo(Symbol(), MODE_LOTSTEP);
double tempDouble = nbLots + lotStep/2;
tempDouble /= lotStep;
int tempInt = tempDouble;
return (tempInt*lotStep);
}