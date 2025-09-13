//+------------------------------------------------------------------+
//|                                               ea_symr_newBar.mq4 |
//|                                                     Version: 1.1 |
//+------------------------------------------------------------------+

#property copyright "Copyright © 2011, Symr"
#property link      ""

#define MAX 9

//+------------------------------------------------------------------+

int curIndex;
datetime times[MAX];

//+------------------------------------------------------------------+

int start;

int init () {

  curIndex = utils.periodToPeriodIndex(Period());
  times[curIndex] = Time[0];
  for(int i=curIndex+1; i<MAX; i++)
    times[i] = times[curIndex]- MathMod(times[curIndex],utils.periodIndexToPeriod(i)*60);

  return(0);
}

//+------------------------------------------------------------------+

int deinit() {
  return (0);
}

//+------------------------------------------------------------------+

int start() {

  if (times[curIndex] != Time[0]) {
    times[curIndex] = Time[0];
    onBar(Period());
    for(int i=curIndex+1; i<MAX; i++) {
      int period  = utils.periodIndexToPeriod(i),
          seconds = period*60,
          time0   = times[curIndex] - MathMod(times[curIndex],seconds);
      if (times[i] != time0) {
        times[i] = time0;
        onBar(period);
      }
    }
  }

  onTick();

  return(0);
}

int utils.periodToPeriodIndex(int period) {
  switch(period) {
    case PERIOD_M1  : return(0); break;
    case PERIOD_M5  : return(1); break;
    case PERIOD_M15 : return(2); break;
    case PERIOD_M30 : return(3); break;
    case PERIOD_H1  : return(4); break;
    case PERIOD_H4  : return(5); break;
    case PERIOD_D1  : return(6); break;
    case 20         : return(7); break;
    case 55         : return(8); break;
  }
}

int utils.periodIndexToPeriod(int index) {
  switch(index) {
    case 0: return(PERIOD_M1); break;
    case 1: return(PERIOD_M5); break;
    case 2: return(PERIOD_M15); break;
    case 3: return(PERIOD_M30); break;
    case 4: return(PERIOD_H1); break;
    case 5: return(PERIOD_H4); break;
    case 6: return(PERIOD_D1); break;
    case 7: return(20); break;
    case 8: return(55); break;
  }
}

//+------------------------------------------------------------------+
// Code
//+------------------------------------------------------------------+

void onTick() {
  
}

//+------------------------------------------------------------------+

void onBar(int period) {

}

//+------------------------------------------------------------------+