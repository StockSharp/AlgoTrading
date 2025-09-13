//+------------------------------------------------------------------+
//|                                                Shuriken Lite.mq4 |
//|                         Copyright © 2011 http://www.fxtools.info |
//-------------------------------------------------------------------+
#property copyright "© FX Tools 2011"
#property link "http://www.fxtools.info"

extern string Expert = "== Shuriken Lite © FX Tools ==";
extern string Link = "=====  www.fxtools.info  =====";
extern string MagicNumbers = "__Magic Numbers__";
extern int Magic_Expert1 = 1;
extern int Magic_Expert2 = 2;
extern int Magic_Expert3 = 3;
extern int Magic_Expert4 = 4;
extern int Magic_Expert5 = 5;
extern int Magic_Expert6 = 6;
extern int Magic_Expert7 = 7;
extern int Magic_Expert8 = 8;
extern int Magic_Expert9 = 9;
extern int Magic_Expert10 = 10;
extern string Display = "__Display Settings__";
extern bool ShowScores = true;
extern bool ShowBackground = true;
extern int X_Pos = 10;
extern int Y_Pos = 10;
extern string Colors = "__Colors__";
extern color Winning = LimeGreen;
extern color Neutral = DarkKhaki;
extern color Losing = OrangeRed;
extern color Text = Silver;
extern color Titles = DimGray;
extern color Background = Black;
//+------------------------------------------------------------------+
int points;
double AccBal;
int DisplaySize=180;
double trades[10],profits[10],losses[10],pips[10];
double totaltrades,totalprofits,totallosses,totalpips,totalwins;
double profitfactor,totalprofit,totalloss,profit;
color rank_col[10];
double score_array[10];
int rank_array[10];
string name_array[10];
//+------------------------------------------------------------------+
int init(){points = 1;
if (Digits==2 || Digits==3 || Digits==5){points = 10;}
if(ShowScores) {initDisplay(); updateDisplay();}
return(0);}
//+------------------------------------------------------------------+
int deinit(){
if(ShowScores) updateDisplay();
ObjectsDeleteAll(0,OBJ_LABEL);
Print("shutdown error - ",GetLastError());                               
return(0);}
//+------------------------------------------------------------------+
int start(){

if (AccBal!=AccountBalance() && ShowScores) {updateDisplay();}
AccBal=AccountBalance();

return(0);}
//+------------------------------------------------------------------+

void objectCreate(string name,int x,int y,string text="-",int size=42,string font="Arial",color colour=CLR_NONE)
{
  ObjectCreate(name,OBJ_LABEL,0,0,0);
  ObjectSet(name,OBJPROP_CORNER,3);
  ObjectSet(name,OBJPROP_COLOR,colour);
  ObjectSet(name,OBJPROP_XDISTANCE,x);
  ObjectSet(name,OBJPROP_YDISTANCE,y);
  ObjectSetText(name,text,size,font,colour);
}
  
void updateDisplay() 
{
  ScanHistory(); 
  
 for (int a=0; a<10; a++) 
  {
  if(pips[a]>0) rank_col[a] = Winning;
  if(pips[a]<0) rank_col[a] = Losing;
  if(trades[a]==0 || pips[a]==0) rank_col[a] = Neutral;
  }
 for (int b=0; b<10; b++) 
  {
  if(trades[b]>0) {score_array[b] = NormalizeDouble((profits[b]/trades[b])*100,1);} else score_array[b] = 0;
  }
 for (int c=0; c<10; c++) 
  {
  name_array[c]="Stgy"+rank_array[c]+"";
  }
 for (int d=0; d<10; d++) 
  {
  ObjectSetText("ZS"+(d+1)+"_Pips",DoubleToStr(pips[d],0));
  ObjectSetText("ZS"+(d+1)+"_Score",DoubleToStr(trades[d],0));
  ObjectSetText("ZS"+(d+1)+"_Score%",DoubleToStr(score_array[d],0));
  ObjectSetText("ZS"+(d+1)+"_%"," %");
  ObjectSet("ZS"+(d+1)+"_Pips",OBJPROP_COLOR,rank_col[d]);
  ObjectSet("ZS"+(d+1)+"_Score",OBJPROP_COLOR,rank_col[d]);
  ObjectSet("ZS"+(d+1)+"_Score%",OBJPROP_COLOR,rank_col[d]);
  ObjectSet("ZS"+(d+1)+"_%",OBJPROP_COLOR,rank_col[d]);
  ObjectSet("ZS"+(d+1)+"_TotPips",OBJPROP_COLOR,rank_col[d]);
  ObjectSet("ZS"+(d+1)+"_TotScore",OBJPROP_COLOR,rank_col[d]);
  ObjectSet("ZS"+(d+1)+"_TotScore%",OBJPROP_COLOR,rank_col[d]);
  }
  ObjectSetText("Z_TotPips",DoubleToStr(totalpips,0));
  ObjectSetText("Z_TotScore",DoubleToStr(totaltrades,0));
  ObjectSetText("Z_TotScore%",DoubleToStr(totalwins,0));
  ObjectSetText("Z_TotPF",DoubleToStr(profitfactor,2));
  ObjectSetText("Z_TotProfit",DoubleToStr(profit,2)); 

WindowRedraw();
}

void initDisplay() 
{
   ObjectsDeleteAll(0,OBJ_LABEL);
   
  objectCreate("Shuriken",X_Pos,Y_Pos,"O",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken",OBJPROP_COLOR,C'17,28,36');
  objectCreate("Shuriken2",X_Pos,Y_Pos,"U",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken2",OBJPROP_COLOR,C'36,22,10');
  objectCreate("Shuriken3",X_Pos,Y_Pos,"M",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken3",OBJPROP_COLOR,C'85,66,46');
  objectCreate("ZShuriken4",X_Pos,Y_Pos,"T",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("ZShuriken4",OBJPROP_COLOR,C'92,91,87');
  objectCreate("ZShuriken2",X_Pos,Y_Pos,"S",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("ZShuriken2",OBJPROP_COLOR,Black);
  objectCreate("ZShuriken3",X_Pos,Y_Pos,"R",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("ZShuriken3",OBJPROP_COLOR,C'46,55,65');
  objectCreate("ZShuriken5",X_Pos,Y_Pos,"Q",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("ZShuriken5",OBJPROP_COLOR,C'214,172,19');
  objectCreate("ZShuriken",X_Pos,Y_Pos,"P",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("ZShuriken",OBJPROP_COLOR,C'7,17,28');
  objectCreate("Shuriken4",X_Pos,Y_Pos,"N",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken4",OBJPROP_COLOR,C'217,184,136'); 
  objectCreate("Shuriken5",X_Pos,Y_Pos,"L",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken5",OBJPROP_COLOR,C'116,0,0');
  objectCreate("Shuriken6",X_Pos,Y_Pos,"K",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken6",OBJPROP_COLOR,C'81,0,0');
  objectCreate("Shuriken7",X_Pos,Y_Pos,"J",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken7",OBJPROP_COLOR,C'81,0,0');
  objectCreate("Shuriken10",X_Pos,Y_Pos,"G",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken10",OBJPROP_COLOR,C'116,0,0');
  objectCreate("Shuriken11",X_Pos,Y_Pos,"F",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken11",OBJPROP_COLOR,C'81,0,0');
  objectCreate("Shuriken12",X_Pos,Y_Pos,"E",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken12",OBJPROP_COLOR,C'131,28,0');
  objectCreate("Shuriken16",X_Pos,Y_Pos,"a",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken16",OBJPROP_COLOR,C'55,55,52');
  objectCreate("Shuriken17",X_Pos,Y_Pos,"b",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken17",OBJPROP_COLOR,C'12,7,10');
  objectCreate("Shuriken18",X_Pos,Y_Pos,"c",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken18",OBJPROP_COLOR,C'12,7,10');
  objectCreate("Shuriken19",X_Pos,Y_Pos,"d",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken19",OBJPROP_COLOR,C'12,7,10');
  objectCreate("Shuriken20",X_Pos,Y_Pos,"e",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken20",OBJPROP_COLOR,C'12,7,10');  
  objectCreate("Shuriken21",X_Pos,Y_Pos,"D",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken21",OBJPROP_COLOR,C'88,88,77');
  objectCreate("Shuriken22",X_Pos,Y_Pos,"C",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken22",OBJPROP_COLOR,C'88,88,77');
  objectCreate("Shuriken23",X_Pos,Y_Pos,"B",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken23",OBJPROP_COLOR,C'88,88,77');
  objectCreate("Shuriken24",X_Pos,Y_Pos,"A",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken24",OBJPROP_COLOR,C'88,88,77');
 if (ShowBackground)
 {objectCreate("Shuriken25",X_Pos,Y_Pos,"7",DisplaySize,"Shuriken",CLR_NONE);
  ObjectSet("Shuriken25",OBJPROP_COLOR,Background);}
 
  objectCreate("Z_Rank",X_Pos+59,Y_Pos+200,"-------- SCORES --------",8,"Times",Text);
  objectCreate("ZShurikenKey",X_Pos+48,Y_Pos+60,"Expert   Pips   Wins   Trades",8,"Times",Titles);
  objectCreate("ZShurikenTotal",X_Pos+153,Y_Pos+42,"Total:",8,"Times",Titles);
  objectCreate("ZShuriken%",X_Pos+83,Y_Pos+42,"%",8,"Times",Text);
  objectCreate("Z_TotPips",X_Pos+116,Y_Pos+42," ",8,"Times",Text);
  objectCreate("Z_TotScore",X_Pos+58,Y_Pos+42," ",8,"Times",Text);
  objectCreate("Z_TotScore%",X_Pos+93,Y_Pos+42," ",8,"Times",Text);
  objectCreate("Z_ProfitFactor",X_Pos+140,Y_Pos+24,"Profit Factor:",8,"Times",Titles);
  objectCreate("Z_TotPF",X_Pos+116,Y_Pos+24," ",8,"Times",Text);
  objectCreate("Z_Profits",X_Pos+82,Y_Pos+24,"Profit:",8,"Times",Titles);
  objectCreate("Z_TotProfit",X_Pos+28,Y_Pos+24," ",8,"Times",Text);
  objectCreate("ZShurikenTitle",X_Pos+16,Y_Pos+6,"Shuriken Lite © FX Tools, www.fxtools.info",8,"Times",Titles);
  
 string numbers[10] = {"1.","2.","3.","4.","5.","6.","7.","8.","9.","10."};
 for (int a=0; a<10; a++) 
  {
  objectCreate("Z"+(a+1)+"_Rank",X_Pos+144,Y_Pos+188,numbers[a],8,"Times",Text);
  ObjectSet("Z"+(a+1)+"_Rank",OBJPROP_XDISTANCE,X_Pos+144);
  ObjectSet("Z"+(a+1)+"_Rank",OBJPROP_YDISTANCE,Y_Pos+188-(12*a));
  }
   ObjectSet("Z10_Rank",OBJPROP_XDISTANCE,X_Pos+140);
 for (int c=0; c<10; c++) 
  {
  objectCreate("ZS"+(c+1)+"_Score",X_Pos+59,Y_Pos+188," ",8,"Times",Neutral);
  ObjectSet("ZS"+(c+1)+"_Score",OBJPROP_YDISTANCE,Y_Pos+188-(12*c));
  }
 for (int d=0; d<10; d++) 
  {
  objectCreate("ZS"+(d+1)+"_%",X_Pos+84,Y_Pos+188," ",6,"Times",Neutral);
  ObjectSet("ZS"+(d+1)+"_%",OBJPROP_YDISTANCE,Y_Pos+188-(12*d));
  }
 for (int e=0; e<10; e++) 
  {
  objectCreate("ZS"+(e+1)+"_Score%",X_Pos+93,Y_Pos+188," ",8,"Times",Neutral);
  ObjectSet("ZS"+(e+1)+"_Score%",OBJPROP_YDISTANCE,Y_Pos+188-(12*e));
  }
 for (int f=0; f<10; f++) 
  {
  objectCreate("ZS"+(f+1)+"_Pips",X_Pos+116,Y_Pos+188," ",8,"Times",Neutral);
  ObjectSet("ZS"+(f+1)+"_Pips",OBJPROP_YDISTANCE,Y_Pos+188-(12*f));
  }

  WindowRedraw();
}


void ScanHistory()
{
ArrayInitialize(trades,0);
ArrayInitialize(profits,0);
ArrayInitialize(losses,0);
ArrayInitialize(pips,0);
totalprofit=0;totalloss=0;totalpips=0;
totaltrades=0;totalprofits=0;totallosses=0;profit=0;
totalwins=0; profitfactor=0;

int total = HistoryTotal();
   for(int cnt=0; cnt<total; cnt++) 
   {        
   OrderSelect(cnt,SELECT_BY_POS,MODE_HISTORY);
     if(OrderType()<=OP_SELL)
     { 
      if(OrderMagicNumber()==Magic_Expert1)
      {trades[0]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[0]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap(); pips[0]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[0]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[0]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
      if(OrderMagicNumber()==Magic_Expert2)
      {trades[1]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[1]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap();pips[1]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[1]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[1]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
      if(OrderMagicNumber()==Magic_Expert3)
      {trades[2]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[2]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap();pips[2]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[2]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[2]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
      if(OrderMagicNumber()==Magic_Expert4) 
      {trades[3]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[3]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap();pips[3]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[3]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[3]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
      if(OrderMagicNumber()==Magic_Expert5) 
      {trades[4]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[4]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap();pips[4]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[4]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[4]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
      if(OrderMagicNumber()==Magic_Expert6) 
      {trades[5]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[5]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap();pips[5]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[5]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[5]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
      if(OrderMagicNumber()==Magic_Expert7) 
      {trades[6]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[6]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap();pips[6]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[6]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[6]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
      if(OrderMagicNumber()==Magic_Expert8) 
      {trades[7]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[7]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap();pips[7]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[7]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[7]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
      if(OrderMagicNumber()==Magic_Expert9) 
      {trades[8]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[8]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap(); pips[8]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[8]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[8]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
      if(OrderMagicNumber()==Magic_Expert10) 
      {trades[9]++; points = 10;
      if (MarketInfo(OrderSymbol(),MODE_DIGITS)==4){points = 1;}
      if(OrderProfit()>=0) {profits[9]++; totalprofit+=OrderProfit()+OrderCommission()+OrderSwap(); pips[9]+=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      if(OrderProfit()<0) {losses[9]++; totalloss+=MathAbs(OrderProfit()+OrderCommission()+OrderSwap()); pips[9]-=(MathMax(OrderOpenPrice(),OrderClosePrice())-MathMin(OrderOpenPrice(),OrderClosePrice()))/(MarketInfo(OrderSymbol(),MODE_POINT)*points);}
      }
     }
   }   
   totaltrades=trades[0]+trades[1]+trades[2]+trades[3]+trades[4]+trades[5]+trades[6]+trades[7]+trades[8]+trades[9];
   totalprofits=profits[0]+profits[1]+profits[2]+profits[3]+profits[4]+profits[5]+profits[6]+profits[7]+profits[8]+profits[9];
   totallosses=losses[0]+losses[1]+losses[2]+losses[3]+losses[4]+losses[5]+losses[6]+losses[7]+losses[8]+losses[9];
   totalpips=pips[0]+pips[1]+pips[2]+pips[3]+pips[4]+pips[5]+pips[6]+pips[7]+pips[8]+pips[9];
   profit=totalprofit-totalloss;
   if (totaltrades>0) {totalwins=NormalizeDouble((totalprofits/totaltrades)*100,1);}
   if (totalloss>0) profitfactor=NormalizeDouble(totalprofit/totalloss,2);
   return(0);
}