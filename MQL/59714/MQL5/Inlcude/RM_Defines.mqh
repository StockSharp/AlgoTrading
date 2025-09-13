//+------------------------------------------------------------------+
//|                                                   RM_Defines.mqh |
//|                                     Niquel y Leo, Copyright 2025 |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Niquel y Leo, Copyright 2025"
#property link      "https://www.mql5.com"
#property strict

//+------------------------------------------------------------------+
//|                        Defines                                   |
//+------------------------------------------------------------------+
#define NOT_MAGIC_NUMBER 2//Not Magic Number 
#define FLAG_CLOSE_ALL_PROFIT 2 //Flag indicating to close only operations with profit
#define FLAG_CLOSE_ALL_LOSS   4 //Flag indicating to close only operations without profit
#define EA_NAME "CRiksManagement | " //Prefix

//--- positions
#define  FLAG_POSITION_BUY 2
#define  FLAG_POSITION_SELL 4

//--- orders
#define FLAG_ORDER_TYPE_BUY             1
#define FLAG_ORDER_TYPE_SELL            2
#define FLAG_ORDER_TYPE_BUY_LIMIT       4
#define FLAG_ORDER_TYPE_SELL_LIMIT      8
#define FLAG_ORDER_TYPE_BUY_STOP        16
#define FLAG_ORDER_TYPE_SELL_STOP       32
#define FLAG_ORDER_TYPE_BUY_STOP_LIMIT  64
#define FLAG_ORDER_TYPE_SELL_STOP_LIMIT 128
#define FLAG_ORDER_TYPE_CLOSE_BY        256

//--- Losses Profits
#define LOSS_PROFIT_COUNT 8

//--- Tickets
#define INVALID_TICKET 0
//+------------------------------------------------------------------+
//|                      Enumerations                                |
//+------------------------------------------------------------------+
enum ENUM_LOTE_TYPE //lot type
 {
  Dinamico,//Dynamic
  Fijo//Fixed
 };

//--- Enumeration to define the types of calculation of the value of maximum profits and losses
enum ENUM_RISK_CALCULATION_MODE
 {
  money, //Money
  percentage //Percentage %
 };

//--- Enumeration to define the type of risk management
enum ENUM_MODE_RISK_MANAGEMENT
 {
  risk_mode_propfirm_dynamic_daiy_loss, //Prop Firm (FTMO-FundendNext)
  risk_mode_personal_account // Personal Account
 };

//--- Enumeration to define the value to which the percentages will be applied
enum ENUM_APPLIED_PERCENTAGES
 {
  Balance, //Balance
  ganancianeta,//Net profit
  free_margin, //Free margin
  equity //Equity
 };

//--- Enumeration for ways to obtain the lot
enum ENUM_GET_LOT
 {
  GET_LOT_BY_ONLY_RISK_PER_OPERATION, //Obtain the lot for the risk per operation
  GET_LOT_BY_STOPLOSS_AND_RISK_PER_OPERATION //Obtain and adjust the lot through the risk per operation and stop loss respectively.
 };

//--- Mode to check if a maximum loss or gain has been exceeded
enum MODE_SUPERATE
 {
  EQUITY, //Only Equity
  CLOSE_POSITION, //Only for closed positions
  CLOSE_POSITION_AND_EQUITY//Closed positions and equity
 };

//--- Enumeration of the types of dynamic operational risk
enum ENUM_OF_DYNAMIC_MODES_OF_GMLPO
 {
  DYNAMIC_GMLPO_FULL_CUSTOM, //Customisable dynamic risk per operation
  DYNAMIC_GMLPO_FIXED_PARAMETERS,//Risk per operation with fixed parameters
  NO_DYNAMIC_GMLPO //No dynamic risk for risk per operation
 };

//--- Enumeration to determine when to review a decrease in the initial balance to modify the risk per operation
enum ENUM_REVISION_TYPE
 {
  REVISION_ON_CLOSE_POSITION, //Check GMLPO only when closing positions
  REVISION_ON_TICK //Check GMLPO on all ticks
 };

enum ENUM_LOSS_PROFIT
 {
  T_LOSS,
  T_PROFIT,
  T_GMLPO
 };

enum ENUM_TYPE_LOSS_PROFIT
 {
  LP_MDP = 0, //Maxima ganancias diaria
  LP_MWP = 1,//Maxima ganancia semanal
  LP_MMP = 2,//Maxima ganancia mensual

  LP_MDL = 3, //Maxima perdida diaria
  LP_MWL = 4, //Maxima perdida semanal
  LP_MML = 5, //Maxima perdida mensual
  LP_ML = 6, //Maxima perdida total
  LP_GMLPO = 7 //Maxima perdida por operacion
 };
//+------------------------------------------------------------------+
//|                      Structures                                  |
//+------------------------------------------------------------------+
//--- Positions
struct Position
 {
  ulong              ticket; //position ticket
  ENUM_POSITION_TYPE type; //position type
  double             profit;
 };

//--- Loss/Profit
struct Loss_Profit
 {
  double             value; //value
  double             assigned_percentage; //percentage to apply
  ENUM_RISK_CALCULATION_MODE mode_calculation_risk; //risk calculation method
  ENUM_APPLIED_PERCENTAGES percentage_applied_to; //percentage applied to
 };

//--- Dynamic gmlpo/ Riesgo por operacion dinamico
struct Dynamic_LossProfit
 {
  double             balance_to_activate_the_risk[];
  double             risk_to_be_adjusted[];
 };

struct RiskParams
 {
  ENUM_MODE_RISK_MANAGEMENT mode;
  MqlParam           params[];
 };

struct ModfierInitInfo
 {
  double             balance;
  ulong              magic;
 };

struct ModifierOnOpenCloseStruct
 {
  ulong              last_deal_ticket;
  ulong              last_position_ticket;
  ENUM_DEAL_ENTRY    deal_entry_type;
  double             daily_profit;
  double             weekly_profit;
  double             monthly_profit;
  double             gross_profit;
  double             last_deal_profit;
  double             last_position_profit;
 };
//+------------------------------------------------------------------+
