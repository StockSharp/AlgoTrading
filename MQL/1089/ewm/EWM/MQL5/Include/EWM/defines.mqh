//+------------------------------------------------------------------+
//|                                                      defines.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Arrays\ArrayDouble.mqh>

// ewm
#define PREV_LEVEL             'Q'      // show the previous wave level on the labels panel
#define NEXT_LEVEL             'W'      // show the next wave level on the labels panel
#define DOWN_LEVEL             'A'      // decrease the wave labels' level
#define UP_LEVEL               'S'      // increase the wave labels' level
#define START_MARKING          'Z'      // start marking waves
#define STOP_MARKING           'X'      // stop marking waves
#define SELECT_GROUP           0x09     // select/deselect groups of wave labels (press Tab key)
#define HIDE_PANEL             0x1B     // hide/show the labels panel (press Esc key)
#define INCREASE_LEVEL         'E'      // reduce the number of wave levels displayed in the chart
#define REDUCE_LEVEL           'R'      // increase the number of wave levels displayed in the chart
#define DELETE_OBJECT          0x2E     // delete selected object (press Del key)
#define SELECT                 0x10     // start/stop the labels selection process (press Shift key)
#define SHIFT_X                3        // padding of the chart frame along the X-axis
#define SHIFT_Y                5        // padding of the chart frame along the Y-axis
#define UP                     true     // increases the wave level
#define DOWN                   false    // decreases the wave level
#define LEVEL                  0        // the wave label level
#define TEXT                   1        //text of the label
#define GROUP                  2        // the group of the wave label
#define UNIQUE_NAME            3        // the unique name of the wave label on which it is identified
#define LABELS                 15       // the number of one wave level labels
#define DELAY                  100      // delay of the cycle in milliseconds
#define TOP                    true     // the wave label located above the bar
#define BOTTOM                 false    // the wave label located below the bar
#define EVERY_BAR              1        // the possibility to set the wave label above/below each bar 
#define NOT_EVERY_BAR          0        //  the possibility to set the wave label above/below each 3-bar fractal
#define SEPARATOR              "_"      // separator
#define NAME_LABEL             "$L$"    // the name of the label in the labels panel
#define NAME_WAVE              "$W$"    // the name of the wave label

// ewa
#define START_ANALYSIS_LEFT    '1'      // analyze the whole chart/analyze from the left of the selected wave
#define START_ANALYSIS_RIGHT   '2'      // analyze the whole chart/analyze from the right of the selected wave
#define PREV_VARIANT_LEFT      '3'      // show the previous variants of the selected wave labeling
#define NEXT_VARIANT_LEFT      '4'      // show the following variants of the selected wave labeling
#define PREV_VARIANT_RIGHT     '5'      // show the previous variants of the wave labeling located to the right from the selected wave
#define NEXT_VARIANT_RIGHT     '6'      // show the following variants of the wave labeling located to the right from the selected wave
#define CONVERT                'V'      // convert automatic labeling manually
#define CLEAR                  'C'      // clear the chart
#define GET_NAME_WAVE_LEFT     '7'      // get the name of the selected wave
#define GET_NAME_WAVE_RIGHT    '8'      // get the name of the wave l;ocated to the right from the selected wave
#define NAME_AUTO_WAVE         "$A$"    // the unique name of the wave label created using auto labeling
#define MAX_POINTS             6        // the maximum number of points for identification
#define TREND_UP               "Up"     // trend up
#define TREND_DOWN             "Down"   // trend down
#define MORE                   ">="     // more or equal
#define LESS                   "<="     // less or equal
#define MIN                    "min"    // the minimum wave value
#define MAX                    "max"    // the maximum wave value
#define VAL                    "val"    // height value
#define LENGTH                 "length" // length ratio
#define TIME                   "time"   // time ratio
#define EOF                    26       // identifier of file end
#define LEFT                   true     // 
#define RIGHT                  false    // 
#define TYPE1                  1        // type of analysis - completed waves analysis
#define TYPE2                  2        // the type of analysis - the unbegun waves analysis
#define TYPE3                  3        // the type of analysis  - the unfinished waves analysis
#define TYPE4                  4        // the type of analysis - unbegun and unfinished waves analysis
#define MSG_BEGIN_ANALYSIS     "Start an analysis?"
#define MSG_SELECTED_MORE      "An analysis is impossible as more than one wave is selected!"
#define MSG_ANALYSIS_COMPLETED "An analysis is complete!"
#define MSG_REMOVE             "All labels will be deleted!"
#define MSG_CLEAR              "Delete all labels from the chart?"
#define NAME_RULES_FILE        "EWM.txt"
#define MSG_FILE_OPEN_ERROR    "The EWM.txt file open error!"

#define COPY_CHART             'J'      // create a copy of chart with all objects

// pitchfork
#define SHOW_SCHIFF            'U'
#define SHOW_WARNING_UP        'D'
#define SHOW_WARNING_DOWN      'F'
#define NAME_PITCHFORK         "$P$"
#define NAME_SCHIFF            "$S$"
#define NAME_REACTION          "$R$"
#define NAME_WARNING_UP        "$WU$"
#define NAME_WARNING_DOWN      "$WD$"
#define PITCHFORK_LEVEL1       0
#define PITCHFORK_TEXT1        1
#define PITCHFORK_GROUP1       2
#define PITCHFORK_LEVEL2       3
#define PITCHFORK_TEXT2        4
#define PITCHFORK_GROUP2       5
#define PITCHFORK_LEVEL3       6
#define PITCHFORK_TEXT3        7
#define PITCHFORK_GROUP3       8
#define PITCHFORK_NAME         9
#define PITCHFORK_VISIBLE      10
#define HIDE_PITCHFORK         'P'
#define NAME_PRICE_LABEL       "$PL$"
#define NAME_HORIZONTAL        "$H$"
#define NAME_VERTICAL          "$V$"
#define NAME_VERTICAL0         "$V0$"
#define SHOW_HORIZONTAL        'T'
#define HIDE_ALL_HORIZONTAL    'Y'
#define SHOW_PRICE_LABEL       'G'
#define HIDE_ALL_PRICE_LABEL   'H'
#define SHOW_VETICAL           'B'
#define HIDE_ALL_VERTICAL      'N'
#define SHOW_VERTICAL0         'M'

double schiff_levels1[]   = {-0.764, -1.000};
double schiff_levels2[]   = {-0.764, -1.000, 0.000};
double pitchfork_levels[] = {-0.764};

CArrayDouble warning_levels;
CArrayDouble reaction_levels;

string etalon[] = {"1", "2", "3", "4", "5", "A", "B", "C", "D", "E", "W", "X", "Y", "XX", "Z"};

MqlRates rates[];

// timeframws on which all objects are displayed
int tfs[] = {OBJ_PERIOD_M1, OBJ_PERIOD_M2, OBJ_PERIOD_M3, OBJ_PERIOD_M4, OBJ_PERIOD_M5, OBJ_PERIOD_M6, OBJ_PERIOD_M10, OBJ_PERIOD_M12, OBJ_PERIOD_M15, OBJ_PERIOD_M20, OBJ_PERIOD_M30, OBJ_PERIOD_H1, OBJ_PERIOD_H2, OBJ_PERIOD_H3, OBJ_PERIOD_H4, OBJ_PERIOD_H6, OBJ_PERIOD_H8, OBJ_PERIOD_H12, OBJ_PERIOD_D1, OBJ_PERIOD_W1, OBJ_PERIOD_MN1};

// chart periods
int periods[] = {PERIOD_M1, PERIOD_M2, PERIOD_M3, PERIOD_M4, PERIOD_M5, PERIOD_M6, PERIOD_M10, PERIOD_M12, PERIOD_M15, PERIOD_M20, PERIOD_M30, PERIOD_H1, PERIOD_H2, PERIOD_H3, PERIOD_H4, PERIOD_H6, PERIOD_H8, PERIOD_H12, PERIOD_D1, PERIOD_W1, PERIOD_MN1};

input int interval = 25;                // The distance between the labels on the labels panel
input int x_distance = 10;              // The position of the labels panel along the X-axis
input int y_distance = 10;              // The position of the labels panel along the Y-axis
input color red_zone_color = clrSalmon; // "Red zone" boundary color
input color verical0_color = LightGray; // Vertical line on the 0 bar color

struct Coord
{
  double price1;
  double price2;
  double price3;
  double price4;
  double pos1;
  double pos2;
  double pos3;
  double pos4;
};
