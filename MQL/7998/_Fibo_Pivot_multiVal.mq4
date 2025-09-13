//+------------------------------------------------------------------+
//|                                         _Fibo_Pivot_multiVal.mq4 |
//|                                                         olyakish |
//+------------------------------------------------------------------+
#property copyright "olyakish"
#property link      ""

extern int CountSymbol=10;
extern string AllVal="EURUSD,GBPUSD,USDCHF,USDJPY,USDCAD,AUDUSD,AUDJPY,CADJPY,EURJPY,EURCHF";
extern string All_Level_P_F1="33,33,33,33,33,33,33,33,33,33";
extern string All_Level_F1_F2="50,50,50,50,50,50,50,50,50,50";
extern string All_Level_F2_F3="33,33,33,33,33,33,33,33,33,33";
extern string All_Level_F3_out="40,40,40,40,40,40,40,40,40,40";
extern string All_F2_F3_Order = "bs,bs,bs,bs,bs,bs,bs,bs,bs,bs";
extern string rem01 = " оличество профитных сделок чтобы по данной паре не велась торговл€";
extern string All_ExpertTrades="15,15,15,15,15,15,15,15,15,15";
extern string rem02 = "ћинимальное значение профита в !пунтах!";
extern string All_ExpertProfit1="150,150,150,150,150,150,150,150,150,150";
extern int AllValProfit=50;
extern int AllValProfitTrades=35;
extern string rem03 = "≈сли b то вверху buy внизу sell";
extern string rem04 = "≈сли s то вверху sell внизу buy";
extern string rem05 = "≈сли bs то вверху и внизу buy/sell";
extern int MagicStart=1000;
extern int GlobalPeriod=15;
extern string HourMinStart="00:01";
extern string HourMinFinish="08:00";
extern string HourMinCloseAll="12:00";
extern bool DrawLine=true;

////--------данные возможности пока в мультивалютном эксперте не работают 
string rem06 = "ѕараметры если переменна€ TZ==false";
int LimitPointIn=150;
int LimitPointOut=50;
bool TZ=true;


double LinePrice[19,50];
int LineStill[19];
color LineColor[19];
string LineName[19];
int timeolddraw[19];
int Magic=0;
double lot=0.1;
bool trade[];
int TimeDel=0;
string TradeSymbol[];
double Level_P_F1[];
double Level_F1_F2[];
double Level_F2_F3[];
double Level_F3_out[];
string F2_F3_Order[];
int ExpertTrades[];
int ExpertProfit[];
int ExpertTradesReal[];
int ExpertProfitReal[];

int init()
   {
      Print(All_ExpertProfit1);
      ArrayResize(TradeSymbol,CountSymbol);
      ArrayResize(Level_P_F1,CountSymbol);
      ArrayResize(Level_F1_F2,CountSymbol);
      ArrayResize(Level_F2_F3,CountSymbol);
      ArrayResize(Level_F3_out,CountSymbol);
      ArrayResize(F2_F3_Order,CountSymbol);
      ArrayResize(ExpertTrades,CountSymbol);
      ArrayResize(ExpertProfit,CountSymbol);
      ArrayResize(trade,CountSymbol);
      ArrayResize(ExpertTradesReal,CountSymbol);
      ArrayResize(ExpertProfitReal,CountSymbol);
      for(int i=0;i<=CountSymbol-1;i++){trade[i]=true;}
      –азбивка(0,AllVal);
      –азбивка(1,All_Level_P_F1);
      –азбивка(2,All_Level_F1_F2);
      –азбивка(3,All_Level_F2_F3);
      –азбивка(4,All_Level_F3_out);
      –азбивка(5,All_F2_F3_Order);
      –азбивка(6,All_ExpertTrades);
      –азбивка(7,All_ExpertProfit1);
      
      return(0);
   }

//+------------------------------------------------------------------+
void –азбивка(int nMass,string stroka)
   {
      int i,n;
      int y=0;
      string mid="";
      if (nMass==0){for (i=0;i<=CountSymbol-1;i++){TradeSymbol[i]=StringSubstr(StringSubstr(stroka,i*7,6),0,6);}}     
      Print(stroka);
      for(n=0;n<=StringLen(stroka)-1;n++)
         {
            if (StringSubstr(stroka,n,1)!=",") { mid=StringConcatenate(mid,StringSubstr(stroka,n,1));}            
            if (StringSubstr(stroka,n,1)=="," || n==StringLen(stroka)-1)
               {
                  switch(nMass)
                     {
                        case 1:
                           {
                              if (mid==""){Level_P_F1[y]=0;y++;}
                              if (mid!=""){Level_P_F1[y]=StrToDouble(mid);y++;mid="";}
                              break;
                           }
                        case 2:
                           {
                              if (mid==""){Level_F1_F2[y]=0;y++;}
                              if (mid!=""){Level_F1_F2[y]=StrToDouble(mid);y++;mid="";}
                              break;
                           }
                        case 3:
                           {
                              if (mid==""){Level_F2_F3[y]=0;y++;}
                              if (mid!=""){Level_F2_F3[y]=StrToDouble(mid);y++;mid="";}
                              break;
                           }                           
                        case 4:
                           {
                              if (mid==""){Level_F3_out[y]=0;y++;}
                              if (mid!=""){Level_F3_out[y]=StrToDouble(mid);y++;mid="";}
                              break;
                           }
                        case 5:
                           {
                              if (mid==""){F2_F3_Order[y]="bs";y++;}
                              if (mid!=""){F2_F3_Order[y]=mid;y++;mid="";}
                              break;
                           }
                        case 6:
                           {
                              if (mid==""){ExpertTrades[y]=0;y++;}
                              if (mid!=""){ExpertTrades[y]=StrToInteger(mid);y++;mid="";}
                              break;
                           }
                        case 7:
                           {
                              if (mid==""){ExpertProfit[y]=0;y++;}
                              if (mid!=""){ExpertProfit[y]=StrToInteger(mid);y++;mid="";}
                              //Print (ExpertProfit[y-1]);
                              break;
                           }                                                                                 
                     }
               }     
          }
      return (0);
   }
//+------------------------------------------------------------------+
bool ѕроверкаЌаличи€ќрдера(int MagicForFind)
   {      
         for(int i=0;i<=OrdersTotal();i++)
            {
               OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
               if (OrderMagicNumber()==MagicForFind && OrderCloseTime()==0){return(true);}                  
            }
      return(false);
   }

//+------------------------------------------------------------------+
int  онтрольѕрофита(int magicStart,int CountValIn)
   {      
         int AllProfitDoday=0;
         int AllProfitTrade=0;
         int ThisValProfitDoday=0;
         int ThisValProfitTrade=0;
         RefreshRates();
         double point;         
         bool exit=false;
         for(int i=OrdersHistoryTotal()-1;i>=0;i--)
            {
               OrderSelect(i,SELECT_BY_POS,MODE_HISTORY);

           //    while (exit==false)
                  {
                     RefreshRates();
                     point=MarketInfo(OrderSymbol(),MODE_POINT);
             //        if(NormalizeDouble(point,4)==0.0001){point=NormalizeDouble(point,4);break;}
               //      if(NormalizeDouble(point,2)==0.01){point=NormalizeDouble(point,2);break;}
                 //    Print("CountValIn=",CountValIn," NormalizeDouble(point,8)=",NormalizeDouble(point,8));
                  }
               if (MagicStart>OrderMagicNumber() || MagicStart+14+15*CountSymbol<OrderMagicNumber()){continue;}   // работаем только со своей группой магиков
               if (Day()==TimeDay(OrderCloseTime()) && OrderCloseTime()!=0) // ордер был закрыт сегодн€
                  {
                     if (OrderType()==0)
                        {
                           if (magicStart<OrderMagicNumber() && magicStart+14>=OrderMagicNumber()){ThisValProfitDoday+=(OrderClosePrice()-OrderOpenPrice())/point;}
                           if (MagicStart<OrderMagicNumber() && MagicStart+14+15*CountSymbol>=OrderMagicNumber()){AllProfitDoday+=(OrderClosePrice()-OrderOpenPrice())/point;}
                        }
                     if (OrderType()==1)
                        {
                           if (magicStart<OrderMagicNumber() && magicStart+14>=OrderMagicNumber()){ThisValProfitDoday+=(OrderOpenPrice()-OrderClosePrice())/point;}
                           if (MagicStart<OrderMagicNumber() && MagicStart+14+15*CountSymbol>=OrderMagicNumber()){AllProfitDoday+=(OrderOpenPrice()-OrderClosePrice())/point;}
                        }
                     if(OrderClosePrice()==OrderTakeProfit())
                        {
                           if (magicStart<OrderMagicNumber() && magicStart+14>=OrderMagicNumber()){ThisValProfitTrade++;}
                           if (MagicStart<OrderMagicNumber() && MagicStart+14+15*CountSymbol>=OrderMagicNumber()){AllProfitTrade++;}
                        }                        
                  }
               else {break;}/// заканчиваем просмотр так как закончились ордера в истории по данному дню                  
            }
         RefreshRates();
         int AllProfitHistory=AllProfitDoday;         
         int ThisValProfitHistory=ThisValProfitDoday;
        // Print ("History=",AllProfitDoday,"  " ,AllValProfit,"  " ,AllProfitTrade,"  " ,AllValProfitTrades);
         
         for(i=OrdersTotal()-1;i>=0;i--)
            {
               OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
               while (exit==false)
                  {
                     RefreshRates();
                     point=MarketInfo(OrderSymbol(),MODE_POINT);
                     if(NormalizeDouble(point,4)==0.0001){point=NormalizeDouble(point,4);break;}
                     if(NormalizeDouble(point,2)==0.01){point=NormalizeDouble(point,2);break;}
                  }               
               //Print ("Symbol=",OrderSymbol()," ",OrderOpenPrice() ,"   A=", MarketInfo(OrderSymbol(),MODE_ASK),"   B=", MarketInfo(OrderSymbol(),MODE_BID),"  P=",point);
               if (MagicStart>OrderMagicNumber() || MagicStart+14+15*CountSymbol<OrderMagicNumber()){continue;}   // работаем только со своей группой магиков
               if (OrderType()==0)
                  {
                     if (MagicStart<OrderMagicNumber() && MagicStart+14+15*CountSymbol>=OrderMagicNumber()){AllProfitDoday+=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/point;}
                     if (magicStart<OrderMagicNumber() && magicStart+14>=OrderMagicNumber()){ThisValProfitDoday+=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/point;}
                  }
              if (OrderType()==1)
                  {
                     if (MagicStart<OrderMagicNumber() && MagicStart+14+15*CountSymbol>=OrderMagicNumber()){AllProfitDoday+=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/point;}
                     if (magicStart<OrderMagicNumber() && magicStart+14>=OrderMagicNumber()){ThisValProfitDoday+=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/point;}
                  }
            }
      //Comment ("AllProfitHistory=",AllProfitHistory,"\n","AllProfitDoday=",AllProfitDoday,"\nTrade=",trade[CountValIn],"\n",TimeToStr(TimeCurrent(),TIME_DATE|TIME_MINUTES),"\nAllProfitTrade=",AllProfitTrade);
      ExpertTradesReal[CountValIn]=ThisValProfitTrade;
      ExpertProfitReal[CountValIn]=ThisValProfitDoday;
      //Print (ThisValProfitDoday,"   ",ExpertProfit[CountValIn],"  ", ThisValProfitTrade,"  ",ExpertTrades[CountValIn]);        
      //Print(
      //Print ("All",AllProfitDoday,"  " ,AllValProfit,"  " ,AllProfitTrade,"  " ,AllValProfitTrades);
      if (AllProfitDoday>=AllValProfit || AllProfitTrade>=AllValProfitTrades){return(-1);}
   
      if (ThisValProfitDoday>=ExpertProfit[CountValIn] || ThisValProfitTrade>=ExpertTrades[CountValIn]){return(1);}      
      return(0);
   }
 
//+------------------------------------------------------------------+

void ”становкаќтложенного(int type,double prOpen,double TP,double SL,int Magic,int CountValIn)
   {
      
      if (type==0)   //устанавливаем на покупку либо байлимит либо байстоп
         {
            if (MarketInfo(TradeSymbol[CountValIn],MODE_ASK)<prOpen-3*MarketInfo(TradeSymbol[CountValIn],MODE_POINT))
               {
                  Print("OP_BUYSTOP,",prOpen,",",TP,",",SL,",",Magic);
                  OrderSend(TradeSymbol[CountValIn],OP_BUYSTOP,lot,NormalizeDouble(prOpen,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),0,NormalizeDouble(SL,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),NormalizeDouble(TP,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),"Buy",Magic,0,Lime);
               }
            if (MarketInfo(TradeSymbol[CountValIn],MODE_BID)>prOpen+3*MarketInfo(TradeSymbol[CountValIn],MODE_POINT))
               {
                  Print("OP_BUYLIMIT,",prOpen,",",TP,",",SL,",",Magic);
                  OrderSend(TradeSymbol[CountValIn],OP_BUYLIMIT,lot,NormalizeDouble(prOpen,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),0,NormalizeDouble(SL,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),NormalizeDouble(TP,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),"Buy",Magic,0,Lime);
               }
         }
      if (type==1)   //устанавливаем на продажу либо селллимит либо селлстоп
         {
            if (MarketInfo(TradeSymbol[CountValIn],MODE_BID)>prOpen+3*MarketInfo(TradeSymbol[CountValIn],MODE_POINT))
               {
                  Print("OP_SELLSTOP,",prOpen,",",TP,",",SL,",",Magic);
                  OrderSend(TradeSymbol[CountValIn],OP_SELLSTOP,lot,NormalizeDouble(prOpen,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),0,NormalizeDouble(SL,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),NormalizeDouble(TP,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),"Sell",Magic,0,Lime);
               }
            if (MarketInfo(TradeSymbol[CountValIn],MODE_ASK)<prOpen-3*MarketInfo(TradeSymbol[CountValIn],MODE_POINT))
               {
                  Print("OP_SELLLIMIT,",prOpen,",",TP,",",SL,",",Magic);
                  OrderSend(TradeSymbol[CountValIn],OP_SELLLIMIT,lot,NormalizeDouble(prOpen,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),0,NormalizeDouble(SL,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),NormalizeDouble(TP,MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),"Sell",Magic,0,Lime);
               }              
         }         
      return(0);      
   }
  
//+------------------------------------------------------------------+

bool «акрытиеќрдеров(int type,int magicStart,int CountValIn)
   {
      //-2 закрыть все ордера по всем парам 
      //-1 закрыть все ордера по стартовому магику по данной паре
      // 0 закрыть все ордера BUY по стартовому магику
      // 1 закрыть все ордера SELL по стартовому магику
      // 2 удалить все отложенные ордера
    if (OrdersTotal()!=0) // у нас есть  ордера
      {
         for (int i=OrdersTotal()-1;i>=0;i--)
            {
               OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
               if (magicStart>OrderMagicNumber() && magicStart+14<OrderMagicNumber() && type!=-2 ){continue;}   // работаем только со своей группой магиков
               if (MagicStart>OrderMagicNumber() && MagicStart+15*CountSymbol+14<OrderMagicNumber() && type==-2 ){continue;}   // работаем только со своей группой магиков               
               if (type==-1 || type==2){if (OrderType()>=2){OrderDelete(OrderTicket());}}   // удал€ем отложенные ордера
               RefreshRates();
               if (type==-1 || type==0){if (OrderType()==OP_BUY){OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(MarketInfo(TradeSymbol[CountValIn],MODE_BID),MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),2,Lime);}}
               if (type==-1 || type==1){if (OrderType()==OP_SELL){OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(MarketInfo(TradeSymbol[CountValIn],MODE_ASK),MarketInfo(TradeSymbol[CountValIn],MODE_DIGITS)),2,Lime);}}
               if (type==-2 && OrderType()==OP_BUY){OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID),MarketInfo(OrderSymbol(),MODE_DIGITS)),2,Lime);}
               if (type==-2 && OrderType()==OP_SELL){OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(MarketInfo(OrderSymbol(),MODE_ASK),MarketInfo(OrderSymbol(),MODE_DIGITS)),2,Lime);}
               
            }
      }   
      return(0);
   }
//+------------------------------------------------------------------+


int start()
  {
    //if (AccountNumber()!=586802){return(0);}
    //Comment("");
    LineColor[0]=Magenta;LineColor[1]=LimeGreen;LineColor[2]=LimeGreen;LineColor[3]=LimeGreen;LineColor[4]=LimeGreen;LineColor[5]=LimeGreen;LineColor[6]=LimeGreen;
    LineColor[7]=SkyBlue;LineColor[8]=SkyBlue;LineColor[9]=SkyBlue;LineColor[10]=SkyBlue;LineColor[11]=SkyBlue;LineColor[12]=SkyBlue;
    LineColor[13]=SkyBlue;LineColor[14]=SkyBlue;LineColor[15]=SkyBlue;LineColor[16]=SkyBlue;LineColor[17]=SkyBlue;LineColor[18]=SkyBlue;
    lot=0.1;
    int iii;
    
    for(iii=0;iii<CountSymbol-1;iii++)
      {
         if (!IsTesting())
            {
               if (WindowHandle(TradeSymbol[iii],GlobalPeriod)==0){continue;}//// нет графика по данному символу - пропускаем цикл
            }
         double yesterday_high = 0;
         double yesterday_open = 0;
         double yesterday_low = 0;
         double yesterday_close = 0;
         double P = 0, S = 0, R = 0, S1 = 0, R1 = 0, S2 = 0, R2 = 0, S3 = 0, R3 = 0;
         double P_R1_1=0,P_R1_2=0,R1_R2_1=0,R2_R3_1=0,R2_R3_2=0,R3_out_1=0;
         double P_S1_1=0,P_S1_2=0,S1_S2_1=0,S2_S3_1=0,S2_S3_2=0,S3_out_1=0;
    
         int startbars=iTime(TradeSymbol[iii],GlobalPeriod,0)/86400;
         startbars*=86400;
         yesterday_high=iHigh(TradeSymbol[iii],GlobalPeriod,iBarShift(TradeSymbol[iii],GlobalPeriod,startbars,false)+1);
         yesterday_low=iLow(TradeSymbol[iii],GlobalPeriod,iBarShift(TradeSymbol[iii],GlobalPeriod,startbars,false)+1);  
         yesterday_close=iClose(TradeSymbol[iii],GlobalPeriod,iBarShift(TradeSymbol[iii],GlobalPeriod,startbars,false)+1);
         int j;   
         for(j=iBarShift(TradeSymbol[iii],GlobalPeriod,startbars,false)+1;j<=3610;j++)
            {
               if (iLow(TradeSymbol[iii],GlobalPeriod,j)<yesterday_low){yesterday_low=iLow(TradeSymbol[iii],GlobalPeriod,j);}
               if(iHigh(TradeSymbol[iii],GlobalPeriod,j)>yesterday_high){yesterday_high=iHigh(TradeSymbol[iii],GlobalPeriod,j);} 
               int t=iTime(TradeSymbol[iii],GlobalPeriod,j)/86400;
               if (iTime(TradeSymbol[iii],GlobalPeriod,j)==t*86400){break;}
            }
         //------ Pivot Points ------
         R = (yesterday_high - yesterday_low);
         P = (yesterday_high + yesterday_low + yesterday_close)/3; LinePrice[0,iii]=P;LineName[0]="P";LineStill[0]=0;//Pivot
         R1 = P + (R * 0.38);LinePrice[1,iii]=R1;LineName[1]="R1";LineStill[1]=0;
         R2 = P + (R * 0.62);LinePrice[2,iii]=R2;LineName[2]="R2";LineStill[2]=0;
         R3 = P + (R * 0.99);LinePrice[3,iii]=R3;LineName[3]="R3";LineStill[3]=0;
         S1 = P - (R * 0.38);LinePrice[4,iii]=S1;LineName[4]="S1";LineStill[4]=0;
         S2 = P - (R * 0.62);LinePrice[5,iii]=S2;LineName[5]="S2";LineStill[5]=0;
         S3 = P - (R * 0.99);LinePrice[6,iii]=S3;LineName[6]="S3";LineStill[6]=0;
    
         P_R1_1=NormalizeDouble(P+(R1-P)*(Level_P_F1[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[7,iii]=P_R1_1;LineName[7]="P_R1_1";LineStill[7]=2;
         P_R1_2=NormalizeDouble(R1-(R1-P)*(Level_P_F1[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[8,iii]=P_R1_2;LineName[8]="P_R1_2";LineStill[8]=2;
         R1_R2_1=NormalizeDouble(R1+(R2-R1)*(Level_F1_F2[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[9,iii]=R1_R2_1;LineName[9]="R1_R2_1";LineStill[9]=2;
         R2_R3_1=NormalizeDouble(R2+(R3-R2)*(Level_F2_F3[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[10,iii]=R2_R3_1;LineName[10]="R2_R3_1";LineStill[10]=2;
         R2_R3_2=NormalizeDouble(R3-(R3-R2)*(Level_F2_F3[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[11,iii]=R2_R3_2;LineName[11]="R2_R3_2";LineStill[11]=2;
         R3_out_1=NormalizeDouble(R3+(R3-R2)*(Level_F3_out[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[12,iii]=R3_out_1;LineName[12]="R3_out_1";LineStill[12]=2;
         P_S1_1=NormalizeDouble(P-(P-S1)*(Level_P_F1[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[13,iii]=P_S1_1;LineName[13]="P_S1_1";LineStill[13]=2;
         P_S1_2=NormalizeDouble(S1+(P-S1)*(Level_P_F1[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[14,iii]=P_S1_2;LineName[14]="P_S1_2";LineStill[14]=2;
         S1_S2_1=NormalizeDouble(S1-(S1-S2)*(Level_F1_F2[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[15,iii]=S1_S2_1;LineName[15]="S1_S2_1";LineStill[15]=2;
         S2_S3_1=NormalizeDouble(S2-(S2-S3)*(Level_F2_F3[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[16,iii]=S2_S3_1;LineName[16]="S2_S3_1";LineStill[16]=2;
         S2_S3_2=NormalizeDouble(S3+(S2-S3)*(Level_F2_F3[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[17,iii]=S2_S3_2;LineName[17]="S2_S3_2";LineStill[17]=2;
         S3_out_1=NormalizeDouble(S3-(S2-S3)*(Level_F3_out[iii]/100),MarketInfo(TradeSymbol[iii],MODE_DIGITS));LinePrice[18,iii]=S3_out_1;LineName[18]="S3_out_1";LineStill[18]=2;
    
         if (TimeToStr(TimeCurrent(),TIME_MINUTES)>=HourMinStart && TimeToStr(TimeCurrent(),TIME_MINUTES)<HourMinFinish && trade[iii]==true)
            {  
               if (MarketInfo(TradeSymbol[iii],MODE_BID)>R2 && MarketInfo(TradeSymbol[iii],MODE_BID)<R3)
                  {
                     //Comment("R2---R3");
                     Magic=MagicStart+15*iii+3;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("0R2---R3");
                           if (TZ){”становкаќтложенного(0,R2_R3_1,R2_R3_2,0,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,R2_R3_1,R3_out_1,0,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,R2_R3_1,P,0,Magic,iii);}
                        }
                     Magic=MagicStart+15*iii+2;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("1R2---R3");
                           if (TZ){”становкаќтложенного(1,R2_R3_2,R2_R3_1,0,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,R2_R3_2,R3_out_1,0,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,R2_R3_2,P,0,Magic,iii);}                            
                        }                  
                  }

               if (MarketInfo(TradeSymbol[iii],MODE_BID)>R1 && MarketInfo(TradeSymbol[iii],MODE_BID)<R2)
                  {
                     //Comment("R1---R2");            
                     if (F2_F3_Order[iii]=="b" || F2_F3_Order[iii]=="bs")
                        {
                           Magic=MagicStart+15*iii+5;
                           if (!ѕроверкаЌаличи€ќрдера(Magic))
                              {
                                 //Comment("0R1---R2");  
                                 if (TZ){”становкаќтложенного(0,R1_R2_1,R2,0,Magic,iii);}
                                 if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,R1_R2_1,R3_out_1,0,Magic,iii);}
                                 if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,R1_R2_1,P,0,Magic,iii);}                           
                              }
                        }
                     if (F2_F3_Order[iii]=="s" || F2_F3_Order[iii]=="bs")
                        {
                           Magic=MagicStart+15*iii+4;
                           if (!ѕроверкаЌаличи€ќрдера(Magic))
                              {
                                 //Comment("1R1---R2");
                                 if (TZ){”становкаќтложенного(1,R1_R2_1,R1,0,Magic,iii);}
                                 if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,R1_R2_1,R3_out_1,0,Magic,iii);}
                                 if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,R1_R2_1,P,0,Magic,iii);}                             
                              }                  
                        }
                  }
               if (MarketInfo(TradeSymbol[iii],MODE_BID)>P && MarketInfo(TradeSymbol[iii],MODE_BID)<R1)
                  {
                     //Comment("P---R1");               
                     Magic=MagicStart+15*iii+7;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("0P---R1");
                           if (TZ){”становкаќтложенного(0,P_R1_1,P_R1_2,0,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,P_R1_1,R3_out_1,0,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,P_R1_1,P,0,Magic,iii);}                       
                        }
                     Magic=MagicStart+15*iii+6;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("1P---R1");
                           if (TZ){”становкаќтложенного(1,P_R1_2,P_R1_1,0,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,P_R1_2,R3_out_1,0,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,P_R1_2,P,0,Magic,iii);}                      
                        }                  
                  }
               if (MarketInfo(TradeSymbol[iii],MODE_BID)<P && MarketInfo(TradeSymbol[iii],MODE_BID)>S1)
                  {
                     //Comment("P---S1"); 
                     Magic=MagicStart+15*iii+9;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("0P---S1");
                           if (TZ){”становкаќтложенного(0,P_S1_2,P_S1_1,0,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,P_S1_2,P,0,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,P_S1_2,S3_out_1,0,Magic,iii);}                       
                        }
                     Magic=MagicStart+15*iii+8;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("1P---S1");
                           if (TZ){”становкаќтложенного(1,P_S1_1,P_S1_2,0,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,P_S1_1,P,0,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,P_S1_1,S3_out_1,0,Magic,iii);}                        
                        }                  
                  }
               if (MarketInfo(TradeSymbol[iii],MODE_BID)<S1 && MarketInfo(TradeSymbol[iii],MODE_BID)>S2)
                  {
                     //Comment("S1---S2"); 
                     if (F2_F3_Order[iii]=="s" || F2_F3_Order[iii]=="bs")
                        {   
                           Magic=MagicStart+15*iii+11;
                           if (!ѕроверкаЌаличи€ќрдера(Magic))
                              {
                                 ///Print("0S1---S2"); 
                                 if (TZ){”становкаќтложенного(0,S1_S2_1,S1,0,Magic,iii);}
                                 if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,S1_S2_1,P,0,Magic,iii);}
                                 if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,S1_S2_1,S3_out_1,0,Magic,iii);}                             
                              }
                        }
                     if (F2_F3_Order[iii]=="b" || F2_F3_Order[iii]=="bs")
                        { 
                           Magic=MagicStart+15*iii+10;
                           if (!ѕроверкаЌаличи€ќрдера(Magic))
                              {
                                 //Print("1S1---S2"); 
                                 if (TZ){”становкаќтложенного(1,S1_S2_1,S2,0,Magic,iii);}
                                 if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,S1_S2_1,P,0,Magic,iii);}
                                 if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,S1_S2_1,S3_out_1,0,Magic,iii);}                             
                              }                  
                        }
                  }           
               if (MarketInfo(TradeSymbol[iii],MODE_BID)<S2 && MarketInfo(TradeSymbol[iii],MODE_BID)>S3)
                  {
                     //Comment("S2---S3");
                     Magic=MagicStart+15*iii+13;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("0S2---S3");
                           if (TZ){”становкаќтложенного(0,S2_S3_2,S2_S3_1,0,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,S2_S3_2,P,0,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,S2_S3_2,S3_out_1,0,Magic,iii);}                             
                        }
                     Magic=MagicStart+15*iii+12;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("1S2---S3");
                           if (TZ){”становкаќтложенного(1,S2_S3_1,S2_S3_2,0,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,S2_S3_1,P,0,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,S2_S3_1,S3_out_1,0,Magic,iii);}                                 
                        }              
                  }
               if (MarketInfo(TradeSymbol[iii],MODE_BID)<S3)
                  {
                     //Comment("<S3"); 
                     Magic=MagicStart+15*iii+14;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("1<S3");                  
                           lot=0.2;
                           trade[iii]=false;
                           if (TZ){”становкаќтложенного(1,S3_out_1,S3_out_1-(S3-S3_out_1),S2,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,S3_out_1,P,0,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,S3_out_1,0,0,Magic,iii);}                        
                        }
                  }
                  
               if (MarketInfo(TradeSymbol[iii],MODE_BID)>R3)
                  {
                     //Comment(">R3"); 
                     Magic=MagicStart+15*iii+1;
                     if (!ѕроверкаЌаличи€ќрдера(Magic))
                        {
                           //Print("1>R3");
                           lot=0.2;
                           trade[iii]=false;
                           if (TZ){”становкаќтложенного(0,R3_out_1,R3_out_1+(R3_out_1-R3),R2,Magic,iii);}
                           if (!TZ && R<LimitPointOut*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(0,R3_out_1,0,0,Magic,iii);}
                           if (!TZ && R>LimitPointIn*MarketInfo(TradeSymbol[iii],MODE_POINT)){”становкаќтложенного(1,R3_out_1,P,0,Magic,iii);}                       
                        }
                  }
                
                        
            }/// if           
      //--------------------------------------------------------------------  
      int Check= онтрольѕрофита(MagicStart+15*iii,iii);
      if(Check==-1)
         {
            //Print("Check",Check,"   magic=",MagicStart+15*iii,"   ",iii);
            «акрытиеќрдеров(-2,MagicStart,iii); //  закрываем на сегодн€ торговлю по всем парам
            «акрытиеќрдеров(2,MagicStart+15*iii,iii); //  удал€ем отложенные            
            ArrayInitialize(trade,0);
         } 
      if(Check==1)
         {
            //Print("Check",Check,"   magic=",MagicStart+15*iii,"   ",iii);
            «акрытиеќрдеров(-1,MagicStart+15*iii,iii); //  закрываем на сегодн€ торговлю по данной паре
            trade[iii]=false;
         }
      }//////////////////iii       
   //--------------------------------------------------------------------      

   if (TimeToStr(TimeCurrent(),TIME_MINUTES)>HourMinFinish)
      {
         //Print(" TimeToStr(TimeCurrent(),TIME_MINUTES)>HourMinFinish"); 
         «акрытиеќрдеров(2,MagicStart+15*iii,iii); //  удал€ем отложенные
         trade[iii]=false;
      }
   if (HourMinStart>TimeToStr(TimeCurrent(),TIME_MINUTES)){ArrayInitialize(trade,1);ArrayInitialize(ExpertTradesReal,0);ArrayInitialize(ExpertProfitReal,0);}     
   if (TimeToStr(TimeCurrent(),TIME_MINUTES)>HourMinCloseAll)
      {
         //Print("  (TimeToStr(TimeCurrent(),TIME_MINUTES)>HourMinCloseAll)");
         «акрытиеќрдеров(-2,MagicStart,iii); //  удал€ем !¬—≈!         
         trade[iii]=true;
      }
//============================================================================
  /* 
   if(DrawLine && timeolddraw[iii]!=iTime(TradeSymbol[iii],GlobalPeriod,1))
      { 
         for(int y=0;y<=18;y++)
            {
               if(ObjectFind(LineName[y])!=0)
                  {
                     ObjectCreate(LineName[y], OBJ_TREND, 0, startbars, LinePrice[y,iii],iTime(TradeSymbol[iii],GlobalPeriod,0),LinePrice[y,iii]);
                     ObjectSet(LineName[y], OBJPROP_STYLE, LineStill[y]);
                     ObjectSet(LineName[y], OBJPROP_COLOR, LineColor[y]);
                     ObjectSet(LineName[y],OBJPROP_RAY , false);
                  }
                else
                  {
                     ObjectSet(LineName[y],OBJPROP_TIME2 , iTime(TradeSymbol[iii],GlobalPeriod,0));
                     ObjectSet(LineName[y],OBJPROP_TIME1 , startbars);
                     ObjectSet(LineName[y],OBJPROP_PRICE1 ,LinePrice[y,iii]);
                     ObjectSet(LineName[y],OBJPROP_PRICE2 ,LinePrice[y,iii]);
                  }      
            }
       timeolddraw[iii]=iTime(TradeSymbol[iii],GlobalPeriod,1);  
      }
  */ 
   return(0);
  }
//+------------------------------------------------------------------+