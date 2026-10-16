# Point 1 · Part 01 — coverage register

Source: user-provided `TZ-01.md`. Exact strategy keys only; **315** keys, **630 language implementations**. This is a *progress register*, not a claim that the assigned part is complete.

## Current results

- **0333_Keltner_Seasonal_Filter** — partial: C#/Python source was inspected; the use of `candle.OpenTime.Month` was confirmed in both implementations. All seven localized READMEs now specify formulas, calendar gating, manual bar-close stop semantics, and entry/exit rules. Two test methods were authored in `Tests/StrategyTests.Point1Part01.cs`; **neither compiled nor executed here**. The Python two-start-month replay is unverified. The packaged BTC/TON fixture spans **March 1–31, 2024** (StockSharp.Configuration/Paths.cs), so it cannot observe a forbidden-season candle or a September short. An independent positive/forbidden breakout fixture and actual stop-fill proof are still outstanding. **Status: not complete.**
- The remaining **308** exact keys (all except 0333, 0103, 0121, 0122, 0303, 0305 and 0307) lack authored regression tests in this branch. None of these seven strategies is certified complete: no branch build or history replay was executed. 0302 and 0352 have additionally been inspected statically, revealing discrepancies but requiring an independent fix and executable proof.
- No red → green result was obtained; no build, MSTest or Python replay results are available. No history was downloaded or modified. No other part of the master TZ was started.
- Environment limitation: the linked GitHub source is accessible through the connector, but this sandbox has neither `dotnet` nor the StockSharp project/reference checkout and packaged BTC/TON history. Reproducing a full test run here is therefore not possible.
- Workspace rules `AGENTS.md` and `configs/instructions.md` were not present in the published main checkout accessible through GitHub; verify them against `D:\stocksharp` before integrating these edits.

## Commands to run (not executed)

Run **serially**, one MSBuild/MSTest worker. Every invocation has a strategy-specific filter and an overall test-session timeout. Execute with the relative `StockSharp` ProjectReference checkout and the packaged BTC/TON history in place.

```powershell
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~CSharpTests.S0333_KeltnerSeasonalFilter|FullyQualifiedName~CSharpTests.Point1Part01_0333" -- RunConfiguration.TestSessionTimeout=600000
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~PythonTests.Point1Part01_0333" -- RunConfiguration.TestSessionTimeout=600000
```

After the above, add independent **fully observed execution** checks for positive and seasonally forbidden breakouts using the original indicator lengths/multiplier, both entry sides, stop exits, and filled positions. March-only history cannot establish the forbidden-month scenario without a separate controlled candle-time fixture; maintain red-before-fix proof for any discovered defect. Do not replace default checks with a relaxed override or switch off `requireTrades`. Record command output, exact failures and SHA before marking any key tested.

**Shared files:** created `Tests/StrategyTests.Point1Part01.cs` only; `Tests/StrategyInventory.cs`, `Tests.csproj`, and shared helpers were not edited.

## Additional changes in 0103, 0121 and 0122 (not yet validated)

- Test-first source commit `0b3a79c`: shared filtered MSTest methods `Point1Part01_0103_DarkPoolPublishedStopExecutes`, `Point1Part01_0121_SantaClausPublishedStopExecutes`, `Point1Part01_0122_JanuaryEffectPublishedStopExecutes` use the existing `CheckPercentStopBetweenBars` helper: enforce 2% default parameter, 0.00001% real stop, real entry, real stop fill before next finished signal candle, and exact close volume. Source-level expectation was red because none of the strategies previously had `StopLossPercent` or `StartProtection`; **no actual red test run exists**.
- Production commit `de543188`: `StopLossPercent` added in all six implementations; `StartProtection(0 absolute take-profit, configured percent stop-loss)` installed on start. Dark-pool SMA exits are checked even on quiet-volume and cooldown bars.
- Production commit `4429da19`: calendar strategies' C#/Python close orders now use the actual signed position size instead of a default trade volume.
- 21 localized READMEs (7 each) explain specific implemented gates, stop semantics and actual one-/five-minute timeframes, and explicitly distinguish the monthly calendar generalizations from true year-end seasonality. Published annual returns remain unverified.
- Filter commands (run **serially**, one worker, with a timeout and existing BTC/TON archive):

```powershell
$env:MSBUILDDISABLENODEREUSE = '1'
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~CSharpTests.Point1Part01_0103_DarkPoolPublishedStopExecutes|FullyQualifiedName~CSharpTests.Point1Part01_0121_SantaClausPublishedStopExecutes|FullyQualifiedName~CSharpTests.Point1Part01_0122_JanuaryEffectPublishedStopExecutes' -- RunConfiguration.TestSessionTimeout=900000
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~PythonTests.Point1Part01_0103_DarkPoolPublishedStopExecutes|FullyQualifiedName~PythonTests.Point1Part01_0121_SantaClausPublishedStopExecutes|FullyQualifiedName~PythonTests.Point1Part01_0122_JanuaryEffectPublishedStopExecutes' -- RunConfiguration.TestSessionTimeout=900000
```

Neither command has been executed in this environment (no dotnet SDK, sibling StockSharp projects or packaged archive). Treat all three as **incomplete** until the above checks and independent signal oracle are green.

## Additional seasonal-momentum test

- Method `Point1Part01_0303_PublishedMarchSeasonalityForbidsNewShorts` is authored in `Tests/StrategyTests.Point1Part01.cs` for both languages, asserting unchanged parameters, no short entries during the March-only fixture, and actual buy order registration/fill.
- The exact tested calendar is March 2024 (score +0.5); negative seasonality months and annual performance cannot be inferred from this fixture. This is an *unexecuted* authored test, not a passing check.

```powershell
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~CSharpTests.Point1Part01_0303_PublishedMarchSeasonalityForbidsNewShorts' -- RunConfiguration.TestSessionTimeout=600000
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~PythonTests.Point1Part01_0303_PublishedMarchSeasonalityForbidsNewShorts' -- RunConfiguration.TestSessionTimeout=600000
```

## Additional authored checks for 0333 and 0305

- `Point1Part01_0333_OriginalSeasonalBreakoutConditions`: independent deterministic table of strict seasonal and EMA±2ATR breakout gates including forbidden months, exact band equality, held positions and reversals. Both C#/Python production sources call the same **mathematical rule independently reimplemented in each language**; the C# table has not actually been run. The prior test methods for 0333 remain.
- `Point1Part01_0305_DynamicRsiUsesActualPercentStop`: both implementations' default RSI(14), MovingAvgPeriod(50), StdDevMultiplier(2), cooldown(48), candle(5m) checked, then existing `CheckPercentStopBetweenBars` requested against 31 days of original BTC/TON to test actual tight protective fills. The test has not been run. Seven localized READMEs corrected from misleading ATR-stop prose to actual native 2% stop and executable RSI formulas.

```powershell
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~CSharpTests.Point1Part01_0333_OriginalSeasonalBreakoutConditions|FullyQualifiedName~CSharpTests.Point1Part01_0305_DynamicRsiUsesActualPercentStop' -- RunConfiguration.TestSessionTimeout=900000
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~PythonTests.Point1Part01_0333_OriginalSeasonalBreakoutConditions|FullyQualifiedName~PythonTests.Point1Part01_0305_DynamicRsiUsesActualPercentStop' -- RunConfiguration.TestSessionTimeout=900000
```

The 0333 static C# signal test is authored in the shared base test class, so both CSharpTests and PythonTests run its compiled C# oracle, whereas the source replay methods exercise their respective implementation. None of the above commands has been executed here.

## Adaptive MACD (0307) and Bollinger breakout (0306) findings

- Both C#/Python 0307 sources have been reviewed statically; default MACD/Histogram parameters match. Method `Point1Part01_0307_AdaptiveMacdDefaultAndNativeStop` is authored for both languages in shard 03, checking unchanged defaults and actual intrabar tight-stop fills on the 31-day fixture; **not executed**.
- Seven 0307 README translations now specify exact two-sided entry/exit conditions and native percent stop, rather than claiming ATR multiples.
- Static review of 0306 confirms ATR x multiplier manual stop logic coexists with a native `UnitTypes.Percent` stop using the very same multiplier. It remains unresolved pending an ATR-frozen protective implementation and independent order/fill tests.

```powershell
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~CSharpTests.Point1Part01_0307_AdaptiveMacdDefaultAndNativeStop' -- RunConfiguration.TestSessionTimeout=900000
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~PythonTests.Point1Part01_0307_AdaptiveMacdDefaultAndNativeStop' -- RunConfiguration.TestSessionTimeout=900000
```

## Per-key coverage

| Key | Task kind | Status and specific outstanding reason |
|---|---|---|
| `0333_Keltner_Seasonal_Filter` | конкретная задача | Частично: C#/Python функции сигнала выделены без изменения порогов, добавлен независимый табличный oracle для разрешённых/запрещённых месяцев и граничных значений, семь README обновлены, месячный Python startup-пробник подготовлен. **Все тесты без запуска**, подтверждения C#/Python replay и исполнения реальных стопов нет; BTC/TON содержит только март. |
| `0103_Dark_Pool_Prints` | проверка замечаний | Код C#/Python исправлен: добавлен параметр `StopLossPercent=2%` и нативная защита; Также устранено блокирование выхода SMA фильтром объёма/кулдауном. Семь README приведены к исполняемым формулам/defaults. Тест реальных intrabar-исполнений добавлен, но **не запускался**. Не подтверждены C#/Python red→green, объёмы фактических исполнений и выполнение контракта на BTC/TON. |
| `0121_Santa_Claus_Rally` | проверка замечаний | Код C#/Python исправлен: добавлен параметр `StopLossPercent=2%` и нативная защита; Выход теперь использует фактический остаток позиции. Семь README приведены к исполняемым формулам/defaults. Тест реальных intrabar-исполнений добавлен, но **не запускался**. Не подтверждены C#/Python red→green, объёмы фактических исполнений и выполнение контракта на BTC/TON. |
| `0122_January_Effect` | проверка замечаний | Код C#/Python исправлен: добавлен параметр `StopLossPercent=2%` и нативная защита; Выход теперь использует фактический остаток позиции. Семь README приведены к исполняемым формулам/defaults. Тест реальных intrabar-исполнений добавлен, но **не запускался**. Не подтверждены C#/Python red→green, объёмы фактических исполнений и выполнение контракта на BTC/TON. |
| `0299_Beta_Adjusted_Pairs_Trading` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0321_Ichimoku_Hurst_Exponent` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0341_Supertrend_Put_Call_Ratio` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0344_Hull_MA_Implied_Volatility_Breakout` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0351_Accrual_Anomaly` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0352_Asset_Class_Trend_Following` | проверка замечаний | Статически выявлено: обе реализации имеют SmaLength=36 и 4-часовые свечи, README требует SMA210 на дневных свечах и MinTradeUsd=50; MinTradeUsd отсутствует в коде, а реализованный двухинструментный относительный фильтр отличается от описанного ежемесячного равного распределения. Не исправлено: требуется выбор верного контракта и независимые C#/Python тесты входов, выходов и перебалансировки. |
| `0353_Asset_Growth_Effect` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0354_Betting_Against_Beta_Stocks` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0355_Betting_Against_Beta` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0357_Book_To_Market_Value` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0358_Commodity_Momentum` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0359_Consistent_Momentum` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0366_Dollar_Carry_Trade` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0370_Earnings_Quality_Factor` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0371_ESGFactor_Momentum` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0372_Fed_Model` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0373_FScore_Reversal` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0374_FXCarry_Trade` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0376_Lexical_Density_Filings` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0377_Low_Volatility_Stocks` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0378_Momentum_Asset_Growth` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0379_Momentum_Factor_Stocks` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0380_Momentum_Rev_Vol` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0391_Residual_Momentum_Factor` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0392_Return_Asymmetry_Commodity` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0393_ROAEffect_Stocks` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0468_Two_X_Spy_Tips` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0483_Five_Ema_NoTouch_Breakout` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0485_Ema50_Crossover_Monthly_Dca` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0487_Sma_Pullback_Atr_Exits` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0502_Fib_Hurst_Breakout` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0512_Williams_Alligator_ATR` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0522_Anomalous_Holonomy_Field_Theory` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0551_BB_RSI_Trailing_Stop` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0554_BB_Heikin_Ashi_Entry` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0555_BB_HeikinAshi_Entry` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0556_Bbsr_Extreme` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0557_Bbtrend_Supertrend_Decision` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0558_Bearish_Wick_Reversal` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0565_Bias_Sentiment_Strength` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0565_Btc_Dca_Ahr999` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0566_Btc_Difficulty_Adjustments` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0568_Big_Mover_Catcher` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0568_Btc_Outperform` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0570_Binomial_Option_Pricing_Model` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0571_Bitcoin_1H15M_Breakout` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0571_BTCUSD_Momentum_After_Abnormal_Days` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0573_Bitcoin_CME_Spot_Spread` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0575_Bitcoin_Futures_Spot_TriFrame` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0575_BullBear_Volume_Percentile_TP` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0576_Bitcoin_Leverage_Sentiment` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0577_Bullish_Divergence_Short_Term_Long_Trade_Finder` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0578_Bitcoin_Momentum` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0580_Bjorgum_Double_Tap` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0580_Buy_Sell_Renko_Based` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0581_Black_Scholes_Delta_Hedge` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0582_Black_Scholes_Option_Pricing` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0583_Bober_XM` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0585_Bollinger_Ema_Stats` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0586_Bollinger_Stochastic_Trailing_Stop` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0586_Buying_Selling_Volume` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0587_Bollinger_Band_Touch_SMI_MACD_Angle` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0587_Buy_The_Dips_Trend` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0588_Buy_Only_EMA_BB` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0589_Bollinger_Bands_RSI` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0590_Bollinger_Bands_Enhanced` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0592_Bollinger_Bands_Mean_Reversion` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0594_Bollinger_Bands_SMA_20_2` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0597_Bollinger_Breakout_Direction` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0599_DCA_Dual_Trailing` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0600_CANX_MA_Crossover` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0606_CCI_EMA_ATR_TP_SL` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0607_CCI_MACD` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0608_CE_XAU_USDT` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0614_Chandelier_Exit_With_200_EMA_Filter` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0617_ChartPatterns` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0619_Classic_Nacked_Z-Score_Arbitrage` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0620_Cleaner_Screeners_Library` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0621_CME_Equity_Futures_Price_Limits` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0622_Cnagda_Fixed_Swing` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0623_Color_Code_Overlay` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0624_Color_Gradient_Framework` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0627_Combo_2_20_EMA_CCI` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0629_Commitment_of_Trader_R` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0631_Consecutive_Bars_Above_Below_EMA_Buy_The_Dip` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0634_Correlation_Cycle` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0635_Correlation_Matrix_Heatmap` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0636_Correlation_Arrays` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0638_Covid_Statistics_Tracker` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0642_Crypto_MVRV_ZScore` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0644_Crypto_Volatility_Bitcoin_Correlation` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0652_D-BoT_Alpha_Short_SMA_and_RSI` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0653_DBot_Alpha_RSI_Breakout` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0654_Daily_Bollinger_Band` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0655_Daily_Breakout_Daily_Shadow` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0656_Daily_Performance_Analysis` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0657_Daily_Play_Ace_Spectrum` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0659_DataChart` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0660_Day_Trading_Risk_Management` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0661_Daytrading_ES_Wick_Length` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0662_DCA_Simulation_for_CryptoCommunity` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0663_DCA_2` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0664_DCA_with_Hedging` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0665_DCA_Mean_Reversion_Bollinger_Band` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0666_DCA` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0668_DebugConsole` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0674_Dema_RSI` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0675_Demo_GPT_Day_Trading_Scalping` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0676_Directional_Index_K_xDMI` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0678_Divergence_For_Many_Indicators` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0679_Divergence_for_Many_Indicators_v4` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0680_Divergence_Indicator_Any_Oscillator` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0684_Dominance_Tagcloud` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0687_Donchian_WMA_Crossover` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0688_Donchian_Zig_Zag_LuxAlgo` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0691_Double_AI_Super_Trend_Trading` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0696_Double_Vegas_SuperTrend_Enhanced` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0698_Dskyz_DAFE_Adaptive_Regime_Quant_Machine_Pro` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0699_Dskyz_AI_Adaptive_Regime_Beginners` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0700_Aurora_Divergence` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0701_Dskyz_DAFE_GENESIS` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0702_Dskyz_DAFE_MAtrix_ATR_Precision` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0704_Dskyz_Adaptive_Futures_Elite` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0708_Dual_Momentum` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0710_Dual_Selector_V2_Cryptogyani` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0711_Dual_SuperTrend_VIX_Filter` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0712_Dual_Phase_Trend_Regime` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0714_Dubic_Ema` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0715_Dynamic_Breakout_Master` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0716_Dynamic_Dots_Dashboard` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0719_Dynamic_Volatility_Differential_Model` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0720_E9_Shark_32_Pattern` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0721_Earnings_Trading` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0722_Earnings_Splits_Dividends` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0723_Economic_Policy_Uncertainty` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0724_Efficient_Work` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0725_Ehlers_Combo` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0729_EMA_MA_Crossover` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0732_EMA_10_55_200_Long_Only_MTF` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0735_EMA_Dow_Theory` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0736_Ema_Cross_Macd_Session_Start` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0737_EMA_Crossover_Short_Focus_Trailing_Stop` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0738_Ema_Crossover_Filters` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0739_EMA_Crossover_Take_Profit` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0743_Ema_Grid_Martingale_Cooldown` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0746_EMA_Scoring` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0747_EMA_Shift_Parallel` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0750_Energy_Advanced_Policy` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0751_Engulfing_Pin_Bar_Breakout` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0757_Enhanced_Ichimoku_Cloud` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0758_Enhanced_Market_Structure` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0759_Enhanced_Range_Filter_ATR_TP_SL` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0761_Entry_Fragger` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0763_Equivolume_Bars` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0764_Equivolume_Overlay_Volume_Bars` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0768_ETH_USDT_EMA_Crossover` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0769_Rsi_Cci_WilliamsR` | проверка замечаний | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0088_Supertrend_Reversal` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0256_Bollinger_Band_Width_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0259_Ichimoku_Cloud_Width_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0260_Supertrend_Distance_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0261_Parabolic_SAR_Distance_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0263_MA_Slope_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0264_EMA_Slope_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0265_Volatility_Adjusted_Momentum` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0266_VWAP_Slope_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0267_RSI_Slope_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0268_Stochastic_Slope_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0269_CCI_Slope_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0270_Williams_R_Slope_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0271_MACD_Slope_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0272_ADX_Slope_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0278_Donchian_Width_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0279_Ichimoku_Cloud_Width_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0280_Supertrend_Distance_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0281_Parabolic_SAR_Distance_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0283_MA_Slope_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0284_EMA_Slope_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0285_VWAP_Slope_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0286_RSI_Slope_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0288_CCI_Slope_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0289_Williams_R_Slope_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0290_MACD_Slope_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0296_Z-Score_Volume_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0298_Correlation_Mean_Reversion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0302_Volatility_Cluster_Breakout` | проверка контракта | Статически выявлено в C#/Python: StopMultiplier используется как множитель ATR для ручного выхода на закрытии, но в StartProtection тот же параметр передаётся в UnitTypes.Percent; это не эквивалентные уровни защиты. Не исправлено: нужна тестируемая схема фиксации ATR при входе и действительной intrabar-защиты; проверки исполнений не было. |
| `0303_Seasonality_Adjusted_Momentum` | проверка контракта | Оба исходника статически сверены: M=Momentum14, SMA14(M), сезонный коэффициент candle.OpenTime.Month, закрытия по смещению/отмене разрешённой сезонности, нативный стоп 2%. Все семь README уточнены; добавлен реальный replay-тест мартовского запрета новых шортов, но **C#/Python тесты не запускались**; отрицательные месяцы и стоп-филлы не подтверждены. |
| `0305_RSI_Dynamic_Overbought_Oversold` | проверка контракта | Сверены C#/Python исходники: RSI14/SMA50 по RSI, ограниченные динамические границы, фильтр цены относительно SMA50, cooldown 48 и нативный процентный стоп 2%; семь README исправлены. Добавлен тест реальных входов и intrabar stop fills для обеих языковых версий, **не запускался**. Не подтверждён independent oracle фактических сигналов и позитивный C#/Python replay. |
| `0306_Bollinger_Volatility_Breakout` | проверка контракта | Статически обнаружено: StopLossMultiplier используется как множитель ATR для ручного выхода, но native StartProtection интерпретирует то же число как процент. Это две разные защиты; обе языковые реализации совпадают, но исходному заявлению об ATR-стопе противоречат. Не исправлено и не проверено фактическими исполнениями. |
| `0307_MACD_Adaptive_Histogram` | проверка контракта | Сверены C#/Python: MACD12/26/9 histogram, SMA20 и StdDev20 от гистограммы, вход по ±2σ, выход по среднему, cooldown16, стоп 2% и 15-минутные свечи. Семь README исправлены, общий тест default и реального стоп-исполнения добавлен для обоих языков, но **не запускался**; отдельный независимый signal oracle ещё нужен. |
| `0308_Ichimoku_Volume_Cluster` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0309_Supertrend_Momentum_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0310_Donchian_Volatility_Contraction` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0311_Keltner_RSI_Divergence` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0312_Hull_MA_Volume_Spike` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0313_VWAP_ADX_Trend_Strength` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0314_Parabolic_SAR_Volatility_Expansion` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0317_CCI_Volatility_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0318_Williams_R_Momentum` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0319_Bollinger_K-Means_Cluster` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0322_Supertrend_RSI_Divergence` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0323_Donchian_Seasonal_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0324_Keltner_Kalman_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0325_Hull_MA_Volatility_Contraction` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0326_VWAP_Stochastic_Divergence` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0327_Parabolic_SAR_Hurst_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0328_Bollinger_Kalman_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0329_MACD_Volume_Cluster` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0330_Ichimoku_Volatility_Contraction` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0332_Donchian_Hurst_Exponent` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0334_Hull_MA_K-Means_Cluster` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0335_VWAP_Hidden_Markov_Model` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0336_Parabolic_SAR_RSI_Divergence` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0337_Adaptive_RSI_Volume_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0338_Adaptive_Bollinger_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0339_MACD_Sentiment_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0340_Ichimoku_Implied_Volatility` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0342_Donchian_Sentiment_Spike` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0345_VWAP_Behavioral_Bias_Filter` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0346_Parabolic_SAR_Sentiment_Divergence` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0347_RSI_Option_Open_Interest` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0348_Stochastic_Implied_Volatility_Skew` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0349_ADX_Sentiment_Momentum` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0350_CCI_Put_Call_Ratio_Divergence` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0356_Bitcoin_Intraday_Seasonality` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0360_Country_Value_Factor` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0361_Crude_Oil_Predicts_Equity` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0363_Currency_Momentum_Factor` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0364_Currency_PPPValue` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0367_Earnings_Announcement_Premium` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0368_Earnings_Announcement_Reversal` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0369_Earnings_Announcements_With_Buybacks` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0375_January_Barometer` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0381_Momentum_Style_Rotation` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0382_Month12Cycle` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0383_Mutual_Fund_Momentum` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0384_Option_Expiration_Week` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0385_Overnight_Sentiment_Anomaly` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0387_Pairs_Trading_Country_ETFs` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0388_Pairs_Trading_Stocks` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0389_Payday_Anomaly` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0390_RDExpenditures` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0394_Sector_Momentum_Rotation` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0395_Short_Interest_Effect` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0396_Short_Term_Reversal_Futures` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0397_Short_Term_Reversal_Stocks` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0398_Skewness_Commodity` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0399_Small_Cap_Premium` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0400_Smart_Factors_Momentum_Market` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0403_Term_Structure_Commodities` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0404_Time_Series_Momentum` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0405_Trend_Following_Stocks` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0406_Turn_Of_Month` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0409_Weeks52High` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0411_Asset_Class_Momentum_Rotational` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0412_Bollinger_Aroon` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0414_Bollinger_Winner_Lite` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0422_Exceeded_Candle` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0426_Ha_Universal` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0428_Improvisando` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0442_Rsi_Ema` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0444_Stoch_Rsi_Supertrend` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0445_ADX_Tester` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0446_Strat_Base` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0447_Supertrend_Ema_Rebound` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0450_Tendency_Ema_Rsi` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0456_Squeeze_Pro_Overlays` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0457_Averaging_Down` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0458_Smc_Bb_Breakout` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0461_Ema_Macd_Rsi` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0465_200_SMA_Buffer` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0466_MA_BB_Supertrend` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0467_2Mars_Okx` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0469_Three_Down_Three_Up` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0470_Three_Red_Green_Volatility` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0473_Rsi_30_70` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0474_30_Minute_Candle` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0479_3x_Supertrend` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0488_80_20` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0489_Beta_Weighted_MA` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0493_Adaptive_Rsi` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0497_Adjustable_Ma_Alternating_Extremities` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0499_Advanced_Ema_Cross` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0501_Advanced_Multi_Seasonality` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0503_Advanced_Position_Management` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0509_Aftershock_Playbook_Stock_Earnings_Drift_Engine` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0515_AI_Volume` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0517_Alligator_Ma_Trend_Catcher` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0519_ALMA_Optimized` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0520_Altcoin_Index_Correlation` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0521_Anands` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0524_Ao_Ac_Trading_Zones` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0527_Arpeet_Macd` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0528_Arpit_Bollinger_Band` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0530_ATR_Based_Trendlines` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0535_Auto_Fibo_Gann_Fan_Retracements` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0536_State_Aware_MA_Cross` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0545_Backward_Number_of_Bars` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0547_Bar_Balance` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0549_Baseline2` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0550_Bayesian_Bbsma_Oscillator` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0552_BB_RSI` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0553_BB_Breakout_Momentum_Squeeze` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0559_Bedo_Osaimi_Istr` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0562_Berlin_Candles` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0564_Best_Dollar_Cost_Average` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0564_Btc_Chop_Reversal` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0566_Bias_Ratio` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0570_Btc_Trading_Robot` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0574_Bitcoin_Exponential_Profit` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
| `0583_ZScore_Buy_Sell` | проверка контракта | Не проверено: требуется независимая проверка контракта и реальных исполнений C#/Python по назначенному замечанию. |
