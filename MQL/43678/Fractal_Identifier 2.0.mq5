//+------------------------------------------------------------------+
//|                                        Fractal_Identifier2.0.mq5 |
//|                                              Chioma J. Obunadike |
//|                                     https://wa.me/+2349124641304 |
//+------------------------------------------------------------------+
#property copyright "Chioma J. Obunadike"
#property link      "https://wa.me/+2349124641304"
#property version   "1.00"
#property indicator_chart_window

//+------------------------------------------------------------------+
int fractals;
int bars= 10;
int OnInit()
  {

fractals = iFractals(_Symbol, _Period);

 return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
void OnTick()
 { 
  // Loop through each element in the buffer
    for(int i=bars;i>=0;i--)
    {
    double fracUp[];
    ArraySetAsSeries(fracUp,true);
    CopyBuffer(fractals,UPPER_LINE,0,11,fracUp);
    
    if (fracUp[i] == EMPTY_VALUE) //check if the bar has a fractal and skip if it doesn't
    {continue;}
    
    else if (fracUp[i] != EMPTY_VALUE)  //confirm that the bar doesn't have an empty value'
    {
    double current_fractal_high = NormalizeDouble(fracUp[i],5);
    Comment ("Most recent Fractal: ", current_fractal_high );
      }    
      }
        
    }



