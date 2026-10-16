# Point 1 / Part 09 — verification record (incomplete)

**Scope:** exactly the 316 strategy keys in TZ-09.md. A key covers C# and Python.
**Status:** NOT COMPLETED. All 316 required C#/Python/README file structures were verified (see structure-verification.md); no strategy is marked behaviorally verified without an independent replay.

## Environment and restrictions

- GitHub-only source access in this session; the Windows checkout `D:\stocksharp\AlgoTrading` and its sibling `StockSharp (GitHub)` are not mounted.
- No `dotnet` executable, MSBuild, MSTest worker or bundled BTC/TON archive available in the execution container; test execution, red/green demonstration, compile and real fills cannot be certified.
- `AlgoTrading/AGENTS.md` is absent on the inspected default GitHub branch, and sibling `configs/instructions.md` was not retrievable through the available connection.
- The complete 316-key file structure is verified. Parameter names were inspected in all seven READMEs for 3410/3413/3433 and the M5 CandleType declaration for 3467. Full translations, default behavior and execution mechanics require verification. `requireTrades` was not changed; no test runs or history downloads were attempted.
- Changes in this branch are limited to assigned C#/Python strategy implementations (3410, 3413, 3433, 3467), the prescribed partial test file and this report. No other repository was modified.

## Reproduction commands (NOT executed)

On the required Windows checkout with the local history and a single MSBuild/MSTest worker, run for each relevant key a command in this form (replace the suffix and choose CSharpTests and PythonTests separately):

```bash
timeout 600s dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~CSharpTests&Name~S3433_AhHmMfi' --blame-hang-timeout 5m -- RunConfiguration.MaxCpuCount=1
timeout 600s dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~PythonTests&Name~S3433_AhHmMfi' --blame-hang-timeout 5m -- RunConfiguration.MaxCpuCount=1
```

The commands above are illustrative Unix-shell timeout wrappers; use a corresponding process timeout on Windows. Filter each further run by the specific strategy; do not run a full unfiltered catalog.

## Per-strategy status

| Key | Result / reason |
| --- | --- |
| `3410_Five_Minute_Rsi_Cci` | Unfinished: C# and Python were rewritten with the documented RSI, SMMA, EMA, two CCI, bid/ask spread gating, equity-based volume, native SL/TP and manual quote-driven trailing. An independent C#/Python-inherited reference replay test S3410 was added, but neither language has been compiled, exercised or green-verified. |
| `3411_NNFXAutoTrade` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3413_5Mins_Envelopes` | Unfinished: C# and Python implement M5 median-price LWMA envelopes, previous-bar extremes, live bid/spread entry gating, requested volume and local SL/TP plus quote-driven trailing. Cross-language independent S3413 archive replay test added; compile, actual fills and risk exits unverified. |
| `3414_ABE_BE_CCI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3417_Engulfing_MFI_Confirmation` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3419_Abe_Be_Rsi` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3422_Bullish_Bearish_Harami_Stochastic` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3425_ABHBHMFI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3426_MarketMaster` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3428_Hammer_Hanging_Stochastic` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3430_Hammer_Hanging_Man_CCI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3432_DreamBot` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3433_AH_HM_MFI` | Unfinished: C# and Python now use MFI(47), SMA(5), documented hammer/hanging-man thresholds and crossing exits. Independent cross-language S3433 oracle test added. Without the .NET history replay, neither real fills nor default profitability are certified; README's unspecified emergency-stop behavior remains open. |
| `3434_AH_HM_RSI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3435_MeetingLinesStochastic` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3436_Master_Exit_Plan` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3437_Alerting_System` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3438_Alerting_System_Threshold` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3439_Mission_Impossible_Power_Two_Open` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3440_AML_CCI_Meeting_Lines` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3442_Specific_Day_Time` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3443_Expert_AML_MFI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3444_AML_RSI_Meeting_Lines` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3446_Morning_Evening_Star_CCI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3447_Simple_Martingale_Template` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3448_Morning_Evening_MFI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3449_AMSESRSI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3451_Basic_Martingale_EA_3` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3453_Fibonacci_Potential_Entries` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3455_Bollinger_RSI_MA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3459_Candle_Patterns_Test` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3460_Spread_Informer` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3462_Spread_Data_Collector` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3466_Ronz_Auto_SLTP` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3467_The_Enchantress` | Unfinished: C# and Python CandleType defaults changed from M60 to documented M5; pattern classifications, virtual orders, real protective executions, while full translation semantics remain unverified. |
| `3468_Trading_Panel` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3469_TradingPanel_Batch` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3470_MACross` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3471_AddOn_TrailingStop` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3473_AutoTrading_Scheduler` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3474_Daily_Target` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3475_Day_Opening_MACD_Histogram` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3476_Averaging_By_Signal` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3477_Scalping_Assistant` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3480_Trading_Panel_Control` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3484_IsConnected` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3486_AverageCandleCross` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3489_Buy_Sell_On_Your_Price` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3490_Commission_Calculator` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3491_Time_Bomb` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3492_Simple_Engulfing` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3494_MA_Break_Impulse_Buy` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3496_StochasticAccelerator` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3497_More_Orders_After_BreakEven` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3498_Heiken_Ashi_Engulf` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3499_Alligator_Candle_Cross` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3501_EA_Objprop_Chart_Id` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3502_Tops_Bottoms_Trend_RSI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3504_Sample_Trailing_Stop` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3508_Close_Profit_End_Of_Week` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3509_OpenPendingorderAfterPositionGetStopLoss` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3514_Hpcs_Inter6_Rsi` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3516_HPCS_Inter5` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3517_HPCS_Inter7` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3518_HPCS_Inter4` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3519_PosNegDiCrossover` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3521_Fast_Slow_MA_Crossover` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3522_iCHO_Trend_CCIDualOnMA_Filter` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3527_TurnGrid` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3528_MA_Grid` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3529_Bull_Row_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3538_MA_on_Momentum_Min_Profit` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3539_MACD_Four_Colors_2_Martingale` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3552_VIDYA_N_Bars_Borders_Martingale` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3554_iVIDyA_Simple` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3556_Trickerless_RHMP` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3558_At_Random_Full` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3560_SR_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3562_Ten_Kijun_Cross` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3568_Mean_Reversion_Momentum` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3572_Conditional_Position_Opener` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3575_RSI_Alert` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3577_XP_Trade_Manager_Grid` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3583_CCI_MACD_Scalper` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3584_Multi_Currency_Template_MT5` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3588_Pause_Trading_On_Consecutive_Loss` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3589_Divergence_Macd_Stochastic` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3590_Divergence_Ema_Rsi_Close_Buy_Only` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3593_Aussie_Surfer_Ltd` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3601_SwingTrader` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3610_MAPriceCross` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3611_MartingaleEA_5_Levels` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3616_Exp_TEMA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3618_KA_Gold_Bot` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3626_Band_OsMA_Custom` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3627_RectangleTest` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3628_RangeEA_Weekly_Grid` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3629_Random_Trader_Bias` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3630_Maybeawo222` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3633_ScalperEMA_Simple` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3635_Martingale_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3636_MartinGale_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3640_Mnist_Pattern_Classifier` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3641_Martingale_Trade_Simulator` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3643_Connect_Disconnect_Sound_Alert` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3644_Symbol_Sync` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3645_Bounce_Number` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3649_KA_Gold_Bot` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3652_Close_on_Profit_or_Loss_in_Account_Currency` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3660_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3671_Grid_EA_Pro` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3672_Moving_Average_Price_Cross` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3678_Two_Pair_Correlation` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3680_Neural_Network_ATR` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3681_AdaptiveTraderPro` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3685_EuroSurge_Simplified` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3686_Backtesting_Trade_Assistant_Panel` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3690_Close_Agent` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3692_Profit_Labels` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3693_Surfing_3_0` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3697_Symbol_Swap` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3698_Symbol_Swap_Panel` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3708_RRS_Non_Directional` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3711_RRSChaotic` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3713_RRS_Tangled_EA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3720_Adjustable_Moving_Average` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3722_StellarLite_ICT_EA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3723_RRS_Impulse` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3724_RM_Stochastic_Band` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3725_RiskManagementATR` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3727_Butterfly_Pattern` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3730_Sniper_Jaw` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3731_Pending_tread` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3732_Tuyul_Gap_End_Of_Week` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3734_MarsiEa` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3736_Tuyul_Uncensored` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3746_Exp_TrendMagic` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3768_Wss_Trader` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3769_Volume_Trader_V2` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3770_Vlado_Williams_Percent_Range` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3772_Universal_MA_Cross_V4` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3775_Terminator` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3776_Sweet_Spot_Extreme` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3777_SurefireThing` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3778_Suffic369` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3779_MACD_Stochastic_Filter` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3781_Smc_Trader_Camel_Cci_Macd1` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3786_Martingail_Expert_Sequence` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3787_Semilong_Www_Forex_Instruments_Info` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3790_RPM5_Bulls_Bears_Eyes` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3791_Plan_X_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3795_NRTR_Reversal` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3796_Noah10Pips2006` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3797_NinaEA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3798_Myfriend_Forex_Instruments` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3803_Macd_Stochastic_Trailing` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3804_3MA_Bunny_Cross` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3806_5min_RSI_Qualified` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3807_L3H3Pivot` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3809_Above_Below_MA_Rejoin` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3810_Alexav_D1_Profit_GBPUSD_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3812_Aocci_Pivot` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3813_Artificial_Intelligence_Right` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3815_Simple_FX_Crossover` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3817_ASCV` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3818_Bago_EA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3820_Binario_3` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3821_Blockbuster_Bollinger_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3822_BreakOut15` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3825_Contrarian_trade_MA_Weekly` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3826_Daydream` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3827_DojiArrows_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3829_Donchain_Counter_Channel_System` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3830_DoubleMA_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3831_DT_RSI_EXP1` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3832_Envelope_Limit_Ladder` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3834_Elderv30aug05v` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3835_EMA_Pullback` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3836_Karakatica` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3840_Franks_4Hour_Limit_Orders` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3841_FrBestExp02_Maloma_Mod` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3843_GBP9AM` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3844_Get_Rich_Gbp_Session_Reversal` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3845_Glam_Trader` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3412_Range_Breakout_Weekly` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3421_Peter_Panel` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3423_Harami_CCI_Confirmation` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3424_RangeBreakout2` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3427_TemplateEAbyMarket` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3429_Template_M5_Envelopes` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3445_Universal_Signal_Demo` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3450_Simplest_DeMarker` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3454_Fibonacci_Potential_Entries_Retracement` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3456_Candlestick_Stochastic` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3457_IFS_Fractals` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3458_Smart_Forex_System` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3461_Ichimoku_Price_Action` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3463_TugbaGold` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3472_Close_Orders_Risk_Control` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3478_Dual_Stoploss` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3479_One_Price_SL_TP` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3481_Tipu_MACD_EA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3482_SpaceX_Delete_StopLoss_TakeProfit_button` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3483_Volume_Calculator` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3485_ValidateMe` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3487_Macd_Diver_And_Rsi` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3488_MACD_Divergence_RSI` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3493_MA_Break` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3495_Williams_Percent_Directional_Index` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3500_Zone_Recovery_Hedge` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3503_Manual_Position_Tracking_Panel` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3505_Sample_Trailingstop_MT5` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3506_Sample_Check_Pending_Order` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3520_Fast_Slow_RVI_Crossover` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3530_RsiMaOnRsiFillingStep` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3531_RsiMaOnRsiDual` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3532_Two_MA_Other_TimeFrame_Correct_Intersection` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3533_CHO_Smoothed_EA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3534_PriceChannel_Signal_v2` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3535_DeMarker_Gaining_Position_Volume` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3543_DeMarker_Gaining_Position_Volume_2` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3545_AdaptiveGridMT4` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3546_DeMarker_Pending_2` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3548_Rsi_Dual_Cloud` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3550_AMA_Trader_2` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3551_DeMarker_Pending` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3553_OsMA_Four_Colors_Arrow` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3555_WPR_Custom_Cloud_Simple` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3557_At_Random` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3559_Two_Pending_Orders_2` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3561_MACD_Fixed_PSAR` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3564_Turbo_Scaler_Grid` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3574_LazyBot_V1` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3585_GridTradingVolatileMarket` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3592_Reverse` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3594_Cryptos` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3597_Risk_Manager_Layered` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3614_AK_47_Scalper` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3617_Resonance_Hunter` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3623_Matrix_Machine_Learning` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3625_BandOsMa` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3631_RSI_Martingale` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3632_Moving_Average_Crossover_Spread` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3637_BBStrategy` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3638_Donchian_Scalper` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3646_Neural_Network_Template` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3647_Indices_Tester` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3648_Balance_Drawdown_In_MT4` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3651_Trail_SL_Manager` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3670_Prop_Firm_Helper` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3674_Raymond_Cloudy_Day` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3679_Revised_Self_Adaptive_EA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3687_Smart_Trend_Follower` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3689_SUPERMACBOT_BY_THE_GUARDIAN_FOREX_TV` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3691_MarketPredictor` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3694_Trailing_Stop_Manager_Trigger` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3696_Bollinger_Bands_Session_Reversal` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3706_BreakRevertPro` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3707_Mean_Reverse` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3709_ATR_Step_Trader` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3716_MP_Candlestick` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3719_MeanReversion_Donchian` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3721_Bands` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3733_Extreme_Strength_Reversal` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3737_Cs2011` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3740_Multi_EA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3742_TradingLab_Best_MACD` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3743_Build_Your_Grid` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3745_TradePad_Sample` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3747_TrendMeLeaveMe_Channel` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3750_Frank_Ud_Minimal` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3751_Swaper` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3752_Dealers_Trade_v7_51_RIVOT` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3753_Dealers_Trade_MACD_MQL4` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3754_OSF_Countertrend` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3755_MACD_Sample_Classic` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3756_ProtoType_IX` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3757_Cloudzs_Trade_2` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3758_MACD_Signal_Crossover` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3760_e_TurboFx` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3761_e_TurboFx_Momentum` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3762_Mc_Valute_Cloud` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3763_Stochastic_Martingale_Grid` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3764_Cronex_DeMarker_Crossover` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3765_Lucky_Code` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3766_Lucky_Shift_Limit` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3767_Zs1_Forex_Instruments` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3771_VarMovAvg` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3773_Tunnel_Method_EMA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3774_Time_Based_Range_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3780_Starter_V6Mod_E` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3782_Smc_Hilo_MaxMin` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3783_Simple_Pivot_Flip` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3784_Simple_MACD_EA` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3785_Session_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3788_Russian20_TimeFilter_Momentum` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3789_RSI_Trader_Aligned_Averages` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3792_PivotEMA3RLHv4` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3793_Pedro_Mod` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3794_OverHedge_V2_Grid` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3799_MultiBreakout_v001k` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3800_ExpICustomV1` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3802_BollTrade_Bollinger_Reversion` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3805_Triple_SMA_Crossover` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3808_Anubis_Cci_Macd` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3811_Alexav_SpeedUp_M1_Body` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3814_Avalanche` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3816_Arttrader` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3819_Bollinger_Band_Pending_Stops` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3823_Bruno_Trend_Filter` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3824_Channels` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3828_Doji_Trader_Breakout` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3833_ENewsLuckyw` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3837_Farhad_Hill_Version_2` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3838_FitFul_13_Time_Gated` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |
| `3839_Flat_Trend` | Unfinished: README, C#, Python, independent signal oracle and execution/protection replay have not been reviewed or run in this session. |

## Tests written but not executed

- `Tests/StrategyTests.Point1Part09.cs`: `S3410_IndependentFiveIndicatorEntriesAndFilledVolume`, `S3413_IndependentMedianLwmaEnvelopeBidEntriesAndActualFills` (two profiles), `S3433_IndependentMfiSmaPatternEntriesAndCrossingExits`; each inherited by CSharpTests and PythonTests.
- Execution results: **unknown**; red/green TDD cannot be claimed because the SDK, packaged history and MSBuild worker are unavailable. A static code review is not a substitute for the test runs.
- Protection fill checks, semantic translations, other assigned strategy contracts and all failures found after compiling remain outstanding. Structural completeness of all 316 keys and the documented parameter-name surfaces of the four touched examples were checked separately.

## Next verification steps

Review all seven README translations for each modified key, add independent red/green behavior tests in `Tests/StrategyTests.Point1Part09.cs`, use only the packaged BTC/TON history, verify fills/position/protection/default parameters on both engines and record filtered test logs. Do not close the part while any of the 316 rows is unverified.
