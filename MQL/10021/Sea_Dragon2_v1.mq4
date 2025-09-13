//+------------------------------------------------------------------+
//|                                                   º£ÉÏÁú2 v1.mq4 |
//|                                                             ÁõËÉ |
//|                                                    qq£º569638390 |
//+------------------------------------------------------------------+
#property copyright "ÁõËÉ"
#property link      "qq£º569638390"

extern double lot=0.1;
extern double     sl       = 10; 
extern double     maxsl       = 150; 
extern double     tp       =10;
extern double     tp_2       =2;

extern double     weishu       =1;
  
int slippage=2;
int Magic=12332344;


double g_point;
int init() {
   if (Point == 0.00001) g_point = 0.0001;
   else {
      if (Point == 0.001) g_point = 0.01;
      else g_point = Point;
   }
   return (0);
}

int start(){  

ckdingdan();
if(jiange()){
if (buyorsell()==OP_BUY)buy();
if (buyorsell()==OP_SELL)sell();
} // jiange end
ckmd();

return(0);
}


void ckmd(){
if(OrdersTotal()>0){
   int b,s;
   double price,price_b,price_s,lot,SLb,SLs,lot_s,lot_b;
   for (int j=0; j<OrdersTotal(); j++)
   {  if (OrderSelect(j,SELECT_BY_POS,MODE_TRADES)==true)
      {  if ((Magic==OrderMagicNumber() ) && OrderSymbol()==Symbol())
         {
            price = OrderOpenPrice();
            lot   = OrderLots();
            if (OrderType()==OP_BUY ) {price_b += price*lot; lot_b+=lot; b++;}                     
            if (OrderType()==OP_SELL) {price_s += price*lot; lot_s+=lot; s++;}
         }  
      }  
   }
  if(lot_b!=0)  SLb = price_b/lot_b;
  if(lot_s!=0)  SLs = price_s/lot_s;
    for(int m=0;m < OrdersTotal();m++){ 
    if(OrderSelect(m,SELECT_BY_POS,MODE_TRADES)==false)     break;
    if ((OrderMagicNumber() == Magic)&&OrderSymbol()==Symbol()){
    if(b==s){
     if(OrderType()==OP_BUY&&OrderTakeProfit()!=OrderOpenPrice()+tp*g_point)OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-maxsl*g_point,OrderOpenPrice()+tp*g_point,0,Gold);
     if(OrderType()==OP_SELL&&OrderTakeProfit()!=OrderOpenPrice()-tp*g_point)OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+maxsl*g_point,OrderOpenPrice()-tp*g_point,0,Gold);
     }//buyorsell end
 
    if(b>s){
     if(OrderType()==OP_BUY&&OrderTakeProfit()!=SLb+Ask-Bid+tp_2*g_point)OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-maxsl*g_point,SLb+Ask-Bid+tp_2*g_point,0,Aqua);
     if(OrderType()==OP_SELL&&OrderTakeProfit()!=OrderOpenPrice()-tp*g_point)OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+maxsl*g_point,OrderOpenPrice()-tp*g_point,0,Aqua);
     }//buyorsell end
 
    if(b<s){
     if(OrderType()==OP_BUY&&OrderTakeProfit()!=OrderOpenPrice()+tp*g_point)OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-maxsl*g_point,OrderOpenPrice()+tp*g_point,0,LightYellow);
     if(OrderType()==OP_SELL&&OrderTakeProfit()!=SLs-Ask+Bid-tp_2*g_point)OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+maxsl*g_point,SLs-Ask+Bid-tp_2*g_point,0,LightYellow);
     }//buyorsell end
 
    }
   }//for end
 
 
 
  }//total end
}


void buy(){
OrderSend(Symbol(),OP_SELL,lot,Bid,slippage,0,0,"BUY",Magic,0,C'11,4,125');Sleep(500);
OrderSend(Symbol(),OP_BUY,ll(OP_BUY),Ask,slippage,0,0,"sell",Magic,0,C'128,2,5');Sleep(500);
return(0);
}

void sell(){
OrderSend(Symbol(),OP_BUY,lot,Ask,slippage,0,0,"BUY",Magic,0,C'128,2,5');Sleep(500);
OrderSend(Symbol(),OP_SELL,ll(OP_SELL),Bid,slippage,0,0,"sell",Magic,0,C'11,4,125');Sleep(500);
return(0);
}

/*
double ll(int dan){
double lo;
double lt=-1;
if(OrdersTotal()>0){
    for(int m=0;m < OrdersTotal();m++){ 
    if(OrderSelect(m,SELECT_BY_POS,MODE_TRADES)==false)     break;
    if ((OrderMagicNumber() == Magic)&&OrderSymbol()==Symbol()&&OrderType()==dan){
   lt=MathMax(lt,OrderLots());
    }
  }//  maxtimeend
 } //total end   
 if(lt<lot*3) lo=lt*2;else lo=NormalizeDouble(lt*1.5,weishu);
return(lo);
}
*/


double ll(int dan){
int lo[15];
lo[0]=1;
lo[1]=1;
lo[2]=2;
lo[3]=3;
lo[4]=6;
lo[5]=9;
lo[6]=14;
lo[7]=22;
lo[8]=33;
lo[9]=48;
lo[10]=82;
lo[11]=111;
lo[12]=122;
lo[13]=164;
lo[14]=185;

int lt=0;
if(OrdersTotal()>0){
    for(int m=0;m < OrdersTotal();m++){ 
    if(OrderSelect(m,SELECT_BY_POS,MODE_TRADES)==false)     break;
    if ((OrderMagicNumber() == Magic)&&OrderSymbol()==Symbol()&&OrderType()==dan){
   lt++;
    }
  }//  maxtimeend
 } //total end   

double loo=lo[lt+1];
if(weishu==1)loo=loo/10;
if(weishu==2)loo=loo/100;

return(loo);
}



int buyorsell(){
int buy=0,sell=0;
if(OrdersTotal()>0){
for(int m=0;m < OrdersTotal();m++){ 
    if(OrderSelect(m,SELECT_BY_POS,MODE_TRADES)==false)     break;
    if ((OrderMagicNumber() == Magic)&&OrderSymbol()==Symbol()){
    if(OrderType()==OP_BUY) buy++;
    if(OrderType()==OP_SELL) sell++;
    }
  }//  forend
 }// totals end
 
   Comment(buy,"****",sell);
if(buy==sell)return(5);
if(buy>sell) return(OP_BUY);
if(buy<sell) return(OP_SELL);
}

bool jiange(){
double time=-1;
double open;
if(OrdersTotal()>0){
    for(int m=0;m < OrdersTotal();m++){ 
    if(OrderSelect(m,SELECT_BY_POS,MODE_TRADES)==false)     break;
    if ((OrderMagicNumber() == Magic)&&OrderSymbol()==Symbol()){
    time=MathMax(time,OrderOpenTime());
    }
  }//  maxtimeend
    for(int i=0;i < OrdersTotal();i++){ 
    if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)     break;
    if ((OrderMagicNumber() == Magic)&&OrderSymbol()==Symbol()){
    if(time==OrderOpenTime())  open=OrderOpenPrice();
    }
   }   
if(MathAbs(open-((Ask+Bid)/2))/(sl*g_point)>=1&&open!=0)  return(true); else return(false);
 } //total for end
return(false);
}// jiage end



void ckdingdan(){
if (OrdersTotal()==0){
ckopen();
}else{
 for(int m=0;m < OrdersTotal();m++){ 
         if(OrderSelect(m,SELECT_BY_POS,MODE_TRADES)==false)     break;
    if ((OrderMagicNumber() == Magic&&OrderSymbol()==Symbol())){  
    break;
    ckopen();
    }
   }// for end
}//else end
return;
}

void ckopen(){
OrderSend(Symbol(),OP_BUY,lot,Ask,slippage,0,0,"BUY",Magic,0,Red);Sleep(500);
OrderSend(Symbol(),OP_SELL,lot,Bid,slippage,0,0,"BUY",Magic,0,Green);Sleep(500);
return(0);
}