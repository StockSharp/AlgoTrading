//+------------------------------------------------------------------+
//|                                             FT_TrendFollower.mq4 |
//|                                                     FORTRADER.RU |
//|                                              http://FORTRADER.RU |
//| http://www.forexsystems.ru/showthread.php?p=6794                 |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU Система по TrendFollower"

extern string GMMA0="Настройка GMMA:";
extern int SrartGMMAPer=50; 
extern int EndGMMAPer=200; 
extern int CountLine=5; 

extern string EMA48="Настройка EMASIG4-8:";
extern int EMA4=4;
extern int EMA8=8;

extern string TradeBar="На каком баре работаем:";
extern int TradeShift=1;


extern string SL="Настройки СтопЛосса:";
extern int Stoploss1=1;
extern int Stoploss1Pips=5;
extern int Stoploss2=0;
extern int Stoploss2Pips=25;

extern string Q="Настройки Выхода:";
extern int Quit=1;//закрытие половины на опорной точке половины при смене линии на красную
extern int HMAPer=80;
extern int Quit1=0;
extern int Quit2=0;
extern int ChPer=34;



int nummodb,nummodbS;

double ggma[1000];
double ggmalast[1000];
int indexline,trend,EmaSigOk,CloseOk,LaguerreOk,EmaSigOkS,CloseOkS,LaguerreOkS;double ema4,ema8,Laguerre,dMacdMain;

int init(){return(0);}
int deinit(){return(0);}

int start()
{int controlgreen,controlvverh,controlvverhS;double sl;int i;
if(errorchek()!=""){Print(errorchek());return(0);}
    
    exit();
    GMMA();

if(Close[1]<ggma[indexline-1]){CloseOk=1;}
if(Close[1]>ggma[indexline-1]){CloseOkS=1;}


if(CloseOk==1 && Close[1]>ggma[0]){LaguerreOk=0;EmaSigOk=0;CloseOk=0;}
if(CloseOkS==1 && Close[1]<ggma[0]){LaguerreOkS=0;EmaSigOkS=0;CloseOkS=0;}
  
    //смотрим все ли зеленые выше красных 
    trend=2;
    if(ggma[indexline-(CountLine+CountLine)]>ggma[indexline-CountLine] )
    {
    trend=1;
    }
    if(ggma[indexline-(CountLine+CountLine)]<ggma[indexline-CountLine] )
    {
    trend=0;
    }
    
    
   Laguerre();
   if(trend==1 && Laguerre<0.15 && Close[1]>ggma[indexline-1]&& Low[1]<ggma[0]){LaguerreOk=1;}
   if(trend==0 && Laguerre>0.75 && Close[1]<ggma[indexline-1]&& High[1]>ggma[0]){LaguerreOkS=1;}
   EMASIG();
   if(trend==1 && ema4<ema8){EmaSigOk=1;}
   if(trend==0 && ema4>ema8){EmaSigOkS=1;}
 
    for( i=CountLine;i>=0;i--)
    {
    //посмотрим направленны все красные зеленые и желтые вверх
    if(ggma[indexline-(CountLine+i)]>ggmalast[indexline-(CountLine+i)]){controlvverh=controlvverh+1;}
    if(ggma[indexline-(CountLine+CountLine+i-1)]>ggmalast[indexline-(CountLine+CountLine+i-1)]){controlvverh=controlvverh+1;}
    if(ggma[i]>ggmalast[i]){controlvverh=controlvverh+1;}
    }
    for( i=CountLine;i>=0;i--)
    {
    //посмотрим направленны все красные зеленые и желтые вверх
    if(ggma[indexline-(CountLine+i)]<ggmalast[indexline-(CountLine+i)]){controlvverhS=controlvverhS+1;}
    if(ggma[indexline-(CountLine+CountLine+i-1)]<ggmalast[indexline-(CountLine+CountLine+i-1)]){controlvverhS=controlvverhS+1;}
    if(ggma[i]<ggmalast[i]){controlvverhS=controlvverhS+1;}
    }
    
    
    
    MACD();
    
    if(trend==1 && Laguerre>0.15 && LaguerreOk==1 && ema4>ema8 && EmaSigOk==1 && dMacdMain>0 && controlvverh>(indexline/2)  )
    {LaguerreOk=0;EmaSigOk=0;CloseOk=0;
    if(Stoploss1==1){sl=Low[1]-(Stoploss1Pips+MarketInfo(Symbol(),MODE_SPREAD))*Point; if(MathAbs(Ask-sl)<15*Point){sl=Ask-15*Point;}}
    if(Stoploss2==1){sl=Ask-Stoploss2Pips*Point;}
    OrderSend(Symbol(),OP_BUY,0.1,Ask,3,NormalizeDouble(sl,4),0,"FORTRADER.RU",1337,0,Green);
    nummodb=0;
    }
    
    if(trend==0 && Laguerre<0.75 && LaguerreOkS==1 && ema4<ema8 && EmaSigOkS==1 && dMacdMain<0 && controlvverhS>(indexline/2)  )
    {LaguerreOkS=0;EmaSigOkS=0;CloseOkS=0;
    if(Stoploss1==1){sl=High[1]+(Stoploss1Pips+MarketInfo(Symbol(),MODE_SPREAD))*Point; if(MathAbs(Bid-sl)<15*Point){sl=Bid+15*Point;}}
    if(Stoploss2==1){sl=Bid+Stoploss2Pips*Point;}
    OrderSend(Symbol(),OP_SELL,0.1,Bid,3,NormalizeDouble(sl,4),0,"FORTRADER.RU",1337,0,Green);
    nummodbS=0;
    }
  
return(0);
}

int MACD()
{
 dMacdMain = iMACD(Symbol(),NULL,5,35,5,PRICE_CLOSE,MODE_MAIN,1);
}

int EMASIG()
{
ema4=iMA(NULL,0,EMA4,0,MODE_EMA,PRICE_CLOSE,TradeShift);
ema8=iMA(NULL,0,EMA8,0,MODE_EMA,PRICE_CLOSE,TradeShift);
}

int Laguerre()
{
Laguerre=iCustom(Symbol(),NULL,"Laguerre",0,TradeShift);
}

int GMMA()
{indexline=0;
//вычислим общее количество линий
int cline=5*CountLine;
//вычислим шаг линий
int shagline=(EndGMMAPer-SrartGMMAPer)/cline;

//получим значения линий линии

for(int per=SrartGMMAPer;per<=EndGMMAPer;per=per+shagline)
{
ggma[indexline]=iMA(NULL,0,per,0,MODE_EMA,PRICE_CLOSE,TradeShift);
ggmalast[indexline]=iMA(NULL,0,per,0,MODE_EMA,PRICE_CLOSE,TradeShift+1);
indexline=indexline+1;
}

}

string errorchek()
{
if(SrartGMMAPer>EndGMMAPer){return("Ошибка! SrartGMMAPer не может быть больше EndGMMAPer");}
if(SrartGMMAPer==EndGMMAPer){return("Ошибка! SrartGMMAPer не может быть равно EndGMMAPer");}
if(CountLine==0){return("Ошибка! CountLine не может быть 0");}
if(CountLine>10){return("Ошибка! CountLine не может быть больше 10");}
if(CountLine>SrartGMMAPer){return("Ошибка! CountLine не может быть больше SrartGMMAPer");}
if(CountLine>EndGMMAPer){return("Ошибка! CountLine не может быть больше EndGMMAPer");}
if(EMA4>EMA8){return("Ошибка! EMA4 не может быть больше EMA8");}
if(Stoploss1==1 && Stoploss2==1){return("Ошибка! StopLoss не может быть влючено два варианта одновременно");}
if(TradeShift>1 || Stoploss1>1 || Stoploss2>1 || Quit>1 || Quit1>1 || Quit2>1){return("Ошибка! значения TradeShift>1 || Stoploss1>1 || Stoploss2>1 || Quit>1 Quit1>1 || Quit2>1 не могут быть больше 1");}
if(Quit>0 && Quit1>0 || Quit2>0){return("Ошибка! Вариант стоп лосса может быть только один");}
if(Quit1>0 && Quit>0 || Quit2>0){return("Ошибка! Вариант стоп лосса может быть только один");}
if(Quit2>0 && Quit>0 || Quit1>0){return("Ошибка! Вариант стоп лосса может быть только один");}

return("");
}

int exit()
{
double Pivot= (iHigh(Symbol(),1440,1) + iLow(Symbol(),1440,1) +  iClose(Symbol(),1440,1))/3;
double support1 = (2 * Pivot) - iHigh(Symbol(),1440,1);
double resist1 = (2 * Pivot) - iLow(Symbol(),1440,1);
double support2 = Pivot - (iHigh(Symbol(),1440,1) - iLow(Symbol(),1440,1));
double resist2 = Pivot + (iHigh(Symbol(),1440,1) - iLow(Symbol(),1440,1));

double hma=iCustom(Symbol(),0,"HMA",HMAPer,0,TradeShift);
double hma1=iCustom(Symbol(),0,"HMA",HMAPer,1,TradeShift);
double hma2=iCustom(Symbol(),0,"HMA",HMAPer,2,TradeShift);

double vltUP=iMA(NULL,0,ChPer,0,MODE_SMA,PRICE_HIGH,TradeShift);
double vltDW=iMA(NULL,0,ChPer,0,MODE_SMA,PRICE_LOW,TradeShift);

Comment("hma " ,hma, " hma1 " ,hma1," hma2 " ,hma2);


for( int i=1; i<=OrdersTotal(); i++)          
     {
     /*закрытие по пивоту и супер линии-------------------------------------------------------------------------------*/
      if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
         if(OrderType()==OP_BUY && Bid>resist1 && OrderProfit()>0  && nummodb==0 && OrderSymbol()==Symbol() && Quit==1)
         {  
          OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/2,2),Bid,3,Violet); 
          nummodb++;
         }
        }
        
       if (OrderSelect(i-1,SELECT_BY_POS)==true) // Если есть следующий
        {                                       // А
         if(OrderType()==OP_BUY && hma1!=EMPTY_VALUE && OrderProfit()>0 && OrderSymbol()==Symbol() && nummodb==1 && Quit==1)
         {  
         OrderClose(OrderTicket(),NormalizeDouble(OrderLots(),2),Bid,3,Violet); 
         nummodb++;
         }
        }
        /*закрытие по пивоту -------------------------------------------------------------------------------*/
              if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
         if(OrderType()==OP_BUY && Bid>resist1 && OrderProfit()>0  && nummodb==0 && OrderSymbol()==Symbol() && Quit1==1)
         {  
          OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/2,2),Bid,3,Violet); 
          nummodb++;
         }
        }
        
       if (OrderSelect(i-1,SELECT_BY_POS)==true) // Если есть следующий
        {                                       // А
         if(OrderType()==OP_BUY && Bid>resist2 && OrderProfit()>0 && OrderSymbol()==Symbol() && nummodb==1 && Quit1==1)
         {  
         OrderClose(OrderTicket(),NormalizeDouble(OrderLots(),2),Bid,3,Violet); 
         nummodb++;
         }
        }
        /*закрытие по пивоту и каналу волатильности-------------------------------------------------------------------------------*/ 
                     if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
         if(OrderType()==OP_BUY && Bid>resist1 && OrderProfit()>0  && nummodb==0 && OrderSymbol()==Symbol() && Quit2==1)
         {  
          OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/2,2),Bid,3,Violet); 
          nummodb++;
         }
        }
        
       if (OrderSelect(i-1,SELECT_BY_POS)==true) // Если есть следующий
        {                                       // А
         if(OrderType()==OP_BUY && Open[1]<vltDW && OrderProfit()>0 && OrderSymbol()==Symbol() && nummodb==1 && Quit2==1)
         {  
         OrderClose(OrderTicket(),NormalizeDouble(OrderLots(),2),Bid,3,Violet); 
         nummodb++;
         }
        }
        
        ///////////////////////////ПРОДАЖИ/////////////////////////////////////////////////////////////////////////////////////////////////////
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
         if(OrderType()==OP_SELL && Ask<support1 && OrderProfit()>0  && nummodbS==0 && OrderSymbol()==Symbol() && Quit==1)
         {  
          OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/2,2),Ask,3,Violet); 
          nummodbS++;
         }
        }
        
       if (OrderSelect(i-1,SELECT_BY_POS)==true) // Если есть следующий
        {                                       // А
         if(OrderType()==OP_SELL && hma!=EMPTY_VALUE && OrderProfit()>0 && OrderSymbol()==Symbol() && nummodbS==1 && Quit==1)
         {  
         OrderClose(OrderTicket(),NormalizeDouble(OrderLots(),2),Ask,3,Violet); 
         nummodbS++;
         }
        }
        /*закрытие по пивоту -------------------------------------------------------------------------------*/
       if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
         if(OrderType()==OP_BUY && Ask<support1 && OrderProfit()>0  && nummodbS==0 && OrderSymbol()==Symbol() && Quit1==1)
         {  
          OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/2,2),Ask,3,Violet); 
          nummodbS++;
         }
        }
        
       if (OrderSelect(i-1,SELECT_BY_POS)==true) // Если есть следующий
        {                                       // А
         if(OrderType()==OP_BUY && Ask<support2 && OrderProfit()>0 && OrderSymbol()==Symbol() && nummodbS==1 && Quit1==1)
         {  
         OrderClose(OrderTicket(),NormalizeDouble(OrderLots(),2),Ask,3,Violet); 
         nummodbS++;
         }
        }
        /*закрытие по пивоту и каналу волатильности-------------------------------------------------------------------------------*/ 
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
         if(OrderType()==OP_BUY && Ask<support1 && OrderProfit()>0  && nummodbS==0 && OrderSymbol()==Symbol() && Quit2==1)
         {  
          OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/2,2),Ask,3,Violet); 
          nummodbS++;
         }
        }
        
       if (OrderSelect(i-1,SELECT_BY_POS)==true) // Если есть следующий
        {                                       // А
         if(OrderType()==OP_BUY && Open[1]>vltUP && OrderProfit()>0 && OrderSymbol()==Symbol() && nummodbS==1 && Quit2==1)
         {  
         OrderClose(OrderTicket(),NormalizeDouble(OrderLots(),2),Ask,3,Violet); 
         nummodbS++;
         }
        }
      }

return(0);
}