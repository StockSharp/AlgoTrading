//+------------------------------------------------------------------+
//|                                                    ProtoType.mq4 |
//|                                                             Rosh |
//|                    http://www.alpari-idc.ru/ru/experts/articles/ |
//+------------------------------------------------------------------+
#property copyright "Rosh"
#property link      "http://www.alpari-idc.ru/ru/experts/articles/"

extern int     EMN=10000; //Expert Magic number
extern int     Slippage=3;
extern double  RiskDelta=5.0; // риск в процентах

extern int     PeriodWPR=8;
extern double  CriteriaWPR=25;
extern int     ATRPeriod=40;// период ATR для индикатора 
extern double  kATR=0.5;
extern int     ZeroBar=8; // выход в безубыток через ZeroBar баров
extern double  MinTargetinSpread=5.0;
extern double  TP_SL_Criteria=2.0;
extern int     MaxOpenedOrders=3; 
extern double  MaxOrderSize=5.0;

string SymbolsArray[13]={"","USDCHF","GBPUSD","EURUSD","USDJPY","AUDUSD","USDCAD","EURGBP","EURAUD","EURCHF","EURJPY","GBPJPY","GBPCHF"};
int TrendOnSymbol[13,7]; //  тренд по символу и таймфрейму
int MyBarsArrays[13,7];// храним количество баров по инструменту и таймфрейму
int TimeNullArrays[13,7];// храним время Time[0] по инструменту и таймфрейму

double GatorTrend[13,7];// тренд по каждому символу и тайм-фрейму из индикатора NRTR-GATOR (Alligator)
double NRTR_Trend[13,7];//  тренд по каждому символу и тайм-фрейму из индикатора NRTR-GATOR (NRTR)
double Complextrend[13,7];// собираем все тренды (Z,A и N тренды) в одно значение.
double TPvsSL[13,7];// отношение TakeProfit к StopLoss на данном символе и таймфрейме 
int BestTPvsSLSymbol[20]; // лучшие символы по соотношению TP/SL
int BestTPvsSLPeriod[20]; // лучшие таймфреймы по соотношению TP/SL

int  LastUpArray[13,7];
int  PreLastUpArray[13,7];
int  LastDownArray[13,7];
int  PreLastDownArray[13,7];

int eurIndex[5]={3,7,8,9,10};
int gbpIndex[4]={2,-7,11,12};
int chfIndex[3]={1,9,12};
int jpyIndex[3]={4.10,11};
int usdIndex[5]={1,-2,4,5,6};
int TestingIndexPeriod=0;
int TestingIndexSymbol=0;
datetime UP1Time,UP2Time,Down1Time,Down2Time;
double UP1Price,UP2Price,Down1Price,Down2Price;


//+------------------------------------------------------------------+
//| сопровождение ордера на тайм-фрейме номер  period_Count          |
//+------------------------------------------------------------------+
int GetMagicNumber(int period_Count)
   {
   int res=EMN+PeriodNumber(period_Count);
   return(res);
   }


//+------------------------------------------------------------------+
//| string SymbolByNumber                                   |
//+------------------------------------------------------------------+
string GetSymbolString(int Number)
  {
//----
   string res="";
   res=SymbolsArray[Number];   
//----
   return(res);
  }


//+------------------------------------------------------------------+
//| Очень простая функция расчета маржи для форексных символов.      |
//| Расчет автоматически идет в базовой валюте счета и не работает   |
//| для сложных видов курсов, которые не имеют прямого пересчета     |
//| в базовую валюту торгового счета.                                |
//+------------------------------------------------------------------+
double MarginCalculate(string symbol,double volume)
  {
   string first   =StringSubstr(symbol,0,3);         // первый символ,    например EUR
   string second  =StringSubstr(symbol,3,3);         // второй символ,    например USD
   string currency=AccountCurrency();                // валюта депозита,  например USD
   double leverage=AccountLeverage();                // кредитное плечо,  например 100
   double contract=MarketInfo(symbol,MODE_LOTSIZE);  // размер контракта, например 100000
   double bid     =MarketInfo(symbol,MODE_BID);      // цена бид
//---- допускаем только стандартные форексные символы XXXYYY
   if(StringLen(symbol)!=6)
     {
      Print("MarginCalculate: '",symbol,"' must be standard forex symbol XXXYYY");
      return(0.0);
     }
//---- проверка наличия данных
   if(bid<=0 || contract<=0) 
     {
      Print("MarginCalculate: no market information for '",symbol,"'");
      return(0.0);
     }
//---- проверяем самые простые варианты - без кроссов
   if(first==currency)   return(contract*volume/leverage);           // USDxxx
   if(second==currency)  return(contract*bid*volume/leverage);       // xxxUSD
//---- проверяем обычные кроссы, ищем прямое преобразование через валюту депозита
   string base=currency+first;                                       // USDxxx
   if(MarketInfo(base,MODE_BID)>0) return(contract/MarketInfo(base,MODE_BID)*volume/leverage);
//---- попробуем наоборот
   base=first+currency;                                              // xxxUSD
   if(MarketInfo(base,MODE_BID)>0) return(contract*MarketInfo(base,MODE_BID)*volume/leverage);
//---- нет возможности прямого перерасчета
   Print("MarginCalculate: can not convert '",symbol,"'");
   return(0.0);
  }
/*
//+------------------------------------------------------------------+
//|подтягивает стоп по параболе                                      |
//+------------------------------------------------------------------+
void ParabolTrailingStop()
  {
  int type;
  int EnterBar;// бар входа в позицию
  double a; // ускорение в форумуле S=a*t^2/2
  double Zbar=ZeroBar,CurrParStop,TSdouble=TSpar;
  double minStopLevel=MarketInfo(Symbol(),MODE_STOPLEVEL);
  a=TSdouble*2./Zbar/Zbar;
//----
   for (int i=0;i<OrdersTotal();i++)
      {
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
         {
         type=OrderType();
         if (type==OP_BUY) 
            {
            EnterBar=iBarShift(NULL,0,OrderOpenTime());
            if (EnterBar==0) continue;
            CurrParStop=NormalizeDouble(OrderOpenPrice()-(TSpar-a*EnterBar*EnterBar/2.0)*Point,Digits);
            if (CurrParStop>OrderStopLoss()) 
               {
               if (Bid-CurrParStop>minStopLevel*Point)OrderModify(OrderTicket(),OrderOpenPrice(),
               CurrParStop,OrderTakeProfit(),0,Blue);
               
               else OrderModify(OrderTicket(),OrderOpenPrice(),Bid-minStopLevel*Point,OrderTakeProfit(),0,Blue);
               }
            }
         if (type==OP_SELL) 
            {
            EnterBar=iBarShift(NULL,0,OrderOpenTime());
            if (EnterBar==0) continue;
            CurrParStop=NormalizeDouble(OrderOpenPrice()+(TSpar-a*EnterBar*EnterBar/2.0)*Point,Digits);
            if (CurrParStop<OrderStopLoss()) 
               {
               if (CurrParStop-Ask>minStopLevel*Point)OrderModify(OrderTicket(),OrderOpenPrice(),
               CurrParStop,OrderTakeProfit(),0,Red);
               
               else OrderModify(OrderTicket(),OrderOpenPrice(),Ask+minStopLevel*Point,OrderTakeProfit(),0,Red);
               }
            }
         }
      }
//----
   return;
  }

*/
//+------------------------------------------------------------------+
//| возвращает период                                                |
//+------------------------------------------------------------------+
int PeriodNumber(int number)
   {
   int per_min;
   switch (number)
      {
      case 0: per_min=PERIOD_M1;break;
      case 1: per_min=PERIOD_M5;break;
      case 2: per_min=PERIOD_M15;break;
      case 3: per_min=PERIOD_M30;break;
      case 4: per_min=PERIOD_H1;break;
      case 5: per_min=PERIOD_H4;break;
      default: per_min=PERIOD_D1;break;
      }
   return(per_min);   
   }

//+------------------------------------------------------------------+
//| признак появления нового бара на периоде номер  period_counter   |
//+------------------------------------------------------------------+
bool isNewBar(int SymbolNumber,int period_counter)
   {
   bool res=false;
   if (IsTesting())
      {
      if (MyBarsArrays[SymbolNumber,period_counter]!=iBars(GetSymbolString(SymbolNumber),PeriodNumber(period_counter)))
         {
         MyBarsArrays[SymbolNumber,period_counter]=iBars(GetSymbolString(SymbolNumber),PeriodNumber(period_counter));
         //Print("Код ошибки в isNewBar=",GetLastError());
         //Print("isNewBar  SymbolNumber=",SymbolNumber,"  period_counter=",period_counter," MyBarsArrays[SymbolNumber,period_counter]=",MyBarsArrays[SymbolNumber,period_counter],"  iBars(GetSymbolString(SymbolNumber),PeriodNumber(period_counter))=",iBars(GetSymbolString(SymbolNumber),PeriodNumber(period_counter))   );
         res=true;
         }
      }
   else
      {
      if (TimeNullArrays[SymbolNumber,period_counter]!=iTime(GetSymbolString(SymbolNumber),PeriodNumber(period_counter),0))
         {
         TimeNullArrays[SymbolNumber,period_counter]=iTime(GetSymbolString(SymbolNumber),PeriodNumber(period_counter),0);
         res=true;
         }
      }
   return(res);   
   }

  
//+------------------------------------------------------------------+
//| определение тренда по четырем последним экстремумам              |
//+------------------------------------------------------------------+
int TrendByWPR(int SymbolNumber,int period_counter)
  {
//----
   int res=0;
   string StringSymbol=GetSymbolString(SymbolNumber); 
   int PeiodMinute=PeriodNumber(period_counter);
   int curPos,LastUpPos,PreLastUpPos,LastDownPos,PreLastDownPos,LastPeak,newPos;
   double LastPeakWPR=-1000;
   bool FindUp=true,FindDown=true,SearchCompleted=false;
   double CurWPR=iWPR(StringSymbol,PeiodMinute,PeriodWPR,0);
//----
   //=======  определим - где мы находимся в данный момент
   if (CurWPR<=CriteriaWPR-100)
      {
      FindDown=false;
      LastPeak=0;
      }   
   if (CurWPR>=-CriteriaWPR)
      {
      FindUp=false;
      LastPeak=0;
      }   
   // ================   начианем поиск пичков-донышков
   while(!SearchCompleted && curPos<Bars)
      {
      if (iWPR(StringSymbol,PeiodMinute,PeriodWPR,curPos)>=-CriteriaWPR && LastPeak<0)
         {
         FindUp=false;
         LastPeak=curPos;
         curPos++;
         continue;
         }
         
      if (iWPR(StringSymbol,PeiodMinute,PeriodWPR,curPos)<=CriteriaWPR-100 && LastPeak<0)
         {
         FindDown=false;
         LastPeak=curPos;
         curPos++;
         continue;
         }
         
      if (iWPR(StringSymbol,PeiodMinute,PeriodWPR,curPos)>=-CriteriaWPR && FindUp)
         {//искали верхушку и нашли
         newPos=curPos; 
         while(iWPR(StringSymbol,PeiodMinute,PeriodWPR,curPos)>CriteriaWPR-100 && curPos<Bars)
            {// теперь нужно найти донышко, чтобы между ними найти точный пичок
            curPos++;
            }
         if (LastUpPos==0) 
            {
            LastUpPos=Highest(StringSymbol,PeiodMinute,MODE_HIGH,curPos-LastPeak,LastPeak);   
            LastPeak=LastUpPos;
            }
         else 
            {
            PreLastUpPos=Highest(StringSymbol,PeiodMinute,MODE_HIGH,curPos-LastPeak,LastPeak);
            LastPeak=PreLastUpPos;
            }
         curPos=newPos;
         FindUp=false;
         FindDown=true;
         curPos++;
         continue;
         }//==============

      if (iWPR(StringSymbol,PeiodMinute,PeriodWPR,curPos)<=CriteriaWPR-100 && FindDown)
         {
         newPos=curPos; 
         while(iWPR(StringSymbol,PeiodMinute,PeriodWPR,curPos)<-CriteriaWPR && curPos<Bars)
            {
            curPos++;
            }
         if (LastDownPos==0) 
            {
            LastDownPos=Lowest(StringSymbol,PeiodMinute,MODE_LOW,curPos-LastPeak,LastPeak);
            LastPeak=LastDownPos;
            }   
         else 
            {
            PreLastDownPos=Lowest(StringSymbol,PeiodMinute,MODE_LOW,curPos-LastPeak,LastPeak);
            LastPeak=PreLastDownPos;
            }
         curPos=newPos;
         FindDown=false;
         FindUp=true;
         curPos++;
         continue;
         }
      if (PreLastDownPos!=0 && PreLastUpPos!=0) SearchCompleted=true;
      curPos++;
      }
   if (Symbol()==StringSymbol && Period()==PeiodMinute)
      {
      Comment("LastUpPos=",LastUpPos,"  PreLastUpPos",PreLastUpPos,"   LastDownPos=",LastDownPos,"  PreLastDownPos=",PreLastDownPos," Время ",TimeToStr(CurTime()));
      SetUpArrows(LastUpPos,PreLastUpPos,LastDownPos,PreLastDownPos);
      }
   LastUpArray[SymbolNumber,period_counter]=LastUpPos;   
   PreLastUpArray[SymbolNumber,period_counter]=PreLastUpPos;   
   LastDownArray[SymbolNumber,period_counter]=LastDownPos;   
   PreLastDownArray[SymbolNumber,period_counter]=PreLastDownPos;   
   if (High[LastUpPos]-High[PreLastUpPos]>=kATR*iATR(StringSymbol,PeiodMinute,ATRPeriod,LastUpPos)&&Low[LastDownPos]>Low[PreLastDownPos]) res=1;     
   if (Low[PreLastDownPos]-Low[LastDownPos]>=kATR*iATR(StringSymbol,PeiodMinute,ATRPeriod,LastDownPos)&&High[PreLastUpPos]>High[LastUpPos]) res=-1;     

   return(res);
  }

//+------------------------------------------------------------------+
//| поставим стрелку                                                 |
//+------------------------------------------------------------------+
void SetArrow(datetime _time,double _price,string _Description ,int _arrowType, color _arrowColor)
   {
   if (ObjectFind(_Description)==-1) 
      {
      ObjectCreate(_Description,OBJ_ARROW,0,_time,_price);
      ObjectSet(_Description,OBJPROP_ARROWCODE,_arrowType);
      ObjectSet(_Description,OBJPROP_COLOR,_arrowColor);
      }
   else
      {
      ObjectSet(_Description,OBJPROP_TIME1,_time);
      ObjectSet(_Description,OBJPROP_PRICE1,_price);
      }     
   return;
   }

//+------------------------------------------------------------------+
//| установить стрелки экстермумов                                   |
//+------------------------------------------------------------------+
void SetUpArrows(int firstUpBar, int secondUpBar,int firstDownBar, int secondDownBar)
  {
//----
   SetArrow(Time[firstUpBar],High[firstUpBar],"FirstUp",241,Blue);
   SetArrow(Time[secondUpBar],High[secondUpBar],"SecondUp",241,Blue);
   SetArrow(Time[firstDownBar],Low[firstDownBar],"FirstDown",242,Red);
   SetArrow(Time[secondDownBar],Low[secondDownBar],"SecondDown",242,Red);
//----
   return(0);
  }
  
//+------------------------------------------------------------------+
//| Проверить наличие тренда                                         |
//+------------------------------------------------------------------+
bool TrendExist()
  {
  bool res=false;
  double TP,SL,Spread,trend;
  double target,support,SymbolPoint;
  int listCounter;
//----
   for (int SymbolIndex=1;SymbolIndex<13;SymbolIndex++)
      {
      for (int tf=2;tf<5;tf++)
         {
         if (Complextrend[SymbolIndex,tf]*Complextrend[SymbolIndex,tf+2]==1 || Complextrend[SymbolIndex,tf]*Complextrend[SymbolIndex,tf+3]==1) 
            {
            trend=Complextrend[SymbolIndex,tf];
            if (trend==1.0)
               {
               target=MathMax(iHigh(GetSymbolString(SymbolIndex),PeriodNumber(tf),LastUpArray[SymbolIndex,tf]),iHigh(GetSymbolString(SymbolIndex),PeriodNumber(tf),PreLastUpArray[SymbolIndex,tf]));
               support=MathMax(iLow(GetSymbolString(SymbolIndex),PeriodNumber(tf),LastDownArray[SymbolIndex,tf]),iLow(GetSymbolString(SymbolIndex),PeriodNumber(tf),PreLastDownArray[SymbolIndex,tf]));
               TP=target-support;
               SL=MarketInfo(GetSymbolString(SymbolIndex),MODE_BID)-support;
               Spread=MarketInfo(GetSymbolString(SymbolIndex),MODE_SPREAD);
               SymbolPoint=MarketInfo(GetSymbolString(SymbolIndex),MODE_POINT);
               if (SL<=0.0 && TP>MinTargetinSpread*Spread*SymbolPoint) TPvsSL[SymbolIndex,tf]=100.0;
               else  
                  {
                  if (SL==0) return(false);
                  TPvsSL[SymbolIndex,tf]=(TP-Spread*SymbolPoint)/SL;
                  }
               //if (IsTesting()) 
               //Print("UpTrend на ",GetSymbolString(SymbolIndex),PeriodNumber(tf),"M  TP=",TP,"  SL=",SL,"  Spread=",Spread,"   TP/SL=",TPvsSL[SymbolIndex,tf]);
               }
            else
               {
               target=MathMin(iLow(GetSymbolString(SymbolIndex),PeriodNumber(tf),LastDownArray[SymbolIndex,tf]),iLow(GetSymbolString(SymbolIndex),PeriodNumber(tf),PreLastDownArray[SymbolIndex,tf]));
               support=MathMin(iHigh(GetSymbolString(SymbolIndex),PeriodNumber(tf),LastUpArray[SymbolIndex,tf]),iHigh(GetSymbolString(SymbolIndex),PeriodNumber(tf),PreLastUpArray[SymbolIndex,tf]));
               TP=support-target;
               Spread=MarketInfo(GetSymbolString(SymbolIndex),MODE_SPREAD);
               SymbolPoint=MarketInfo(GetSymbolString(SymbolIndex),MODE_POINT);
               SL=support-MarketInfo(GetSymbolString(SymbolIndex),MODE_BID);
               if (SL<=0.0 && TP>MinTargetinSpread*Spread*SymbolPoint) TPvsSL[SymbolIndex,tf]=100.0;
               else  
                  {
                  if (SL==0) return(false);
                  TPvsSL[SymbolIndex,tf]=(TP-Spread*SymbolPoint)/SL;
                  }
               //if (IsTesting()) 
               //Print("DownTrend на ",GetSymbolString(SymbolIndex),PeriodNumber(tf),"M  TP=",TP,"  SL=",SL,"  Spread=",Spread,"   TP/SL=",TPvsSL[SymbolIndex,tf]);
               }   
            if (TPvsSL[SymbolIndex,tf]>=TP_SL_Criteria)  
               {
               BestTPvsSLSymbol[listCounter]=SymbolIndex;
               BestTPvsSLPeriod[listCounter]=tf;
               //Print("Записали BestTPvsSLSymbol[listCounter] и BestTPvsSLPeriod[listCounter],SymbolIndex=",SymbolIndex,"  tf=",tf);
               listCounter++;
               res=true;
               }
            //if (IsTesting()) Print("Есть трендовый вход на ",GetSymbolString(SymbolIndex),PeriodNumber(tf),"M");
            }
         }
      }
//----
   return(res);
  }
  
  
//+------------------------------------------------------------------+
//| Попробуем открыть новый ордер                                    |
//+------------------------------------------------------------------+
void TryOpenOrder()
  {
   int BestTPSLindex=-1000;
   int i=0;
   int ticket,SymbolIndex,PeriodIndex;
   double target,support,SL,Spread,SymbolPoint,StopLevel,TP,openPrice,lots;
   string TPvsSLcomment;
   double StopPrice,TakePrice,ATR_Range;
   int err;
//----
   if (AccountFreeMargin()<0) return;
   while (i<=19 &&BestTPvsSLSymbol[i]!=0) 
      {
      if (TPvsSL[BestTPvsSLSymbol[i],BestTPvsSLPeriod[i]]>BestTPSLindex) 
         {
         if (AccountFreeMargin()<MarginCalculate(GetSymbolString(BestTPvsSLSymbol[i]),MarketInfo(GetSymbolString(BestTPvsSLSymbol[i]),MODE_MINLOT))) 
            {
            i++;
            continue;
            }
         //BestTPSLindex=TPvsSL[BestTPvsSLSymbol[i],BestTPvsSLPeriod[i]];
         BestTPSLindex=i;
         }
      //Print("Symbol ",GetSymbolString(BestTPvsSLSymbol[i]),PeriodNumber(BestTPvsSLPeriod[i]),"  TP/SL=",TPvsSL[BestTPvsSLSymbol[i],BestTPvsSLPeriod[i]]);
      i++;
      }
   if (BestTPSLindex>=0) 
      {  
      SymbolIndex=BestTPvsSLSymbol[BestTPSLindex];
      PeriodIndex=BestTPvsSLPeriod[BestTPSLindex];
      //Print("Лучший сигнал TP/SL=",TPvsSL[BestTPvsSLSymbol[BestTPSLindex],BestTPvsSLPeriod[BestTPSLindex]],"  на ",GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),"  BestTPSLindex=",BestTPSLindex);      

      if (TrendOnSymbol[SymbolIndex,PeriodIndex]>0)
         {// покупаем
         if (OrderOnSymbolExist(GetSymbolString(SymbolIndex),OP_BUY)) return;
         target=MathMax(iHigh(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),LastUpArray[SymbolIndex,PeriodIndex]),iHigh(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),PreLastUpArray[SymbolIndex,PeriodIndex]));
         support=MathMax(iLow(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),LastDownArray[SymbolIndex,PeriodIndex]),iLow(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),PreLastDownArray[SymbolIndex,PeriodIndex]));
         ATR_Range=2.0*iATR(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),ATRPeriod,1);
         TP=target-support;
         if (TP<ATR_Range) TP=ATR_Range;
         SL=MarketInfo(GetSymbolString(SymbolIndex),MODE_BID)-support;
         if (SL<ATR_Range) SL=ATR_Range;
         Spread=MarketInfo(GetSymbolString(SymbolIndex),MODE_SPREAD);
         SymbolPoint=MarketInfo(GetSymbolString(SymbolIndex),MODE_POINT);
         RefreshRates();
         openPrice=MarketInfo(GetSymbolString(SymbolIndex),MODE_ASK);
         StopPrice=NormalizeDouble(openPrice-SL,Digits);
         StopLevel=MarketInfo(GetSymbolString(SymbolIndex),MODE_STOPLEVEL);
         //if (SL/SymbolPoint<=StopLevel+Spread) StopPrice=NormalizeDouble(openPrice-(StopLevel+Spread+1)*SymbolPoint,Digits);
         TakePrice=NormalizeDouble(openPrice+TP,Digits);
         TPvsSLcomment=DoubleToStr(TPvsSL[SymbolIndex,PeriodIndex],2);
         lots=GetLotsOnRisk(GetSymbolString(SymbolIndex),RiskDelta,SL,SymbolPoint);
         Print("lots=",lots);
         if (lots>=MarketInfo(GetSymbolString(SymbolIndex),MODE_MINLOT))
            {
            if (IsTesting()) ticket=OrderSend(GetSymbolString(SymbolIndex),OP_BUY,lots,openPrice,Slippage,StopPrice,TakePrice,TPvsSLcomment,GetMagicNumber(PeriodIndex),0,Blue);
            else ticket=OrderSend(GetSymbolString(SymbolIndex),OP_BUY,lots,openPrice,Slippage,0,0,TPvsSLcomment,GetMagicNumber(PeriodIndex),0,Blue);
            if (ticket<0) 
               {
               err=GetLastError();
               Print("Не удалось открыть ордер в покупку ",lots," lots at ",GetSymbolString(SymbolIndex),"  on price ",openPrice,", sl ",StopPrice,", tp ",TakePrice );
               if (err==134) Print("Ошибка открытия ордера ",err,"  AccountFreeMargin=",AccountFreeMargin());
               if (err==130) Print("Ошибка открытия ордера ",err," StopLevel=",StopLevel,"  Spread=",Spread);
               }
            else
               {
               if (!GlobalVariableCheck(ticket+"FirstStop")) GlobalVariableSet(ticket+"FirstStop",SL);
               else GlobalVariableSet(ticket+"FirstStop",SL);
               if (!GlobalVariableCheck(ticket+"FirstTarget")) GlobalVariableSet(ticket+"FirstTarget",TakePrice);
               else GlobalVariableSet(ticket+"FirstTarget",TakePrice);
               if (!GlobalVariableCheck(ticket+"TimeFrame")) GlobalVariableSet(ticket+"TimeFrame",PeriodIndex);
               else GlobalVariableSet(ticket+"TimeFrame",PeriodIndex);
               }   
            }
         }

      if (TrendOnSymbol[SymbolIndex,PeriodIndex]<0)
         {// продаем
         if (OrderOnSymbolExist(GetSymbolString(SymbolIndex),OP_SELL)) return;
         target=MathMin(iLow(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),LastDownArray[SymbolIndex,PeriodIndex]),iLow(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),PreLastDownArray[SymbolIndex,PeriodIndex]));
         support=MathMin(iHigh(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),LastUpArray[SymbolIndex,PeriodIndex]),iHigh(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),PreLastUpArray[SymbolIndex,PeriodIndex]));
         ATR_Range=2.0*iATR(GetSymbolString(SymbolIndex),PeriodNumber(PeriodIndex),ATRPeriod,1);
         TP=support-target;
         if (TP<ATR_Range) TP=ATR_Range;
         SL=support-MarketInfo(GetSymbolString(SymbolIndex),MODE_BID);
         if (SL<ATR_Range) SL=ATR_Range;
         Spread=MarketInfo(GetSymbolString(SymbolIndex),MODE_SPREAD);
         SymbolPoint=MarketInfo(GetSymbolString(SymbolIndex),MODE_POINT);
         RefreshRates();
         openPrice=MarketInfo(GetSymbolString(SymbolIndex),MODE_BID);
         StopPrice=NormalizeDouble(openPrice+SL,Digits);
         StopLevel=MarketInfo(GetSymbolString(SymbolIndex),MODE_STOPLEVEL);
         //if (SL/SymbolPoint<=StopLevel+Spread) StopPrice=NormalizeDouble(openPrice-(StopLevel+Spread+1)*SymbolPoint,Digits);
         TakePrice=NormalizeDouble(openPrice-TP,Digits);
         TPvsSLcomment=DoubleToStr(TPvsSL[SymbolIndex,PeriodIndex],2);
         lots=GetLotsOnRisk(GetSymbolString(SymbolIndex),RiskDelta,SL,SymbolPoint);
         if (lots>=MarketInfo(GetSymbolString(SymbolIndex),MODE_MINLOT))
            {
            if (IsTesting()) ticket=OrderSend(GetSymbolString(SymbolIndex),OP_SELL,lots,openPrice,Slippage,StopPrice,TakePrice,TPvsSLcomment,GetMagicNumber(PeriodIndex),0,Red);
            else ticket=OrderSend(GetSymbolString(SymbolIndex),OP_SELL,lots,openPrice,Slippage,0,0,TPvsSLcomment,GetMagicNumber(PeriodIndex),0,Red);
            if (ticket<0) 
               {
               err=GetLastError();
               Print("Не удалось открыть ордер в продажу ",lots," lots at ",GetSymbolString(SymbolIndex),"  on price ",openPrice,", sl ",StopPrice,", tp ",TakePrice );
               if (err==134) Print("Ошибка открытия ордера ",err,"  AccountFreeMargin=",AccountFreeMargin());
               if (err==130) Print("Ошибка открытия ордера ",err," StopLevel=",StopLevel,"  Spread=",Spread);
               }
            else
               {
               if (!GlobalVariableCheck(ticket+"FirstStop")) GlobalVariableSet(ticket+"FirstStop",SL);
               else GlobalVariableSet(ticket+"FirstStop",SL);
               if (!GlobalVariableCheck(ticket+"FirstTarget")) GlobalVariableSet(ticket+"FirstTarget",TakePrice);
               else GlobalVariableSet(ticket+"FirstTarget",TakePrice);
               if (!GlobalVariableCheck(ticket+"TimeFrame")) GlobalVariableSet(ticket+"TimeFrame",PeriodIndex);
               else GlobalVariableSet(ticket+"TimeFrame",PeriodIndex);
               }   
               
            }
      
         }      
      }      
//----
   return;
  }

//+------------------------------------------------------------------+
//| Проверяем наличие ордера на на символе и по типу                 |
//+------------------------------------------------------------------+
bool OrderOnSymbolExist(string SymbolName, int Type)
  {
//----
   bool res=false;
   for (int i=0; i<OrdersTotal();i++)
      {
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
         {
         if (OrderSymbol()==SymbolName&&OrderType()==Type) res=true;
         }
      }
//----
   return(res);
  }

//+------------------------------------------------------------------+
//| Попробуем сократить количесвто ордеров                           |
//+------------------------------------------------------------------+
void TryCloseOrder()
  {
//----
   
//----
   return;
  }

//+------------------------------------------------------------------+
//| Проверяем ордера на трейлинг и частичное закрытие                |
//+------------------------------------------------------------------+
void CheckOrdersForTrailing()
  {
//----
   int EnterBar,ticket,total=OrdersTotal();
   datetime timeOpen;
   int tf,type,TSpar;
   double SymbolPoint,CurrParStop,a;
   double openPrice,StopPrice,ClosePrice,NewClosePrice,ATR2,Zbar=ZeroBar;
   int SymbolDigits,minStopLevel;
   if (total==0) return;
   for (int cnt=total-1;cnt>=0;cnt--)
      {
      if (OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES))
         {
         ticket=OrderTicket();
         timeOpen=OrderOpenTime();
         if (GlobalVariableCheck(ticket+"TimeFrame")) tf=PeriodNumber(GlobalVariableGet(ticket+"TimeFrame"));
         EnterBar=iBarShift(OrderSymbol(),tf,timeOpen);
         if (EnterBar==0) continue;
         SymbolPoint=MarketInfo(OrderSymbol(),MODE_POINT);
         SymbolDigits=MarketInfo(OrderSymbol(),MODE_DIGITS);
         minStopLevel=MarketInfo(OrderSymbol(),MODE_STOPLEVEL);
         openPrice=OrderOpenPrice();
         StopPrice=OrderStopLoss();
         if (GlobalVariableCheck(ticket+"FirstStop")) TSpar=GlobalVariableGet(ticket+"FirstStop")/SymbolPoint;
         a=TSpar*2./Zbar/Zbar;
         type=OrderType();
         RefreshRates();
         if (type==OP_BUY) 
            {
            if (StopPrice<openPrice) 
               {
               CurrParStop=NormalizeDouble(OrderOpenPrice()-(TSpar-a*EnterBar*EnterBar/2.0)*SymbolPoint,SymbolDigits);
               if (CurrParStop>OrderStopLoss()) 
                  {
                  if (MarketInfo(OrderSymbol(),MODE_BID)-CurrParStop>minStopLevel*SymbolPoint)OrderModify(OrderTicket(),OrderOpenPrice(),
                  CurrParStop,OrderTakeProfit(),0,Blue);
               
                  else OrderModify(OrderTicket(),OrderOpenPrice(),MarketInfo(OrderSymbol(),MODE_BID)-minStopLevel*SymbolPoint,OrderTakeProfit(),0,Blue);
                  }
               continue;   
               }   
            else
               {
               if (EnterBar>Zbar)
                  {
                  ClosePrice=MarketInfo(OrderSymbol(),MODE_BID);
                  ATR2=2*iATR(OrderSymbol(),tf,ATRPeriod,1);
                  NewClosePrice=NormalizeDouble(ClosePrice-ATR2,MarketInfo(OrderSymbol(),MODE_DIGITS));
                  if (StopPrice<NewClosePrice) OrderModify(OrderTicket(),OrderOpenPrice(),NewClosePrice,OrderTakeProfit(),0,Red);
                  }
               continue;   
               }   

            }
         if (type==OP_SELL) 
            {
            if (StopPrice>openPrice||StopPrice==0.0)
               {
               CurrParStop=NormalizeDouble(OrderOpenPrice()+(TSpar-a*EnterBar*EnterBar/2.0)*SymbolPoint,SymbolDigits);
               if (CurrParStop<OrderStopLoss()||OrderStopLoss()==0) 
                  {
                  if (CurrParStop-MarketInfo(OrderSymbol(),MODE_ASK)>minStopLevel*SymbolPoint)OrderModify(OrderTicket(),OrderOpenPrice(),
                  CurrParStop,OrderTakeProfit(),0,Red);
               
                  else OrderModify(OrderTicket(),OrderOpenPrice(),MarketInfo(OrderSymbol(),MODE_ASK)+minStopLevel*SymbolPoint,OrderTakeProfit(),0,Red);
                  }
               continue;
               }
            else
               {
               if (EnterBar>Zbar)
                  {
                  ClosePrice=MarketInfo(OrderSymbol(),MODE_ASK);
                  ATR2=2*iATR(OrderSymbol(),tf,ATRPeriod,1);
                  NewClosePrice=NormalizeDouble(ClosePrice+ATR2,MarketInfo(OrderSymbol(),MODE_DIGITS));
                  if (StopPrice>NewClosePrice) OrderModify(OrderTicket(),OrderOpenPrice(),NewClosePrice,OrderTakeProfit(),0,Red);
                  continue;
                  }
               }   
            }
         }
      }   
//----
   return;
  }

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
double GetLotsOnRisk(string SymbolName, double RiskPecentage,double StoplossInPoint,double PointValue)
  {
//----
   double res=0.0;   
   double MaxLoss=AccountFreeMargin()*RiskPecentage/100.0;
   if (RiskPecentage==0) 
      {
      res=MarketInfo(SymbolName,MODE_MINLOT);
      return(res);
      }
   res=MathCeil( MaxLoss/(MarketInfo(SymbolName,MODE_TICKVALUE)*StoplossInPoint/PointValue)*10);
   res=NormalizeDouble(res/10.0,1);
   if (MarginCalculate(SymbolName,res)>AccountFreeMargin()) res=res-MarketInfo(SymbolName,MODE_LOTSTEP);
   if (res>MaxOrderSize) res=MaxOrderSize;
   
//----
   return(res);
  }
  
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
/*  
  for (int i=0;i<13;i++)
   {
   Print("i=",i,"   Symbol=",GetSymbolString(i),"  spread=",MarketInfo(GetSymbolString(i),MODE_SPREAD),"  маржа на 0.1 лот=",MarginCalculate(GetSymbolString(i),0.1),"  стоимость пункта=",MarketInfo(GetSymbolString(i),MODE_TICKVALUE));
   } 
*/
   int index=0;
   if (IsTesting())
      {
      while(Period()!=PeriodNumber(index))
         {
         index++;
         }
      TestingIndexPeriod=index;
      int indexSymbol=1;
      while(Symbol()!=GetSymbolString(indexSymbol))
         {
         indexSymbol++;
         }
      TestingIndexSymbol=indexSymbol;
      Print("indexPeriod=",index,"  PeriodTesting=",PeriodNumber(index), "  Symbol index=",indexSymbol," SymbolTesting=",GetSymbolString(indexSymbol));            
      }
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
  int PeriodCounter=2,SymbolCounter=1,trendOnTF;
  string ComString;
  int tf;
//----
   if (IsTesting())
      {
      Print("Прошли проверку на IsTesting");
      for (int indexCount=TestingIndexPeriod;indexCount<=6;indexCount++)
         {
         if (isNewBar(TestingIndexSymbol,indexCount)) 
            {
            Print("Вызовем трейлинг");
            if (OrdersTotal()>0) CheckOrdersForTrailing();            
            //Print("NewBar on ",GetSymbolString(TestingIndexSymbol),PeriodNumber(indexCount),"M");
            trendOnTF=TrendByWPR(TestingIndexSymbol,indexCount);
            TrendOnSymbol[TestingIndexSymbol,indexCount]=trendOnTF;
            GatorTrend[TestingIndexSymbol,indexCount]=iCustom(GetSymbolString(TestingIndexSymbol),PeriodNumber(indexCount),"NRTR_GATOR",40,2,false,7,0);
            //Print("GatorTrend on",PeriodNumber(indexCount),"M =",GatorTrend[TestingIndexSymbol,indexCount]);
            NRTR_Trend[TestingIndexSymbol,indexCount]=iCustom(GetSymbolString(TestingIndexSymbol),PeriodNumber(indexCount),"NRTR_GATOR",40,2,false,6,1);
            //Print("NRTR_Trend on",PeriodNumber(indexCount),"M =",NRTR_Trend[TestingIndexSymbol,indexCount]);
            Complextrend[TestingIndexSymbol,indexCount]=(TrendOnSymbol[TestingIndexSymbol,indexCount]+GatorTrend[TestingIndexSymbol,indexCount]+NRTR_Trend[TestingIndexSymbol,indexCount])/3.0;
            ComString="";
            for (tf=indexCount;tf<7;tf++)
               {
               //ComString=ComString+StringConcatenate("Period ",PeriodNumber(tf),"M  Z_trend=",TrendOnSymbol[TestingIndexSymbol,tf],"  G_trend=",
               //GatorTrend[TestingIndexSymbol,tf],"  N_trend=",NRTR_Trend[TestingIndexSymbol,tf],"\n");
               ComString=ComString+StringConcatenate("Period ",PeriodNumber(tf),"M  Complextrend=",Complextrend[TestingIndexSymbol,tf],"\n");
            }
            Comment(ComString);   
            Print("Trend on ",GetSymbolString(SymbolCounter),PeriodNumber(PeriodCounter),"M=",trendOnTF);
            }
         }
      }
   else
      {
      for (SymbolCounter=1;SymbolCounter<13;SymbolCounter++)
         {
         for (PeriodCounter=2;PeriodCounter<=6;PeriodCounter++)
            {
            if (isNewBar(SymbolCounter,PeriodCounter))
               {
               if (OrdersTotal()>0) CheckOrdersForTrailing();            
               //Print("NewBar on ",GetSymbolString(SymbolCounter),PeriodNumber(PeriodCounter),"M");
               trendOnTF=TrendByWPR(SymbolCounter,PeriodCounter);
               TrendOnSymbol[SymbolCounter,PeriodCounter]=trendOnTF;
               GatorTrend[SymbolCounter,PeriodCounter]=iCustom(GetSymbolString(SymbolCounter),PeriodNumber(PeriodCounter),"NRTR_GATOR",40,2,false,7,0);
               NRTR_Trend[SymbolCounter,PeriodCounter]=iCustom(GetSymbolString(SymbolCounter),PeriodNumber(PeriodCounter),"NRTR_GATOR",40,2,false,6,1);
               Complextrend[SymbolCounter,PeriodCounter]=(TrendOnSymbol[SymbolCounter,PeriodCounter]+GatorTrend[SymbolCounter,PeriodCounter]+NRTR_Trend[SymbolCounter,PeriodCounter])/3.0;
               if (Symbol()==GetSymbolString(SymbolCounter))
                  {
                  ComString="";
                  for (tf=2;tf<7;tf++)
                     {
                     ComString=ComString+StringConcatenate("Period ",PeriodNumber(tf),"M  Z_trend=",TrendOnSymbol[SymbolCounter,tf],"  G_trend=",
                     GatorTrend[SymbolCounter,tf],"  N_trend=",NRTR_Trend[SymbolCounter,tf],"\n");
                     }
                  Comment(ComString);   
               //Print("Trend on ",GetSymbolString(SymbolCounter),PeriodNumber(PeriodCounter),"M=",trendOnTF);
                  }
               } 
            }
         }
      }
   
   ArrayInitialize(TPvsSL,0.0);
   ArrayInitialize(BestTPvsSLSymbol,0);
   ArrayInitialize(BestTPvsSLPeriod,0);
   if (TrendExist())
      {
      if (OrdersTotal()<MaxOpenedOrders) TryOpenOrder();
      else TryCloseOrder();
      }
//----
   return(0);
  }
//+------------------------------------------------------------------+