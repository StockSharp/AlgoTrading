# Point 1 / Part 10 — incomplete verification ledger

Source: uploaded TZ-10.md; **315 exact strategy keys** (229 issue checks, 86 contract checks). This document does not replace or modify the source specification.

## Environment and coverage

- Target: `StockSharp/AlgoTrading`, isolated branch `feature/tz-point1-part10` created from `main`.
- Modified implementations: `3846_Graal_Fractal_Channel` and `3849_Gaps_Reversion` (both C# and Python).
- Added `Tests/StrategyTests.Point1Part10.cs` with a fractal oracle and a gap oracle. Existing generated default tests remain enabled; no `requireTrades` bypass.
- Updated all seven localized strategy README files for each changed strategy.
- **No MSBuild, MSTest, Python runtime import, end-to-end replay, structural validation, or external oracle tests ran** in this environment: the GitHub integration can read/write repository files but cannot execute code with the required sibling StockSharp checkout or packaged BTC/TON archive.
- **No strategy is certified complete.** The source modifications must not be reported as verified fixes until tests and real fills pass.
- `AGENTS.md` is not present in the connected GitHub repository's `main` checkout. The workspace `configs/instructions.md` is not available through the connected GitHub installation, so those additional instructions still need verification in the local workspace.

## Commands required on the isolated local worktree

Use one MSBuild/MSTest worker at a time. From the AlgoTrading root, with sibling `StockSharp (GitHub)` checkout:

```bash
python Tools/validate_api_structure.py
timeout 600s dotnet build Tests/Tests.csproj --configuration Release --maxcpucount:1
timeout 600s dotnet test Tests/Tests.csproj --no-build --configuration Release --filter "FullyQualifiedName~S3846_|FullyQualifiedName~S3849_" -- RunConfiguration.MaxCpuCount=1
```

Use the filtered command for red/green observations and add parameter-profile-specific replay checks for both C# and Python. Do not label default behavior verified merely because a reduced warm-up profile trades.

## Per-strategy status

| Key | Status | Concrete outstanding reason |
|---|---|---|
| `3846_Graal_Fractal_Channel` | Incomplete (C#/Python changes + test authored) | Requires red/green replay, compilation, actual fill/remaining-position assertions, stop/TP/Williams/counter-order branch coverage, default-vs-custom profile and seven-language cross-check. |
| `3847_Gselector_Pattern_Probability` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3848_Gold_Warrior02b_Impulse` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3849_Gaps_Reversion` | Incomplete (C#/Python changes + test authored) | Requires red/green replay, archive gap opportunities, quote-spread oracle, order/fill verification, started-vs-finished candle timing and seven-language cross-check. |
| `3850_Fractured_Fractals_MQL4` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3851_Firebird_MA_Envelope_Exhaustion` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3852_Farhad_Crab` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3854_Ema_6_12` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3855_E_Friday_Session` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3856_Disaster` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3858_Trend_Scalper` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3859_Bronze_Pan` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3861_BadOrders` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3862_5_8_MA_Cross_Protect` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3863_MultiTimeframeEmaAlignment` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3864_Two_PerBar_Ron` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3865_AK47_A1` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3868_CrossMA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3870_MA2CCI_Adaptive_Volume` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3871_MA2CCI_Classic` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3873_Trade_Channel_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3874_Trendcapture` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3879_MoStAsHaR15_Pivot_Line` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3880_Lbs_V12` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3883_Graal_Ema_Momentum` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3884_Otkat_Sys` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3885_Weekly_Rebound_Corridor` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3886_Morning_Pullback_Corridor` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3887_Double_Up` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3888_Nextbar` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3890_ASCPlusPlus` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3891_SilverTrendV3_JTPO` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3892_UsdChf_New` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3893_DoubleUp2` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3894_KSRobot15` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3895_Ichimoku_2005` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3899_Up3x1_Premium_2vM` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3900_Combo_Right_Perceptron` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3901_Moving_Average_Shift` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3903_Trade_Channel_ATR` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3904_OzFx_Simple` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3905_Fibo_Pivot_MultiVal` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3906_Universum_3_0` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3907_Money_Rain_Recovery` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3909_VLT_Trader_Straddle` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3912_4_SMA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3914_Fx_Chaos_Pyramid` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3915_TRAYLERv` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3916_TCPivotStop_Floor_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3918_Fortrader_10_Pips` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3919_et4_MTC_v1` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3920_Universal_Investor` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3921_GTerminal` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3923_Alert_MACD_Slow` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3924_Up3x1ShiftedSMA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3925_Up3x1_Krohabor_Shift` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3928_MA_S_R_Trading` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3929_Forex_Profit_System` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3930_One_Two_Three_Pattern` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3931_HBS_System` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3932_TrendCapture_Legacy` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3933_Pending_Limit_Grid` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3934_Up3x1_Investor_Range_Filter` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3935_HarVesteR_Macd_Trend` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3936_Manual_EA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3938_GPF_TCPivotLimit` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3939_TCPivotStop_Session` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3940_Pipsover` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3942_FullDamp` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3943_AMATraderV21` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3944_RndTrade_RandomHold` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3945_Kloss` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3947_Macd_Pattern_Trader_v01` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3956_Polish_Layer_Expert_Advisor_System_Efficient` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3958_ThePuncher` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3959_Multi_Indicator_Optimizer` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3960_Pendulum_Swing` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3962_Momo_Trades_V3` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3964_Gazonkos_Expert` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3966_Three_MA_Cross_Channel` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3967_Fuzzy_Logic` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3970_Trend_Follower_Rainbow` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3972_Macd_Pattern_Trader_All_v001` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3974_NatusekoProtrader4H` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3975_ZigAndZag_Scalpel` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3976_Vector_Basket_Trend` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3977_Altarius_RSI_Stochastic_Dual` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3978_Moving_Average_Position_System` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3981_Trend_RDS_Reversal` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3982_RAVIiAO` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3983_Sidus_v1` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3984_20_Pips_Price_Channel` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3985_MAMACD_NoVlt` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3987_Divergence_Trader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3988_EMA_Cross_2` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3990_Risk_Monitor` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3991_MartingailExpertV10Stochastic` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3996_Open_Close` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3997_MACD_Zero_Filtered_Cross` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4001_Laptrend_1` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4002_SimpleTrade_Flip` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4003_MA_Cross_Method_PriceMode` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4004_FT_BillWillams_Trader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4005_FT_Bill_Williams_AO` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4006_TenPips_Opposite_Last_N_Hour_Trend` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4007_Swetten` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4009_BullVsMedved_Window` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4011_Ilan1_4_Grid` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4015_CorrectedAverageChannel` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4016_Rsi_Test` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4024_AppPrice_Level_Cross` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4026_Pivots` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4028_Expert_Master_EURUSD` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4030_Regularities_of_Exchange_Rates` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4037_ACB1` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4038_MA_Reverse` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4040_A_System_Championship` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4045_Backbone_Basket` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4047_21hour` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4050_Smoothed_MA_Directional` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4051_Profit_Hunter_HSI_with_Fibonacci` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4052_MTrendLine` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4053_Alligator_Volatility` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4056_Billy_Expert_Reversal` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4057_Future_Pattern_Memory` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4063_Locker_Hedging_Grid` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4064_Spazm_Volatility_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4065_Pipso_Night_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4067_AIS1_Trading_Robot` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4069_NTK07Grid` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4070_Very_Blondie_System` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4071_SVOS_EURJPY_D1` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4072_Sidus_EMA_RSI` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4073_TrueSort_1001` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4074_Wave_Power_EA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4075_Stairs` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4077_Glfx` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4078_Alliheik_Trader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4079_AIS2_Trading_Robot` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4080_Gandalf_PRO_Projection` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4081_Two_MA_Four_Level_Bands` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4082_AIS3_Trading_Robot_Template` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4083_AIS_Trade_Machine` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4084_ADXSystem_DiCross` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4091_TrainYourself` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4092_Elli` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4093_NeuroNirvaman` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4094_Fractal_ZigZag` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4095_Nirvaman_Imax` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4096_STO_M5xM15xM30` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4097_Simple` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4098_Exp_Amstell` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4100_JMaster_RSX` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4102_Wajdyss_MA_Expert` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4103_SuperForexV2` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4111_EmaRsiVa_Crossover` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4112_Moving_Average_With_Frames` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4113_Forex_Sky` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4116_Rubberbands_3` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4121_ComFracti_Fractal_Rsi` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4129_FT_Time_Bigdog` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4130_ADX_Simple` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4131_Exp_Fibo_Zz` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4135_Scalpel` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4138_Expert610_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4139_Lcs_Macd_Trader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4141_Multi_Lot_Scalper` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4143_MACD_Sample_1010` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4144_SendCloseOrder` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4145_RobotPowerM5_Meta4V12` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4147_Ema_Wma_Crossover_Risk` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4149_Escape_MeanReversion` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4151_Nevalyashka_Direction` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4152_Vortex_Indicator_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4155_PsarBug` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4156_Parabolic_SAR_Crossover_Alert` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4158_Casino111` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4160_NewFSCEA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4162_Grail_Expert_MA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4163_Parabolic_Sar_Bug2` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4167_Six_Indicators_Momentum` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4169_TrailingStopFrCn` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4170_TDSGlobal_4` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4171_Envelope_MA_Short` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4172_News_Trader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4174_Stoch_Levels` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4175_My_System` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4176_Mo_Bidir` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4177_Ema_Wma_Rsi` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4178_MARE5_1_Shift` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4179_Moving_Average_Money` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4181_Nevalyashka_BreakdownLevel` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4182_MACD_Signal_Atr` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4183_Heiken_Ashi_Smoothed_MTF` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4184_JK_BullP_AutoTrader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4185_10point3_Macd_Grid` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4186_TenPoints3` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4187_10_Points_3_V005` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4189_Early_Bird_RangeBreak` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4190_Colibri_Grid_Manager` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4191_Ard_Order_Management_Command` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4192_Udy_Ivan_Madumere` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4194_Speed_MA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4195_Parabolic_SAR_Bug_3` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4196_TrailingStopFrCnSar` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4197_OsMaMaster` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4199_Easiest_Ever_Daytrade` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4200_SecondEasiest` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4202_RobotADX2MA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4203_Early_Open_Trend` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4204_Easiest_RSI` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4205_RandomT` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4206_MACDNotSoSample` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4208_Take_Profit_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4209_Slope_Rsi_Mtf` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4211_Cyclops_CycleIdentifier` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4212_Early_Top_Prorate_V1` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4213_VmMatrixDoubleZero` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4214_Stop_Hunter` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4215_Flat_001a` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4216_PSAR_Multi_Timeframe` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4217_CCIT3_Zero_Cross` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4218_Rsi_Ma` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4219_ErrorEA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4220_MAMirror` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4221_DVD_100_50_Cent` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4223_XBug` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4226_VivaLasVegas` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4227_Daily_STP_Entry_Frame` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3853_Ema_Cross_Contest_Hedged_Ladder` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3857_Dlmv1Grid` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3860_Breadandbutter2` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3866_AG_MACD_Dual` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3867_Equal_Volume_Range_Bars` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3876_Omzdwwi_Pending_Manager` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3878_True_Scalper_Profit_Lock_BreakEven` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3881_Synchronized_Hour_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3889_Eugene` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3896_20_200_Time_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3898_T3MA_MTC9` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3902_Auto_KDJ` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3910_Elite_eFibo_Trader_v2_1` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3911_Get_trend_Stochastic` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3913_FX_CHAOS_Scalp_MT4` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3917_TCP_Pivot_Limit_Reversal` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3922_ADX_MA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3927_Hercules` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3937_VQ_EA` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3946_Monday_Typical_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3948_Kloss_Simple` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3949_Macd_Pattern_Trader_V02` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3951_TheMasterMind_Reversal` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3952_Waddah_Attar_Win` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3953_ZigAndZag_Trader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3954_Macd_Pattern_Trader_v03` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3955_SHE_Kanskigor` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3957_MasterMind2` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3961_MacdPatternTrader_DoubleTop` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3963_CashMachine_5min` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3965_MacdPatternTrader_Trigger` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3973_DeMark_Lines` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3980_EES_Hedger` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3986_Straddle_Trail_V240` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3989_Cyberia_Trader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3992_ERegression_Channel` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3993_CCI_MA_v1_5` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3994_RPoint_250` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3995_ZigZag_EvgeTrofi_1` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3998_Support_Resistance_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `3999_Open_Close2Ampn_Stochastic` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4000_Master_Mind_Triple_Wpr` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4013_FT_TrendFollower` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4014_Brandy_v1_2` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4017_Starter` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4018_20_Pips_Once_A_Day_Opposite_Trend` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4021_RabbitM2_Regime_Swing` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4022_Awesome_Fx_Trader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4023_Rabbit_M3` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4025_DayTrading_Impulse` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4027_CyberiaTraderAI` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4031_OpenTiks` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4032_Divergence_Trader_Basket` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4041_PROphet` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4043_Center_Of_Gravity_Mean_Reversion` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4044_EMA_Cross_Trailing` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4049_Hans123Trader_RangeBreakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4054_N7SAO772012` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4055_Stat_Euclidean_Metric` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4060_Multi_Time_Frame_Regression` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4061_Parabolic_SAR_Fibo_Limits` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4086_Parabolic_SAR_Limit` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4087_Open_Time_Daily_Window` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4089_TSTPullbackReversal` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4099_Ard_Order_Management_Stochastic` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4101_Amstell_SL` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4106_Elite_eFibo_Trader` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4107_NtoQf` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4108_Rubberbands_Safety_Net` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4109_Auto_RXD_V1_67` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4110_Rubber_Bands_Grid` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4119_Champion` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4125_ComFracti` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4136_For_Max_V2` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4137_AIS5_Trade_Machine` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4140_FiftyFiveMedianSlope` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4153_Vortex_Oscillator_System_Momentum` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4157_Combo_Ea4FsfrUpdated5` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4159_Lilith_Goes_To_Hollywood` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4161_Multi_Combo` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4164_Parabolic_Sar_Flip_Alert` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4188_Early_Bird_Range_Breakout` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4201_Hard_Profit` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4210_Huge_Income` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4222_Parabolic_SAR_First_Dot` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
| `4224_Daily_Trend_Reversal` | Incomplete | Both implementations, default parameters, all documented entry/exit/risk branches, and independent execution oracle not yet reviewed and replay-tested. |
 
## Merge / isolation notes

No shared `Tests/StrategyInventory.cs`, `Tests.csproj`, helpers, other part-specific test files, CI, scripts, or sibling repositories were changed. The new part-specific test file is the only test source added. Only these two assigned strategy directories and this ledger should be in the part-10 diff.

## Important limitations

The tests have **not** been run even once, so their assertions, event ordering, and the proposed strategy implementations are still unverified. In particular, finished-only candle providers discover a gap only at the candle close and cannot establish entry-at-open execution. Do not mark the remaining 313 strategies as passed. No profitability claims are supported by these changes.
