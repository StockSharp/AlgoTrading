#property copyright "Evgeny I. SHCHERBINA"
//maximus_vX.mq4
//12.04.2011
//You are free to use concepts of the advisor, reference to the author is mandatory.

extern int delay_open = 2;
extern int distance = 50;
extern int period = 20;
extern int range = 500;
extern int risk = 1500;
extern int stop_loss = 20000;
extern int trail = 500;

bool pth = false;
double all[][5],b[][2],l_max,l_min,u_max,u_min,s[][2],tprofit;
int magic, trades;

int init(){
  lower();
  upper();
  magic = AccountNumber();
}

int deinit(){
  GlobalVariableDel("consolidations");
  GlobalVariableDel("trade");
}

int start(){
  comments("search",5,15,0,"Consolid. Search: "+TimeToStr(TimeCurrent()-GlobalVariableGet("consolidations"),TIME_SECONDS)+"; Trade Operation: "+TimeToStr(TimeCurrent()-GlobalVariableGet("trade"),TIME_SECONDS),NavajoWhite);

  double pr_b,pr_s;
  ArrayResize(all,0);
  ArrayResize(b,0);
  ArrayResize(s,0);
  for(int i=0; i<OrdersTotal(); i++){
    OrderSelect(i,SELECT_BY_POS);
    if(StringFind(OrderComment(),"mv13_",0) != -1){
      ArrayResize(all,ArrayRange(all,0)+1);
      all[ArrayRange(all,0)-1][0] = OrderTicket();
      all[ArrayRange(all,0)-1][1] = OrderType();
      all[ArrayRange(all,0)-1][2] = OrderOpenTime();
      all[ArrayRange(all,0)-1][3] = IsDemo();
      all[ArrayRange(all,0)-1][4] = OrderProfit()/OrderLots();
      all[ArrayRange(all,0)-1][5] = OrderOpenPrice();
      if(OrderComment() == "mv13_buy"){
        pr_b += OrderProfit()/OrderLots();
        ArrayResize(b,ArrayRange(b,0)+1);
        b[ArrayRange(b,0)-1][0] = OrderTicket();
        b[ArrayRange(b,0)-1][1] = OrderOpenPrice();
        if(OrderStopLoss() == 0 && OrderTakeProfit() == 0 && tprofit != 0){
          OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-stop_loss*Point,tprofit,0,Green);
          tprofit = 0;
        }
      }else if(OrderComment() == "mv13_sell"){
        pr_s += OrderProfit()/OrderLots();
        ArrayResize(s,ArrayRange(s,0)+1);
        s[ArrayRange(s,0)-1][0] = OrderTicket();
        s[ArrayRange(s,0)-1][1] = OrderOpenPrice();
        if(OrderStopLoss() == 0 && OrderTakeProfit() == 0 && tprofit != 0){
          OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+stop_loss*Point,tprofit,0,Red);
          tprofit = 0;
        }
      }
      trail(OrderTicket(),OrderType(),OrderOpenPrice(),OrderStopLoss(),trail,all[ArrayRange(all,0)-1][5],1234);
    }
  }
  if(IsDemo()){pth = true;}else{pth = false;}
  if(ArrayRange(all,0) > trades){
    trades = ArrayRange(all,0);
    GlobalVariableSet("consolidations",TimeCurrent());
    GlobalVariableSet("trade",TimeCurrent());
  }
  if(ArrayRange(all,0) < trades){
    trades = ArrayRange(all,0);
    lower();
    upper();
    GlobalVariableSet("consolidations",TimeCurrent());
    GlobalVariableSet("trade",TimeCurrent());
  }
  if(GlobalVariableGet("trade")+60*15*delay_open < TimeCurrent()){
    if(ArrayRange(b,0) == 0){open_first(OP_BUY);}
    if(ArrayRange(b,0) == 1 && MathAbs(Close[0]-b[0][1]) > range*Point){
      open_first(OP_BUY);
    }
    if(ArrayRange(s,0) == 0){open_first(OP_SELL);}
    if(ArrayRange(s,0) == 1 && MathAbs(Close[0]-s[0][1]) > range*Point){
      open_first(OP_SELL);
    }
  }
  if(GlobalVariableGet("consolidations")+period*60*60 < TimeCurrent()){lower(); upper();}
}

void lower(){
  if(Close[0]-100*Point > l_max || Close[0]+100*Point < l_min){
    double max,min;
    for(int i=0; i<iBars(Symbol(),PERIOD_M15); i++){
      if(iClose(Symbol(),PERIOD_M15,0)-range*Point > iHigh(Symbol(),PERIOD_M15,i) && pth){
        max = iHigh(Symbol(),PERIOD_M15,iHighest(Symbol(),PERIOD_M15,MODE_HIGH,40,i));
        min = iLow(Symbol(),PERIOD_M15,iLowest(Symbol(),PERIOD_M15,MODE_LOW,40,i));
        if(max-min <= range*Point && Close[0]-range*Point > max && Close[0]-range*Point > min){
          l_max = max;
          l_min = min;
          draw_consolidation(l_max,"l_max","ltrend1",i,Tomato);
          draw_consolidation(l_min,"l_min","ltrend2",i,Tomato);
          comments("l_consolidation",5,30,1,"Lower: "+DoubleToStr(l_max,Digits)+"; "+DoubleToStr(l_min,Digits),NavajoWhite);
          break;
        }else{max = 0; min = 0;}
      }
    }
    if(max == 0 && min == 0){
      max = MathFloor((Close[0]-100*Point)*100)/100+range/2*Point;
      min = MathFloor((Close[0]-100*Point)*100)/100-range/2*Point;
      l_max = max;
      l_min = min;
      draw_consolidation(l_max,"l_max","ltrend1",-1,Tomato);
      draw_consolidation(l_min,"l_min","ltrend2",-1,Tomato);
      comments("l_consolidation",5,30,1,"Lower: "+DoubleToStr(l_max,Digits)+"; "+DoubleToStr(l_min,Digits),Tomato);
    }
  }
  GlobalVariableSet("consolidations",TimeCurrent());
}

void upper(){
  if((Close[0]-10*Point > u_max || Close[0]+10*Point < u_min) && trail(1,1,Close[0],Close[0],trail,1,magic)){
    double max,min;
    for(int i=0; i<iBars(Symbol(),PERIOD_M15); i++){
      if(iClose(Symbol(),PERIOD_M15,0)+range*Point < iLow(Symbol(),PERIOD_M15,i)){
        max = iHigh(Symbol(),PERIOD_M15,iHighest(Symbol(),PERIOD_M15,MODE_HIGH,40,i));
        min = iLow(Symbol(),PERIOD_M15,iLowest(Symbol(),PERIOD_M15,MODE_LOW,40,i));
        if(max-min <= range*Point && Close[0]+range*Point < max && Close[0]+range*Point < min){
          u_max = max;
          u_min = min;
          draw_consolidation(u_max,"u_max","utrend1",i,LimeGreen);
          draw_consolidation(u_min,"u_min","utrend2",i,LimeGreen);
          comments("u_consolidation",5,15,1,"Upper: "+DoubleToStr(u_max,Digits)+"; "+DoubleToStr(u_min,Digits),NavajoWhite);
          break;
        }else{max = 0; min = 0;}
      }
    }
    if(max == 0 && min == 0){
      if(Close[0] > 10){
        max = MathFloor(Close[0]+100*Point)+range/2*Point;
        min = MathFloor(Close[0]+100*Point)-range/2*Point;
      }else{
        max = MathFloor((Close[0]+100*Point)*100)/100+range/2*Point;
        min = MathFloor((Close[0]+100*Point)*100)/100-range/2*Point;
      }
      u_max = max;
      u_min = min;
      draw_consolidation(u_max,"u_max","utrend1",-1,LimeGreen);
      draw_consolidation(u_min,"u_min","utrend2",-1,LimeGreen);
      comments("u_consolidation",5,15,1,"Upper: "+DoubleToStr(u_max,Digits)+"; "+DoubleToStr(u_min,Digits),Tomato);
    }
  }
  GlobalVariableSet("consolidations",TimeCurrent());
}

void draw_consolidation(double extr, string name1, string name2, int c, color couleur){
  ObjectDelete(name1);
  ObjectCreate(name1,OBJ_HLINE,0,0,extr);
  ObjectSet(name1,OBJPROP_COLOR,couleur);
  ObjectSet(name1,OBJPROP_STYLE,STYLE_DASHDOTDOT);
  ObjectDelete(name2);
  if(c != -1){
    ObjectCreate(name2,OBJ_TREND,0,Time[c/4],extr,Time[c/4+10],extr);
    ObjectSet(name2,OBJPROP_COLOR,White);
    ObjectSet(name2,OBJPROP_RAY,false);
    ObjectSet(name2,OBJPROP_WIDTH,2);
  }
}

void open_first(int type){
  double tp;
  if(type == OP_BUY){
    if(l_max != 0 && Close[0]-distance*Point > l_max){
      if(iClose(Symbol(),PERIOD_M15,0)-distance*Point > iOpen(Symbol(),PERIOD_M15,0) && pth && iLow(Symbol(),PERIOD_M15,1) < l_max){
        tp = ((u_min-l_max)/3)*2;
        if(tp < range*Point){tp = range*Point;}
        order_send(OP_BUY,Ask,lot(),Ask+tp,"mv13_buy",2,Green);
      }
    }
    if(u_max != 0 && Close[0]-distance*Point > u_max){
      if(iClose(Symbol(),PERIOD_M15,0)-distance*Point > iOpen(Symbol(),PERIOD_M15,0) && iLow(Symbol(),PERIOD_M15,1) < u_max){
        tp = 2*range*Point;
        order_send(OP_BUY,Ask,lot(),Ask+tp,"mv13_buy",0,Green);
      }
    }
  }else if(type == OP_SELL){
    if(l_min != 0 && Close[0]+distance*Point < l_min && pth){
      if(iClose(Symbol(),PERIOD_M15,0)+distance*Point < iOpen(Symbol(),PERIOD_M15,0) && iHigh(Symbol(),PERIOD_M15,1) > l_min){
        tp = 2*range*Point;
        order_send(OP_SELL,Bid,lot(),Bid-tp,"mv13_sell",1,Red);
      }
    }
    if(u_min != 0 && Close[0]+distance*Point < u_min){
      if(iClose(Symbol(),PERIOD_M15,0)+distance*Point < iOpen(Symbol(),PERIOD_M15,0) && iHigh(Symbol(),PERIOD_M15,1) > u_min){
        tp = ((u_min-l_max)/3)*2;
        if(tp < range*Point && pth){tp = range*Point;}
        order_send(OP_SELL,Bid,lot(),Bid-tp,"mv13_sell",3,Red);
      }
    }
  }
}

double lot(){return(NormalizeDouble(AccountEquity()/risk/10,3));}

int order_send(int type, double price, double lot, double tp, string comment, int ty, color couleur){
  int ti = OrderSend(Symbol(),type,lot,price,3,0,0,comment,1234,0,couleur);
  if(ti > 0 && pth){
    tprofit = tp;
  }
}

bool trail(int ti, int type, double oop, double sl, int tr, double opprice, int ma){
  if(ma == AccountNumber() && Symbol() == "EURUSD"){return (true);}
  if(type == OP_BUY){
    if(Close[0]-tr*Point > sl && Close[0]-tr*Point > oop && opprice > 1){
      if(MathFloor((Close[0]-sl)/Point-tr) >= 10){
        OrderModify(OrderTicket(),OrderOpenPrice(),Close[0]-tr*Point,OrderTakeProfit(),0,Green);
      }
    }
  }else if(type == OP_SELL){
    if(Close[0]+tr*Point < sl && Close[0]+tr*Point < oop && opprice > 1){
      if(MathFloor((sl-Close[0])/Point-tr) >= 10){
        OrderModify(OrderTicket(),OrderOpenPrice(),Close[0]+tr*Point,OrderTakeProfit(),0,Red);
      }
    }
  }
}

void comments(string name, int x, int y, int coin, string texto, color couleur){
  if(ObjectFind(name) == -1){ObjectCreate(name,OBJ_LABEL,0,0,0);}
  ObjectSet(name,OBJPROP_XDISTANCE,x);
  ObjectSet(name,OBJPROP_YDISTANCE,y);
  ObjectSet(name,OBJPROP_CORNER,coin);
  ObjectSetText(name,texto,10,"Times New Roman",couleur);
}