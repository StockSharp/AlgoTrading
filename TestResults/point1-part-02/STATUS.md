# Point 1 / Part 02 — evidence ledger

- Assignment: `TZ-02.md`, 316 **exact folder keys**; 2 specified changes, 223 suspected defects, 91 missing contract verifications.
- Repository: `StockSharp/AlgoTrading` only. Branch: `feature/tz-point1-part02-20261016`, forked from `main` at `2967cb54b37e337e332103901ea815fca46b5e6c`.
- Scope: no core StockSharp, other repositories, workspace configs, CI, or install scripts modified.
- **Status: INCOMPLETE.** A code change without a passing behavioral replay is **not** reported as verified or fixed.
- Test environment: GitHub source access is available, but no .NET SDK or StockSharp sibling checkout/history package is installed in the execution container. Accordingly **no MSTest or Python runtime archive replay has executed**. No red/green TDD outcome can be asserted.
- The referenced `AGENTS.md` is absent at the AlgoTrading remote root, and `configs/instructions.md` is not accessible in the repositories queried; we inspected `CLAUDE.md`, the test harness, and the publicly available sibling StockSharp `AGENTS.md`.
- All listed keys below remain in review until both languages are verified against an independent oracle and every required fill/exit/protection assertion passes.

## Change groups started

### 1186_Price_and_Volume_Breakout_Buy
- **Source changes prepared, verification outstanding.** Replaced EMA-cross template in both languages with previous-bar price/volume extrema + SMA + configurable direction + fifth opposite-close exit. Same defaults `60/60/200/Long/1 day`.
- An independent archive oracle was added to `Tests/StrategyTests.Point1Part02.cs` to exercise `Long`, `Short`, `Both` and BTC/TON with an explicit `3/3/5/5-minute` mechanics profile. It asserts unchanged defaults before override; this profile is **not** called a default test.
- All seven README languages describe preceding bars, direction options, and streak reset.
- Because 200 *daily* bars cannot warm up on the one-month packaged archive, the default auto-trade row was moved into `StrategyInventory.Overrides`, leaving trade requirements **enabled** in the custom test.
- **Not yet proved:** test is red before fix, green after fix, archive entries/exits and actual fills, compilation/interop in both languages.

### 1243_Renko
- **No production changes.** Added a proposed monthly `PriceSource=Ticks` vs `Level1` test in `StrategyTests.Point1Part02.cs`.
- It requires a complete-month replay; checks nondecreasing brick times, complete nonzero bricks, each direction, an independent oracle for reversal-order direction/volume and actual fills, and an elapsed-time ceiling/relative Ticks-to-Level1 cost.
- **Not yet proved:** tick mode timing, processing cost, actual completeness against the original tick stream, full-month performance, or tests passing in either language.

## Confirmed defects from targeted static source review

Both C# and Python were fetched and independently inspected for the following eight keys: `0772_Exodus`, `0773_Express_Generator`, `0774_External_Signals_Tester`, `0777_Faith_Indicator`, `0778_Falcon_Liquidity_Grab`, `0779_Fancy_Bollinger_Bands`, `0780_Fearzone_Panel`, `0781_Fibonacci_Bollinger_Bands`. Each currently contains identical generic EMA(120/450) crossing logic and only `FastEmaPeriod`, `SlowEmaPeriod`, `CandleType` parameters, not the algorithm in the README. These are **confirmed source-contract mismatches**, not behavioral test passes. Do not report them as fixed, and do not reuse the EMA template as the replacement. Each needs its own independent oracle, red/green trace, actual orders and executions and seven-language documentation check.

### 0779_Fancy_Bollinger_Bands

- Before change: confirmed both languages implemented generic EMA(120/450) crossover, with neither Bollinger bands nor stated entry/exit behavior.
- Added independent full-month BTC and TON order/fill oracle `S0779_BollingerBreakoutsAndBandReentryExitsMatchIndependentBands` first, then replaced the C#/Python strategy implementations with closed-candle SMA + population standard deviation Bollinger bands (default 20, ×2; default 1 minute). Long crosses above upper, short crosses below lower; exit when close crosses back inside the respective band, no native stops.
- Updated **all seven** localized README files with explicit prior/current-band semantics, the exact reverse crossover, and defaults.
- **Not yet proved:** failing pre-fix run, passing post-fix run, trade/order/fill coverage and compilation (SDK/archive unavailable in this environment). This is a source fix pending validation, **not completed**.

## Shared files touched (merge coordination)
- `Tests/StrategyInventory.cs`: only add `1186_Price_and_Volume_Breakout_Buy` to Overrides. No other keys changed.
- `Tests/Tests.csproj`: unchanged.
- `Tests/StrategyTests.Point1Part02.cs`: new file used by existing partial `StrategyTests`; no other part's test files changed.

## Filtered commands to run in a checkout with StockSharp sibling and monthly archive

Only one test worker may execute at a time; each command has a shell timeout and selected strategy filter:

```bash
timeout 900s dotnet test Tests/Tests.csproj -m:1 --filter 'FullyQualifiedName~S1186_' --blame-hang-timeout 00:01:00 -- RunConfiguration.MaxCpuCount=1
timeout 900s dotnet test Tests/Tests.csproj -m:1 --filter 'FullyQualifiedName~S1243_' --blame-hang-timeout 00:01:00 -- RunConfiguration.MaxCpuCount=1
timeout 900s dotnet test Tests/Tests.csproj -m:1 --filter 'FullyQualifiedName~S0779_' --blame-hang-timeout 00:01:00 -- RunConfiguration.MaxCpuCount=1
```

**Execution results:** not run (missing SDK, sibling core, and packaged history in current environment). C# / Python and README structure/translation changes therefore await actual validation.

## Per-key ledger

| Exact key | Work class | Current status and reason |
|---|---|---|
| `1186_Price_and_Volume_Breakout_Buy` | Specified change | Source changed; C#/Python archive TDD and default checks NOT RUN |
| `1243_Renko` | Specified change | Test proposed; full-month Tick/Level1 performance NOT RUN |
| `0772_Exodus` | Investigate note | STATIC DEFECT CONFIRMED — both languages use generic EMA(120/450) crossover unrelated to README; original behavior, independent tests, fills and protection NOT IMPLEMENTED/NOT RUN |
| `0773_Express_Generator` | Investigate note | STATIC DEFECT CONFIRMED — both languages use generic EMA(120/450) crossover unrelated to README; original behavior, independent tests, fills and protection NOT IMPLEMENTED/NOT RUN |
| `0774_External_Signals_Tester` | Investigate note | STATIC DEFECT CONFIRMED — both languages use generic EMA(120/450) crossover unrelated to README; original behavior, independent tests, fills and protection NOT IMPLEMENTED/NOT RUN |
| `0777_Faith_Indicator` | Investigate note | STATIC DEFECT CONFIRMED — both languages use generic EMA(120/450) crossover unrelated to README; original behavior, independent tests, fills and protection NOT IMPLEMENTED/NOT RUN |
| `0778_Falcon_Liquidity_Grab` | Investigate note | STATIC DEFECT CONFIRMED — both languages use generic EMA(120/450) crossover unrelated to README; original behavior, independent tests, fills and protection NOT IMPLEMENTED/NOT RUN |
| `0779_Fancy_Bollinger_Bands` | Investigate note | SOURCE FIX + ORACLE WRITTEN — replaced EMA template with BB breakout/reentry, C#/Python, synced seven README; red/green archive executions NOT RUN |
| `0780_Fearzone_Panel` | Investigate note | STATIC DEFECT CONFIRMED — both languages use generic EMA(120/450) crossover unrelated to README; original behavior, independent tests, fills and protection NOT IMPLEMENTED/NOT RUN |
| `0781_Fibonacci_Bollinger_Bands` | Investigate note | STATIC DEFECT CONFIRMED — both languages use generic EMA(120/450) crossover unrelated to README; original behavior, independent tests, fills and protection NOT IMPLEMENTED/NOT RUN |
| `0784_Fibonacci_Counter_Trend_Trading` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0785_Fibonacci_Levels_High_Low` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0786_Fibonacci_Retracement_Crypto` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0788_Fibonacci_Trend` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0789_Fibonacci_Trend_Reversal` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0790_Fibonacci_Only_V2` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0791_Fibonacci_Only` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0793_Financial_Ratios_Fundamental` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0794_Fine_Tune_Inputs_Fourier_Smoothed_Hybrid_Volume_Spread_Analysis` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0796_Fine_tune_Inputs_Gann_Laplace_Smooth_Volume_Zone_Oscillator` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0797_Finish_Renaming` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0798_Fisher_Crossover` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0801_FlexiMA_Variance_Tracker` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0802_FlexiMA_x_FlexiST` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0803_FlexiSuperTrend` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0805_FON60DK` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0809_Forex_Pair_Yield_Momentum` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0812_FreedX_Grid_Backtest` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0813_Friday_Bond_Short` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0815_Funamental_and_financials` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0816_Function_Logistic_Equation` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0817_Function_Simple_Markov_Chain_Monte_Carlo_Simulation` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0818_Function_Sorted_Indices` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0819_Weighted_Standard_Deviation` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0820_Function_Highest_Lowest` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0821_Function_Adf` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0822_Function_Linear_Regression` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0825_FVG_Breakout_Lite` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0829_Gann_Laplace_Smoothed_Hybrid_VSA` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0830_Gann_Swing_Multi_Layer` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0831_Gap_Down_Reversal` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0833_Gartley_222` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0834_Gaussian_Channel` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0836_GC_with_Trend_Filter_and_Sudden_Move_Profit_Taking` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0837_Gemini_Trend_Following_System` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0839_Get_Value_From_N_Years_Ago` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0840_Global_Index_Spread_RSI` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0842_GM8_ADX_Second_Ema` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0845_Gold_Friday_Anomaly` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0847_Gold_Pro` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0852_Gold_Volume_Based_Entry` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0853_Golden_Cross_VWMA_EMA` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0854_Golden_Transform` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0855_Good_Mode_Rsi_V2` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0856_Volume_Support_Resistance_Zones` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0857_Gradient_Trend_Filter` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0860_Grid_Like` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0866_Guage` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0871_HarmonicPattern` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0874_Heatmap_Macd` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0878_Hierarchical_K_Means_Clustering` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0880_High_Yield_Spread_SMA_Filter` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0880_High_Yield_Spread_with_SMA_Filter` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0882_Higher_Order_Pivots` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0884_Hma200_Ema20_Crossover` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0887_HOD_LOD_PMH_PML_PDH_PDL` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0888_Hoffman_Heiken_Bias` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0890_Stochastic_Exit_Alerts` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0892_How_to_use_Leverage_and_Margin` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0894_Hsv_And_Hsl_Gradient_Tools` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0895_HTF_Candles_Lib` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0910_Ichimoku_Cloud_Buy_Sell` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0924_Innocent_Heikin_Ashi_Ethereum` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0926_Intelle_city_World_Cycle_Ath_Atl_Logarithmic` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0927_Internal_Bar_Strength_IBS` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0932_InwCoin_Martingale` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0936_IU_BBB_Big_Body_Bar` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0939_Iu_Ema_Channel` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0940_IU_Gap_Fill` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0941_IU_Higher_Timeframe_MA_Cross` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0942_IU_Open_Equal_To_High_Low` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0944_IU_Range_Trading` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0947_John_Bob_Trading_Bot` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0949_Kaito_Box_with_Rsi_Div` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0950_Kaufman_Adaptive_Moving_Average` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0952_Keltner_Channel_Based_Grid` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0954_Keltner_Channel_Golden_Cross` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0955_Keltner_Channel` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0956_Khaled_Tamims_Avellaneda_Stoikov` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0957_KST_Skyrexio` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0958_KumoTrade_Ichimoku` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0960_Lanz_1_0_Backtest` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0961_LANZ_2_0_Backtest` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0962_Lanz_3_0_Backtest` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0965_Lanz_6_0_Backtest` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0966_Larry_Conners_SMTP` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0968_Larry_Connors_Bollinger_PercentB` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0970_Larry_Connors_Rsi_3` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0973_Linear_Continuation` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0980_Liquid_Pulse` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0982_Liquidity_Internal_Market_Shift` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0989_London_BreakOut_Classic` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0993_Long_Short_Exit_Risk_Management` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0994_Long_Term_Profitable_Swing_Abbas` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0996_Long_Only_MTF_EMA_Cloud` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1002_MA_Crossover_with_TP_SL_5_EMA_Filter` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1014_MACD_with_1D_Stochastic_Confirmation_Reversal` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1016_Machine_Learning_SuperTrend_TP_SL` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1017_Magic_Wand_STSM` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1022_Market_Trend_Levels_Non_Repainting` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1031_Function_Matrix_Library` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1035_Max_Pain` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1044_Mechanical_Trading` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1045_Medico_Action_Zone_Self_Adjust_TF_Version_2` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1050_MFS_3_Bars_Pattern` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1057_MOC_Delta_MOO_Entry_V2_REVERSE` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1058_MOC_Delta_MOO_Entry_v2` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1059_MOC_Delta_MOO_Entry` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1061_Modular_Range_Trading` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1062_MollyETF_EMA_Crossover` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1065_Momentum_Long_Short` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1069_Monte_Carlo_Simulation_Random_Walk` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1070_Monthly_Breakout` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1079_Moving_Average_Entanglement` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1086_Multi_Conditions_Curve_Fitting` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1105_MultiLayer_Acceleration_Deceleration` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1123_NQ_Phantom_Scalper_Pro` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1132_Octopus_Nest` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1138_Optimized_Heikin_Ashi_Buy_Sell` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1150_Overnight_Effect_High_Volatility_Crypto` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1158_Parent_Session_Sweeps_Alert` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1159_Pavan_CPR` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1161_PEAD` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1165_Pin_Bar_Reversal` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1187_Price_Based_Z_Trend` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1188_Price_Convergence` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1190_Price_Statistical_ZScore` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1191_Probability_of_ATR_Index` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1192_Professional_ORB` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1193_Profitable_Pullback_Mark804` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1194_Profitable_SuperTrend_MA_Stoch` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1195_ProfitView_Template` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1196_Projection` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1197_Prop_Firm_Business_Simulator` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1199_PS_January_Barometer_Backtester` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1200_Bollinger_Bands_Trailing_Stop` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1201_Pullback_Pro_Dow` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1202_PulseWave_Markking77` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1203_Pure_Price_Action_Breakout_with_1_5_RR` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1204_Pure_Price_Action` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1206_PVT_Crossover` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1208_QQQ_v2_ESL_easy_peasy_x` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1211_Quantitative_Trend_Uptrend_Long` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1212_Quantum_Reversal` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1213_Quatro_SMA` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1214_R_Based_Template` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1215_Rally_Base_Drop_SND_Pivots` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1216_Random_ATR_Bybit` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1217_Random_Coin_Toss` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1218_Random_Entry_and_Exit` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1219_Random_State_Machine` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1221_Range_Filter_ATR_Low_Drawdown` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1222_Range_Filter_ATR_TP_SL` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1224_Range_Filter` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1225_Range_Filter_DW` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1226_Rate_of_Change` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1227_Rawstocks_15_Minute_Model` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1229_Realtime_Delta_Volume_Action` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1230_RedK_Compound_Ratio_MA` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1231_RedK_Slow_Smooth_Average_RSS_WMA` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1232_Refined_MA_Engulfing` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1233_Refined_SMA_EMA_Crossover_with_Ichimoku_and_200_SMA_Filter` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1234_Reflected_Ema_Difference_RED` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1235_Reflex_Oscillator` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1237_Relative_Currency_Strength` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1239_Relative_Strength` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1240_Relative_Volume_at_Time` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1242_Renko_RSI` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1244_Resampling_Filter_Pack` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1245_Resampling_Reverse_Engineering_Bands` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1246_Responsive_Linear_Regression_Channels` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1247_Revelations` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1248_Reversal_Breakout_ORB` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1249_Reversal_Finder` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1250_Reversal_Trading_Bot` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1251_Reversal_Trap_Sniper` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1252_Reverse_Keltner_Channel` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1253_Revolution_Volatility_Bands_With_Range_Contraction_Signal_VII` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1254_Risk_Management_and_Positionsize_MACD_Example` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1255_Risk_to_Reward_FIXED_SL_Backtester` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1256_RKs_Framework_Auto_Color_Gradient` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1257_RMI_Trend_Sync` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1258_RSI_ADX_Long_Short` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1259_RSI_Backed_Weighted_MA` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1260_RSI_MACD_Long_Only` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1261_RSI_Stochastic_WMA` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1262_RSI_and_ATR_Trend_Reversal_SL_TP` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1263_RSI_Box_Pseudo_Grid_Bot` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1264_RSI_Buy_Sell_Force` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1265_RSI_Crossover_with_Compounding_Monthly` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1266_RSI_Cyclic_Smoothed` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1267_RSI_Divergence_AliferCrypto` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1268_RSI_Divergence` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1269_RSI_Long_Only_with_Confirmed_Crossbacks` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1270_Rsi_Long_Term_15min` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1271_RSI_ProPlus_Bear_Market` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1272_RSI_Long_Position_DAX_2_hours_Dow_Jones_1_hour` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1273_RSI_with_Adjustable_RSI_and_Stop_Loss` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1274_RSI_with_Manual_TP_and_SL` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1275_RSI_With_TP_SL_Lower_TF` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1277_RSI_Swing_Radar` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1278_RSI_Trend_Following` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1279_RSI_Volume_MACD_EMA_Combo` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1280_RSI_Adaptive_T3_Squeeze_Momentum` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1281_RSI_Adaptive_T3` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1282_RSI_CCI_Fusion` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1283_RTB_Momentum_Breakout` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1284_RVI_Crossover` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1285_SP_100_Option_Expiration_Week` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1286_S4_IBS_Mean_Rev_3candleExit` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1287_Safa_Bot_Alert` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1288_Scale_In_Scale_Out` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1289_Scalping_15min_EMA_MACD_RSI_ATR` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1290_Scalping_By_TradingConToto` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `1291_Scalping_With_Williams_R_MACD_and_SMA` | Investigate note | NOT REVIEWED — requires independent C#/Python archive verification |
| `0911_Ichimoku_Clouds_Long_and_Short` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0913_ICT_Indicator_with_Paper_Trading` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0916_iD_EMARSI_on_Chart` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0922_Indicator_Test_With_Conditions_Table` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0925_Inside_Candle` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0929_Intraday_Combo_HH` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0930_Intraday_Momentum` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0934_is` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0935_IU_4_Bar_UP` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0938_IU_Break_Of_Any_Session` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0946_JMA_Quantum_Edge` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0948_The_Price_Radio` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0953_Keltner_Channel_by_Kevin_Davey` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0959_Kyrie_Crossover` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0967_Larry_Conners_Vix_Reversal_II` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0969_Larry_Connors_3_Day_High_Low` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0974_Linear_Correlation_Oscillator` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0976_Linear_Mean_Reversion` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0978_Linear_Regression_All_Data` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0979_Linear_Regression_Channel` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0984_Liquidity_Grab_Volume_Trap` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0985_Liquidity_Sweep_Filter` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0986_Litecoin_Trailing_Stop` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0992_Long_Explosive_V1` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0995_Long_Leg_Doji_Breakout` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0998_LSMA_Fast_Simple_Alternative_Calculation` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `0999_Lube` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1000_Lunar_calendar_day_Crypto_Trading` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1004_MA_MACD_BB_BackTester` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1007_MACD_Aggressive_Scalp_Simple` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1008_MACD_Crossover` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1011_MACD_of_Relative_Strenght` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1012_MACD_Volume_BBO_Reversal` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1015_Machine_Learning_Logistic_Regression` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1019_Markdown_The_Pine_Editors_Hidden_Gem` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1020_Market_EKG` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1024_Martin_No_Loss_Exit_V3` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1025_Martingale_with_MACD_KDJ_opening_conditions` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1029_MathStatisticsKernelFunctions` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1032_Mawreez_RSI_Divergence_Detector` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1034_Max_Gain` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1036_Max_Profit_Min_Loss_Options` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1037_McClellan_AD_Volume_Integration_Model` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1038_McGinley_Dynamic_Improved` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1040_Mean_Deviation_Index` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1042_Mean_Reversion_V_F` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1043_Mean_Reversion_With_Incremental_Entry` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1047_Merovinh_Mean_Reversion_Lowest_Low` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1048_MESA_Stochastic_Multi_Length` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1051_MH_Hull_Moving_Average_Based_Trading` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1052_MicuRobert_Ema_Cross` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1054_MK_Custome_Adaptive_SuperTrend` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1055_MM_Fibonacci` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1056_MNQ_EMA` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1066_MomentumSync_PSAR_RSI_ADX_Filtered_3_Tier_Exit` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1068_Monte_Carlo_Range_Forecast` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1072_Monthly_Performance_Table` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1074_Monthly_Returns` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1076_MA_Crossover_with_Take_Profit_and_Stop_Loss` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1080_Moving_Average_Rainbow_Stormer` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1083_Moving_Regression` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1084_MTF_Oscillator_Framework` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1085_MTF_Seconds_Values_JD` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1089_Multi_Timeframe_RSI_Buy_Sell` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1097_Multi_Step_FlexiMA` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1098_Multi_Step_FlexiSuperTrend` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1099_Multi-Step_Vegas_SuperTrend` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1100_Multi_TF_AI_SuperTrend_with_ADX` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1103_Multi_Timeframe_RSI_Grid_with_Arrows` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1104_Multi_Timeframe_Trend_200_EMA_Filter_Longs_Only` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1106_MultiLayer_Awesome_Oscillator_Saucer` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1108_MVO_MA_Signal` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1113_Nasdaq_Day_and_Night_Breakdown` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1115_NEO_IMACD_SL_y_TP` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1121_Normalized_Oscillators_Spider_Chart` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1122_Nova_Futures_Pro_Safe_v6` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1124_NSE_Index_with_Entry_Exit_Markers` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1129_OBV_ATR` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1130_OBV_Traffic_Lights` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1133_OKX_MA_Crossover` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1136_Optimized_Auto_Detect` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1137_Optimized_Grid_with_KNN` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1139_Lorenzo_Super_Scalp` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1143_ORB_VWAP_Braid_Filter` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1144_ORB_Heikin_Ashi_SPY_Correlation` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1145_Order_Block_Finder` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1146_Oscillator_Evaluator` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1147_Out_of_the_Noise_Intraday_with_VWAP` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1151_Overnight_Positioning_EMA` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1152_P_Square_Nth_Percentile` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
| `1160_Payday_Anomaly` | Verify behavior | NOT REVIEWED — requires independent C#/Python archive verification |
