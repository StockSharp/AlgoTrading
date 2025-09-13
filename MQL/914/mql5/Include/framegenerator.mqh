//+------------------------------------------------------------------+
//|                                               FrameGenerator.mqh |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

//--- Add classes to draw the chart line show the values
#include <SpecialChart.mqh>
#include <SimpleTable.mqh>
#include <ColorProgressBar.mqh>
#include <Controls\Button.mqh>
//+------------------------------------------------------------------+
//|  A class to output optimization results                          |
//+------------------------------------------------------------------+
class CFrameGenerator
  {
private:
   int               m_trades;
   int               m_deals;
   double            m_profit;
   double            m_lots;
   double            m_ddawn_money;
   double            m_ddown_percent;
   double            m_profitfactor;
   double            m_recoveryfactor;
   CSpecialChart     m_spec_chart;
   CSimpleTable      m_statistics;
   CSimpleTable      m_inputs;
   CColorProgressBar m_cpb;
   CButton           m_replaybutton;
   bool              m_completed;
public:
   //--- Constructor/destructor
                     CFrameGenerator();
                    ~CFrameGenerator();
   //--- Events of the strategy tester
   void              OnTester(const double OnTesterValue);
   void              OnTesterInit(int lines);
   void              OnTesterPass(void);
   void              OnTesterDeinit(void);
   //--- Chart events
   void              OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam,const int delay_ms);
   //--- Going through frames
   void              ReplayFrames(const int delay_ms);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CFrameGenerator::CFrameGenerator()
  {
   m_completed=false;
   Print(__FUNCTION__);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CFrameGenerator::~CFrameGenerator()
  {
  }
//+------------------------------------------------------------------+
//|  Should be called in the OnTesterInit() handler                  | 
//+------------------------------------------------------------------+
void CFrameGenerator::OnTesterInit(int lines)
  {
   Comment("Waiting for initialization of optimization...");
//--- Prepare our special chart to show the balance lines
   if(!m_spec_chart.CreateBitmapLabel("Chart",200,20,500,260,COLOR_FORMAT_XRGB_NOALPHA))
      Print("Метод Create() вернул false");
//--- Set the frame color and width
   m_spec_chart.SetBackground(clrGray);
   m_spec_chart.SetFrameColor(clrIvory);
   m_spec_chart.SetFrameWidth(1);
//--- The number of series on the chart
   m_spec_chart.SetLines(lines);
//--- обновимся
   m_spec_chart.Update();
   Comment("The chart has been prepared");
//--- Add a progress bar
   m_cpb.Create("color passes",200,260,500,20,COLOR_FORMAT_XRGB_NOALPHA);
   m_cpb.BackColor(clrIvory);
   m_cpb.BorderColor(clrGray);
   m_cpb.BorderWidth(1);
   m_cpb.Update();
//--- Show the names of the values
   m_statistics.Create("statistics",0,20,135,65,20);
   m_statistics.BackColor(clrWhite);
   m_statistics.BorderColor(clrGray);
   m_statistics.TextColor(clrBlack);
   m_statistics.AddRow("Net Profit"," - ");
   m_statistics.AddRow("Profit Factor"," - ");
   m_statistics.AddRow("Factor Recovery"," - ");
   m_statistics.AddRow("Trades"," - ");
   m_statistics.AddRow("Deals "," - ");
   m_statistics.AddRow("Equity DD"," - ");
   m_statistics.AddRow("OnTester()"," - ");
//--- Prepare a table of input parameters
   m_inputs.Create("statistics",0,158,135,65,20);
   m_inputs.BackColor(clrMintCream);
   m_inputs.BorderColor(clrGray);
   m_inputs.TextColor(clrBlack);
//---
   m_replaybutton.Create(0,"repaly button",0,200,20,0,0);
   m_replaybutton.Width(500);
   m_replaybutton.Height(20);
   m_replaybutton.Text("Preparation");
//--- Update the current symbol chart
   ChartRedraw();
   Print(__FUNCTION__);
  }
//+------------------------------------------------------------------+
//|  Should be called in the OnTesterDeinit() handler                | 
//+------------------------------------------------------------------+
void CFrameGenerator::OnTesterDeinit(void)
  {
   m_completed=true;
   Comment("Optimization completed");
//--- Change the text and color of the header 
   m_replaybutton.ColorBackground(clrLightGreen);
   m_replaybutton.Text("Optimization completed: Click to replay");
  }
//+------------------------------------------------------------------+
//|  Prepares an array of the balance values and sends it in a frame |
//|  The function should be called in the EA                         |
//|  in the OnTester() handler                                       |
//+------------------------------------------------------------------+
void CFrameGenerator::OnTester(const double OnTesterValue)
  {
//---
   double   balance[];
   int      data_count=0;
   double   balance_current=1000;
//--- Temporary variables for working with deals
   ulong    ticket=0;
   double   profit;
   string   symbol;
   long     entry;
//--- Request the entire trading history
   HistorySelect(0,TimeCurrent());
   uint deals_total=HistoryDealsTotal();
//--- Collect data on deals
   for(uint i=0;i<deals_total;i++)
     {
      if((ticket=HistoryDealGetTicket(i))>0)
        {
         symbol=HistoryDealGetString(ticket,DEAL_SYMBOL);
         entry =HistoryDealGetInteger(ticket,DEAL_ENTRY);
         profit=HistoryDealGetDouble(ticket,DEAL_PROFIT);
         if(entry==DEAL_ENTRY_OUT || entry==DEAL_ENTRY_INOUT)
           {
            balance_current+=profit;
            data_count++;
            ArrayResize(balance,data_count);
            balance[data_count-1]=balance_current;
           }
        }
     }
//--- The data[] array for sending data to a frame
   double data[];
   ArrayResize(data,ArraySize(balance)+7);
   ArrayCopy(data,balance,7,0);
//--- Fill in the first 7 values ??of the array with the testing results
   data[0]=TesterStatistics(STAT_PROFIT);                // Net profit
   data[1]=TesterStatistics(STAT_PROFIT_FACTOR);         // Profit Factor
   data[2]=TesterStatistics(STAT_RECOVERY_FACTOR);       // Recovery Factor
   data[3]=TesterStatistics(STAT_TRADES);                // Number of trades
   data[4]=TesterStatistics(STAT_DEALS);                 // Number of deals
   data[5]=TesterStatistics(STAT_EQUITY_DDREL_PERCENT);  // The maximum drawdown as a percentage
   data[6]=OnTesterValue;                                // The value of the custom optimization criterion
//--- Output to a log to check in the testing mode (not optimization)
   PrintFormat("STAT_PROFIT=%.2f",data[0]);
   PrintFormat("STAT_PROFIT_FACTOR=%.2f",data[1]);
   PrintFormat("STAT_RECOVERY_FACTOR=%.2f",data[2]);
   PrintFormat("STAT_TRADES=%d",(int)data[3]);
   PrintFormat("STAT_DEALS=%d",(int)data[4]);
   PrintFormat("STAT_EQUITY_DDREL_PERCENT=%.2f%%",data[5]);
   PrintFormat("STAT_CUSTOM_ONTESTER=%G",data[6]);
//--- Create a data frame and send it to the terminal
   if(!FrameAdd(MQL5InfoString(MQL5_PROGRAM_NAME),1,deals_total,data))
      Print("Frame add error: ",GetLastError());
   else
      Print("Frame added, Ok");
//---
  }
//+------------------------------------------------------------------+
//|  Receives a frame with data during optimization                  |
//|  and displays a chart                                            |
//+------------------------------------------------------------------+
void CFrameGenerator::OnTesterPass(void)
  {
//--- Variables for working with frames
   string  name;
   ulong   pass;
   long    id;
   double  value,data[];
   string params[];
   uint par_count;
//--- Auxiliary variables
   static datetime start=0;
   static int      cnt=0;
//---
   if(start==0) start=TimeLocal();
//--- When receiving a new frame, try to get data from it
   while(FrameNext(pass,name,id,value,data))
     {
      //--- Prepare a comment line
      string comm="OnTesterPass("+name+"), pass="+(string)pass+" id="+(string)id+
                  "  Deals="+StringFormat("%d",(int)value)+"\r\n";
      Comment(comm);
      //--- If the parameter table is still empty
      if(m_inputs.Rows()==0)
        {
         //--- Get the input parameters of the Expert Advisor, for which the frame is formed
         if(FrameInputs(pass,params,par_count))
           {
            //--- Go through the parameters, params[i] looks like "parameter=value"
            for(uint i=0;i<par_count;i++)
              {
               //--- Fill in the table of input parameters
               string array[];
               //--- Split into two lines and add a row to the table
               if(StringSplit(params[i],'=',array)==2) m_inputs.AddRow(array[0],array[1]);
              }
           }
        }
      else //--- The table is already crated, we only need to update its parameters    
        {
         //--- Get the input parameters of the Expert Advisor, for which the frame is formed
         if(FrameInputs(pass,params,par_count))
           {
            //--- Go through the parameters, params[i] looks like "parameter=value"
            for(uint i=0;i<par_count;i++)
              {
               //--- Build a comment line
               StringAdd(comm,params[i]+"\r\n");
               //--- Fill in the table of input parameters
               string array[];
               //--- Split into two lines and update the second cell in the row
               if(StringSplit(params[i],'=',array)==2) m_inputs.SetValue(1,i,array[1]);
              }
           }

        }
      //--- Update the table of statistics
      m_statistics.SetValue(1,0,StringFormat("%.2f",data[0]));
      m_statistics.SetValue(1,1,StringFormat("%.2f",data[1]));
      m_statistics.SetValue(1,2,StringFormat("%.2f",data[2]));
      m_statistics.SetValue(1,3,StringFormat("%G",data[3]));
      m_statistics.SetValue(1,4,StringFormat("%G",data[4]));
      m_statistics.SetValue(1,5,StringFormat("%.2f%%",data[5]));
      m_statistics.SetValue(1,6,StringFormat("%G",data[6]));

      //--- An array for receiving balance values of the current frame
      double seria[];
      ArrayCopy(seria,data,0,7,ArraySize(data)-7);
      //--- Send the array to be displayed on a special balance graph
      m_spec_chart.AddSeria(seria,data[0]>0);
      //--- update the balance lines on the chart 
      m_spec_chart.Update();
      //--- Update the progress bar
      m_cpb.AddResult(data[0]>0);
      //--- Increase the counter of processed frames
      cnt++;
      //--- Update the text in the chart header
      m_replaybutton.Text(StringFormat("Frames processed (tester passes): %d for %s",
                          cnt,TimeToString(TimeLocal()-start,TIME_MINUTES|TIME_SECONDS)));
     }
//---
  }
//+------------------------------------------------------------------+
//|  Handling events on the chart                                    |
//+------------------------------------------------------------------+
void CFrameGenerator::OnChartEvent(const int id,const long &lparam,
                                   const double &dparam,const string &sparam,
                                   const int delay_ms)
  {
   if(!m_completed) return;
//--- If this is an event of a mouse click on a graphical object
   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      //--- If the event of our object is received
      if(sparam==m_replaybutton.Name())
        {
         //--- Unpress the button
         ObjectSetInteger(0,sparam,OBJPROP_STATE,false);
         //--- Change the header color
         m_replaybutton.ColorBackground(clrKhaki);
         //--- Start playing
         m_completed=false; // To avoid starting several times in a row
         ReplayFrames(100); // The procedure of playing
         m_completed=true; // Unblock   
         //--- Restore the color and the text of the header
         m_replaybutton.ColorBackground(clrLightGreen);
         m_replaybutton.Text("Optimization completed: Click to replay");
        }
     }
  }
//+------------------------------------------------------------------+
//| Replaying frames after the end of optimization                   |
//+------------------------------------------------------------------+
void CFrameGenerator::ReplayFrames(const int delay_ms)
  {
//--- Variables for working with frames
   string  name;
   ulong   pass;
   long    id;
   double  value,data[];
   string params[];
   uint par_count;
//--- Counter of frames
   int      frame_counter=0;
//--- Reset the progress bar counters
   m_cpb.Reset();
   m_cpb.Update();  
//--- Move the frame pointer to the beginning
   FrameFirst();
//--- Start going through frames
   while(FrameNext(pass,name,id,value,data))
     {
      //--- Get the input parameters of the Expert Advisor, for which the frame is formed
      if(FrameInputs(pass,params,par_count))
        {
         //--- Go through the parameters, params[i] looks like "parameter=value"
         for(uint i=0;i<par_count;i++)
           {
            //--- Fill in the table of input parameters
            string array[];
            //--- Split into two lines and update the second cell in the row
            if(StringSplit(params[i],'=',array)==2) m_inputs.SetValue(1,i,array[1]);
           }
        }
      //--- Update the table of statistics
      m_statistics.SetValue(1,0,StringFormat("%.2f",data[0]));
      m_statistics.SetValue(1,1,StringFormat("%.2f",data[1]));
      m_statistics.SetValue(1,2,StringFormat("%.2f",data[2]));
      m_statistics.SetValue(1,3,StringFormat("%G",data[3]));
      m_statistics.SetValue(1,4,StringFormat("%G",data[4]));
      m_statistics.SetValue(1,5,StringFormat("%.2f%%",data[5]));
      m_statistics.SetValue(1,6,StringFormat("%G",data[6]));
      //--- An array for receiving balance values of the current frame
      double seria[];
      ArrayCopy(seria,data,0,7,ArraySize(data)-7);
      //--- Send the array to be displayed on a special balance graph
      m_spec_chart.AddSeria(seria,data[0]>0);
      //--- update the balance lines on the chart 
      m_spec_chart.Update();
      //--- Update the progress bar
      m_cpb.AddResult(data[0]>0);
      //--- Increase the counter of processed frames
      frame_counter++;
      //--- Update the text in the chart header
      m_replaybutton.Text(StringFormat("Playing with a pause of %d ms: frame %d",
                          delay_ms,frame_counter));
      Sleep(delay_ms);
     }
  }

//+------------------------------------------------------------------+
