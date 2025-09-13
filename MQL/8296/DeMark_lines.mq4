//------------------------------------------------------------------/
// Indicator-advisor based on Demark threory with additions
// Some part of the code was taken from Ind-TD-DeMark-3-1.mq4 [Kara ©]
//------------------------------------------------------------------/
#property copyright "GreenDog" 
#property link      "krot@inbox.ru" // v2.3

#property indicator_chart_window 
#property indicator_buffers 2
#property indicator_color1 Red 
#property indicator_color2 Blue 



extern int showBars=3000; // if showBars= 0 so the indicator will be shown for all the chart
extern int LevDP=2; //  Demar Pint Levels; 2 = central bar will be higher (lower) then 2 bars on the left side and 2 bars on the right one.
extern int qSteps=1; // number of steps/ no more than 3
extern int BackStep=0; // number of staps back
extern int startBar=0; // ifstartBar=0 so we have recommendation for the current bar, if 1 - for possible next bar
extern bool TrendLine=false; // false = no trend lines
extern bool HorizontLine=false; // true = break levels
extern bool ChannelLine=false; // true = channels which is parallel with trend lines 
extern bool TakeLines=false; // true = take profit lines
extern bool Comments=false; // true = comments

extern int Trend=0; // 1 = for UpTrendLines only, -1 = for DownTrendLines only, 0 = for all TrendLines
extern bool ShowArrows=true;
extern bool CustomFeatures=true;
extern color UpTrendColor=Green;
extern color DownTrendColor=Red;

extern int TrendlineWidth=3;
extern bool ShowAlerts=false;
extern bool EmailAlert=false;
int trendwidth;
color trendcol;
//int ColNum[];
double Buf1[]; 
double Buf2[]; 



int    DotPip=10;

//---- buffers


double BuyBuffer[];
double SellBuffer[];




string Col[]={"Red","DarkBlue","Coral","DodgerBlue","SaddleBrown","MediumSeaGreen"};
//int ColNum[1]=coll1; //={coll1,coll2,coll3,coll4,coll5,coll6};
int ColNum[]={Red,DeepSkyBlue,Coral,Aqua,SaddleBrown,MediumSeaGreen}; //DeepSkyBlue
int qPoint=0; // переменна€ дл€ нормализации цены

int qBars; double qTime=0; // переменные дл€ ликвидации глюков при загрузке

int init() 
  {
	IndicatorBuffers(4);
   qBars=Bars;
   qSteps=MathMin(3,qSteps);
   while (NormalizeDouble(Point,qPoint)==0)qPoint++;
   int code=161; string Rem="DLines © GameOver";
   IndicatorShortName(Rem); 
   if (ShowArrows){
   SetIndexStyle(0,DRAW_ARROW); 
   SetIndexStyle(1,DRAW_ARROW); 
   }
   SetIndexArrow(0,code); 
   SetIndexArrow(1,code); 
   SetIndexBuffer(0,Buf1); 
   SetIndexBuffer(1,Buf2); 
   SetIndexEmptyValue(0,0.0);
   SetIndexEmptyValue(1,0.0);
   SetIndexLabel(0,Rem); 
   SetIndexLabel(1,Rem); 

SetIndexStyle(2,DRAW_ARROW,EMPTY,2);
SetIndexArrow(2,241);
SetIndexBuffer(2, BuyBuffer);

SetIndexStyle(3,DRAW_ARROW,EMPTY,2);
SetIndexArrow(3,242);
SetIndexBuffer(3, SellBuffer);


   return(0);
  } 

int deinit() {
   ObjectsDeleteAll();
   
   Comment("");
   ArrayInitialize(Buf1,0.0);
   ArrayInitialize(Buf2,0.0);
   for(int i=1;i<=LevDP;i++){
      ObjectDelete("HA_"+i);ObjectDelete("LA_"+i);
      ObjectDelete("HL_"+i);ObjectDelete("LL_"+i);
      ObjectDelete("HHL_"+i);ObjectDelete("HLL_"+i);
      ObjectDelete("HCL_"+i);ObjectDelete("LCL_"+i);
      for(int j=0;j<4;j++) {ObjectDelete("HTL_"+i+j);ObjectDelete("LTL_"+i+j);}
   }
}
  
int start(){

   
   
   
   if (qBars!=Bars){ deinit(); Sleep(1000); qBars=Bars; qTime=0; return(0);}
   if (qTime==Time[0]) return(0); qTime=Time[0]; // запускаеца тока на 1м тике
   // заполнили и отобразили точки демарка
   if (showBars==0) showBars=Bars-LevDP;
   for (int cnt=showBars;cnt>LevDP;cnt--) {
      Buf1[cnt]=DemHigh(cnt,LevDP);
      Buf2[cnt]=DemLow(cnt,LevDP);
   }
   
   string Comm;
   double j;
   int m,s,k;
   m=Time[0]+Period()*60-CurTime();
   j=m/60.0;
   s=m%60;
   m=(m-m%60)/60;
   Comm=Comm+"Demark with alerts";
   Comm=Comm+( m + " minutes " + s + " seconds left to bar end")+"\n";
   if (Comments){Comment(Comm);}else{Comment("Demark with alerts" + "\n" + m + " minutes " + s + " seconds left to bar end" +"\n");}
   
   Comm=Comm+"Trend Line["+On(TrendLine)+"]; Channel ["+On(ChannelLine)+
   "]; Breakout level ["+On(HorizontLine)+"]; Targets ["+On(TakeLines)+"]\n";
   for(cnt=1;cnt<=qSteps;cnt++) Comm=Comm+(TDMain(cnt));
   Comm=Comm+"ЧЧЧЧ © GameOver ЧЧЧЧ";
   if (Comments==true) Comment(Comm);
   return(0); 
}

string TDMain(int Step){
   int H1,H2,L1,L2,qExt,i,col;
   double tmp,qTL,qLevel,HT[4],LT[4];
   bool isHitch;
   string Comm="їЧїЧї Step "+Step+" of "+qSteps+" (BackStep "+BackStep+")\n",Text,Rem,qp;
   static datetime timealertupper, timealertlower;
   
   // дл€ DownTrendLines
   if (Trend<=0){
      Comm=Comm+"ї "+Col[Step*2-2]+" DownTrendLine ";
      col=ColNum[Step*2-2];
      H1=GetTD(Step+BackStep,Buf1);
      H2=GetNextHighTD(H1);
      qTL=(High[H2]-High[H1])/(H2-H1);
      qExt=Lowest(NULL,0,MODE_LOW,H2-H1-1,H1+1); // локальный минимум между точками
      qLevel=High[H2]-qTL*(H2); if (Step+BackStep==1) qLevel=qLevel-qTL*startBar;
      if (H1<0 || H2<0) Comm=Comm+"not enough points on the chart for construction\n";
      else {
         Comm=Comm+"["+DoubleToStr(High[H2],qPoint)+"ї"+DoubleToStr(High[H1],qPoint)+"]";
         Comm=Comm+"; Level "+DoubleToStr(qLevel,qPoint);
         if (Step+BackStep==1) {
            if (startBar>0) Comm=Comm+"; Future Bar "+UpHitch(-1,qLevel);
            else Comm=Comm+"; Last Bar "+UpHitch(startBar,qLevel);
         }
         Comm=Comm+"\n";
         // јнализ - был ли пробой трендовой линии
         i=H1;isHitch=false;Text="";         
         while(i>0 && isHitch==false){
         BuyBuffer[i]=0;
            tmp=High[H2]-qTL*(H2-i);
            Rem="HA_"+Step;
            if (Close[i]>tmp){
               qp=UpHitch(i,tmp);
               if (qp!=""){
                  isHitch=true;
                  Text=Text+"Source "+DoubleToStr(tmp,qPoint)+" "+qp;
                  BuyBuffer[i]=Low[i]-DotPip*Point;
                  if (timealertupper!=Time[i]){
                  
                  if (ShowAlerts){
                  Alert ("MOUTEKI: Upper trendline broken on " + Symbol() + " TF: " + Period() + " @ " + Bid);
                  }
                  
                  ObjectCreate(Rem,OBJ_ARROW,0,0,0,0,0);
                  ObjectSet(Rem,OBJPROP_COLOR,col);
                  ObjectSet(Rem,OBJPROP_PRICE1,Low[i]-Point);
                  ObjectSet(Rem,OBJPROP_TIME1,Time[i]);
                  ObjectSet(Rem,OBJPROP_ARROWCODE,241);
                  //}
                  if (EmailAlert){
                     SendMail("MOUTEKI: Upper trendline broken on " + Symbol() + " TF: " + Period() + " @ " + Bid, "MOUTEKI: Upper trendline broken on " + Symbol() + " TF: " + Period() + " @ " + Bid + "\n" + "Alerts by LightKeeper. martkoz@optusnet.com.au");
                  }
                  timealertupper=Time[i];
                  }
                  /////////////////////////////////////
                  ObjectCreate(Rem,OBJ_ARROW,0,0,0,0,0);
                  ObjectSet(Rem,OBJPROP_COLOR,col);
                  ObjectSet(Rem,OBJPROP_PRICE1,Low[i]-Point);
                  ObjectSet(Rem,OBJPROP_TIME1,Time[i]);
                  ObjectSet(Rem,OBJPROP_ARROWCODE,241);
                  ////////////////////////////////////
                  if (ShowArrows==true){
                  while(i>0){ // пробой отменен, если после пробо€ был новый лоу или закрытие ниже
                     i--;
                     if (Low[i]<Low[qExt] || Close[i]<(Low[qExt]+(High[H1]-Low[qExt])*0.236)){                        
                        Text=Text+" (cancel)";
                        ObjectSet(Rem,OBJPROP_PRICE1,Low[i]-Point);
                        ObjectSet(Rem,OBJPROP_TIME1,Time[i]);
                        ObjectSet(Rem,OBJPROP_ARROWCODE,251);
                        break;
                     }
                  }
                  }
               }
               else { Text=Text+"False "+DoubleToStr(tmp,qPoint)+"; "; ObjectDelete(Rem);}
            }
            i--;
         }
         if (Text=="") Text="No breakout";
         Comm=Comm+Text+"\n";
         // end analysis
         Rem="HL_"+Step; // собсно лини€ тренда
         if (CustomFeatures){trendcol=DownTrendColor;}else{trendcol=col;}
         if (TrendLine){
            if (CustomFeatures==false){trendwidth=3-MathMin(2,Step);}else{trendwidth=TrendlineWidth;}
            ObjectCreate(Rem,OBJ_TREND,0,0,0,0,0);
            ObjectSet(Rem,OBJPROP_TIME1,Time[H2]);ObjectSet(Rem,OBJPROP_TIME2,Time[H1]);
            ObjectSet(Rem,OBJPROP_PRICE1,High[H2]);ObjectSet(Rem,OBJPROP_PRICE2,High[H1]);
            ObjectSet(Rem,OBJPROP_COLOR,trendcol);
            ObjectSet(Rem,OBJPROP_WIDTH,trendwidth);
         }    
         else ObjectDelete(Rem);
         Rem="HHL_"+Step; // уровень пробо€ линии тренда
         if (HorizontLine && (Step+BackStep)==1){
            ObjectCreate(Rem,OBJ_HLINE,0,0,0,0,0);
            ObjectSet(Rem,OBJPROP_PRICE1,qLevel);
            ObjectSet(Rem,OBJPROP_COLOR,trendcol);
         }
         else ObjectDelete(Rem);
         Rem="HCL_"+Step; // лини€ канала
         if (ChannelLine){
            ObjectCreate(Rem,OBJ_TREND,0,0,0,0,0);
            ObjectSet(Rem,OBJPROP_TIME1,Time[qExt]);ObjectSet(Rem,OBJPROP_TIME2,Time[0]);
            ObjectSet(Rem,OBJPROP_PRICE1,Low[qExt]);ObjectSet(Rem,OBJPROP_PRICE2,Low[qExt]-qTL*qExt);
            ObjectSet(Rem,OBJPROP_COLOR,trendcol);
         }
         else ObjectDelete(Rem);
         Rem="HTL_"+Step; // линии целей
         if (TakeLines){
            HT[3]=Low[qExt]+(High[H1]-Low[qExt])*1.618-qLevel; //  доп уровень
            HT[0]=High[H2]-qTL*(H2-qExt)-Low[qExt];
            HT[1]=High[H2]-qTL*(H2-qExt)-Close[qExt];
            qExt=Lowest(NULL,0,MODE_CLOSE,H2-H1,H1);
            HT[2]=High[H2]-qTL*(H2-qExt)-Close[qExt];
            Comm=Comm+"Targets: ";
            for(i=0;i<4;i++){
               qTL=NormalizeDouble(qLevel+HT[i],qPoint);
               ObjectCreate(Rem+i,OBJ_HLINE,0,0,0,0,0);
               ObjectSet(Rem+i,OBJPROP_PRICE1,qTL);
               ObjectSet(Rem+i,OBJPROP_STYLE,STYLE_DOT);
               ObjectSet(Rem+i,OBJPROP_COLOR,trendcol);
               Comm=Comm+DoubleToStr(qTL,qPoint)+" ("+DoubleToStr(HT[i]/Point,0)+"p.) ";
             }
             Comm=Comm+"\n";
         }
         else {
            for(i=0;i<4;i++) ObjectDelete(Rem+i);
         }
      }
   }

   // дл€ UpTrendLines
   if (Trend>=0){
      Comm=Comm+"ї "+Col[Step*2-1]+" UpTrendLine ";
      col=ColNum[Step*2-1];
      L1=GetTD(Step+BackStep,Buf2);
      L2=GetNextLowTD(L1);
      qTL=(Low[L1]-Low[L2])/(L2-L1);
      qExt=Highest(NULL,0,MODE_HIGH,L2-L1-1,L1+1); // локальный минимум между точками
      qLevel=Low[L2]+qTL*L2; if (Step+BackStep==1) qLevel=qLevel+qTL*startBar;
      if (L1<0 || L2<0) Comm=Comm+"not enough points on the chart for construction\n";
      else {
         Comm=Comm+"["+DoubleToStr(Low[L2],qPoint)+"ї"+DoubleToStr(Low[L1],qPoint)+"]";
         Comm=Comm+"; Level "+DoubleToStr(qLevel,qPoint);
         if (Step+BackStep==1) {
            if (startBar>0) Comm=Comm+"; Future Bar "+DownHitch(-1,qLevel);
            else Comm=Comm+"; Last Bar "+DownHitch(startBar,qLevel);
         }
         Comm=Comm+"\n";
         // јнализ - был ли пробой трендовой линии
         i=L1;isHitch=false;Text="";
         while(i>0 && isHitch==false){
         SellBuffer[i]=0;
            tmp=Low[L2]+qTL*(L2-i);
            Rem="LA_"+Step;
            if (Close[i]<tmp){
               qp=DownHitch(i,tmp);
               if (qp!=""){
                  isHitch=true;
                  Text=Text+"Source "+DoubleToStr(tmp,qPoint)+" "+qp;
                  SellBuffer[i]=High[i]+DotPip*Point;
                  if (timealertlower!=Time[i]){
                        
                      if(ShowAlerts){
                        Alert ("MOUTEKI: Lower trendline broken on " + Symbol() + " TF: " + Period() + " @ " + Bid);
                        }
                  ObjectCreate(Rem,OBJ_ARROW,0,0,0,0,0);
                  ObjectSet(Rem,OBJPROP_COLOR,col);
                  ObjectSet(Rem,OBJPROP_PRICE1,Low[i]-Point);
                  ObjectSet(Rem,OBJPROP_TIME1,Time[i]);
                  ObjectSet(Rem,OBJPROP_ARROWCODE,242);
                  //}
                  
                  if (EmailAlert){
                     SendMail("MOUTEKI: Lower trendline broken on " + Symbol() + " TF: " + Period() + " @ " + Bid, "MOUTEKI: Lower trendline broken on " + Symbol() + " TF: " + Period() + " @ " + Bid + "\n" + "Alerts by LightKeeper. martkoz@optusnet.com.au");
                  }
                  
                  timealertlower=Time[i];
                  }
                  ///////////////////////////////
                  ObjectCreate(Rem,OBJ_ARROW,0,0,0,0,0);
                  ObjectSet(Rem,OBJPROP_COLOR,col);
                  ObjectSet(Rem,OBJPROP_PRICE1,Low[i]-Point);
                  ObjectSet(Rem,OBJPROP_TIME1,Time[i]);
                  ObjectSet(Rem,OBJPROP_ARROWCODE,242);
                  //////////////////////////////
                  if (ShowArrows==true){ 
                  while(i>0){ // пробой отменен, если после пробо€ был новый хай или закрытие выше
                     i--;
                     if (High[i]>High[qExt] || Close[i]>High[qExt]-(High[qExt]-Low[L1])*0.236){
                        Text=Text+" (cancel)";
                        ObjectSet(Rem,OBJPROP_PRICE1,Low[i]-Point);
                        ObjectSet(Rem,OBJPROP_TIME1,Time[i]);
                        ObjectSet(Rem,OBJPROP_ARROWCODE,251);
                        break;
                     }
                  }
               } }
               else { Text=Text+"False "+DoubleToStr(tmp,qPoint)+"; "; ObjectDelete(Rem);}
            }
            i--;
         }
         if (Text=="") Text="No breakout";
         Comm=Comm+Text+"\n";
         // end analysis
         Rem="LL_"+Step; // собсно лини€ тренда
         if (CustomFeatures){trendcol=UpTrendColor;}else{trendcol=col;}
         if (TrendLine==1) {
            if (CustomFeatures==false){trendwidth=3-MathMin(2,Step);}else{trendwidth=TrendlineWidth;}
            ObjectCreate(Rem,OBJ_TREND,0,0,0,0,0);
            ObjectSet(Rem,OBJPROP_TIME1,Time[L2]);ObjectSet(Rem,OBJPROP_TIME2,Time[L1]);
            ObjectSet(Rem,OBJPROP_PRICE1,Low[L2]);ObjectSet(Rem,OBJPROP_PRICE2,Low[L1]);
            ObjectSet(Rem,OBJPROP_COLOR,trendcol);
            ObjectSet(Rem,OBJPROP_WIDTH,trendwidth);
         }    
         else ObjectDelete(Rem);
         Rem="HLL_"+Step; // уровень пробо€ линии тренда
         if (HorizontLine && (Step+BackStep)==1){
            ObjectCreate(Rem,OBJ_HLINE,0,0,0,0,0);
            ObjectSet(Rem,OBJPROP_PRICE1,qLevel);
            ObjectSet(Rem,OBJPROP_COLOR,trendcol);
         }
         else ObjectDelete(Rem);
         Rem="LCL_"+Step; // лини€ канала
         if (ChannelLine){
            ObjectCreate(Rem,OBJ_TREND,0,0,0,0,0);
            ObjectSet(Rem,OBJPROP_TIME1,Time[qExt]);ObjectSet(Rem,OBJPROP_TIME2,Time[0]);
            ObjectSet(Rem,OBJPROP_PRICE1,High[qExt]);ObjectSet(Rem,OBJPROP_PRICE2,High[qExt]+qTL*qExt);
            ObjectSet(Rem,OBJPROP_COLOR,trendcol);
         }
         else ObjectDelete(Rem);
         Rem="LTL_"+Step;
         if (TakeLines){ // линии целей
            LT[3]=qLevel-High[qExt]+(High[qExt]-Low[L1])*1.618; // доп уровень
            LT[0]=High[qExt]-qTL*(L2-qExt)-Low[L2];
            LT[1]=Close[qExt]-qTL*(L2-qExt)-Low[L2];
            qExt=Highest(NULL,0,MODE_CLOSE,L2-L1,L1);
            LT[2]=Close[qExt]-qTL*(L2-qExt)-Low[L2];
            Comm=Comm+"Targets: ";
            for(i=0;i<4;i++){
               qTL=NormalizeDouble(qLevel-LT[i],qPoint);
               ObjectCreate(Rem+i,OBJ_HLINE,0,0,0,0,0);
               ObjectSet(Rem+i,OBJPROP_PRICE1,qTL);
               ObjectSet(Rem+i,OBJPROP_STYLE,STYLE_DOT);
               ObjectSet(Rem+i,OBJPROP_COLOR,trendcol);
               Comm=Comm+DoubleToStr(qTL,qPoint)+" ("+DoubleToStr(LT[i]/Point,0)+"p.) ";
             }
            Comm=Comm+"\n";
         }
         else {
            for(i=0;i<4;i++) ObjectDelete(Rem+i);
         }
      }
   }
   return(Comm);
}

int GetTD(int P, double Arr[]){
   int i=0,j=0;
   while(j<P){ i++; while(Arr[i]==0){i++;if(i>showBars-2)return(-1);} j++;}
   return (i);         
}
int GetNextHighTD(int P){ 
   int i=P+1;
   while(Buf1[i]<=High[P]){i++;if(i>showBars-2)return(-1);}
   return (i);
}
int GetNextLowTD(int P){
   int i=P+1;
   while(Buf2[i]>=Low[P] || Buf2[i]==0){i++;if(i>showBars-2)return(-1);}
   return (i);
}
// рекурсивна€ проверка на услови€ ƒемарка (хай), возвращает значение или 0
double DemHigh(int cnt, int sh){
   if (High[cnt]>=High[cnt+sh] && High[cnt]>High[cnt-sh]) {
      if (sh>1) return(DemHigh(cnt,sh-1));
      else return(High[cnt]);
   }
   else return(0);
}
// рекурсивна€ проверка на услови€ ƒемарка (лоу), возвращает значение или 0
double DemLow(int cnt, int sh){
   if (Low[cnt]<=Low[cnt+sh] && Low[cnt]<Low[cnt-sh]) {
      if (sh>1) return(DemLow(cnt,sh-1));
      else return(Low[cnt]);
   }
   else return(0);
}
string On(bool On){
   if (On) return("on"); else return("off");
}
string UpHitch(int P, double qLevel){ // определение квалификаторов прорыва вверх
   string Comm="";
   if (Close[P+1]<Close[P+2]) Comm=Comm+" 1";
   if (P>=0 && Open[P]>qLevel) Comm=Comm+" 2";
   if (2*Close[P+1]-Low[P+1]<qLevel) Comm=Comm+" 3";
   if (Comm!="") Comm="[ Break Qualificator:"+Comm+" ]";
   return(Comm);
}
string DownHitch(int P, double qLevel){ // определение квалификаторов прорыва вниз
   string Comm="";
   if (Close[P+1]>Close[P+2]) Comm=Comm+" 1";
   if (P>=0 && Open[P]<qLevel) Comm=Comm+" 2";
   if (2*Close[P+1]-High[P+1]>qLevel) Comm=Comm+" 3";
   if (Comm!="") Comm="[ Break Qualificator:"+Comm+" ]";
   return(Comm);
}