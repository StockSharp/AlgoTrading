using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Money management mode for Plateau strategy.
/// </summary>
public enum PlateauMoneyManagementMode
{
\t/// <summary>Use fixed lot size for every order.</summary>
\tFixedLot,

\t/// <summary>Risk a percentage of the account per trade.</summary>
\tRiskPercent,
}

/// <summary>
/// Price source used for moving averages and Bollinger Bands.
/// Matches the input price options from the original MQL5 expert.
/// </summary>
public enum PlateauAppliedPrice
{
\t/// <summary>Close price of the candle.</summary>
\tClose,

\t/// <summary>Open price of the candle.</summary>
\tOpen,

\t/// <summary>High price of the candle.</summary>
\tHigh,

\t/// <summary>Low price of the candle.</summary>
\tLow,

\t/// <summary>Median price (high + low) / 2.</summary>
\tMedian,

\t/// <summary>Typical price (high + low + close) / 3.</summary>
\tTypical,

\t/// <summary>Weighted price (high + low + close + close) / 4.</summary>
\tWeighted,
}

/// <summary>
/// Moving average method equivalent to the MQL5 implementation.
/// </summary>
public enum PlateauMovingAverageMethod
{
\t/// <summary>Simple moving average.</summary>
\tSimple,

\t/// <summary>Exponential moving average.</summary>
\tExponential,

\t/// <summary>Smoothed moving average.</summary>
\tSmoothed,

\t/// <summary>Linear weighted moving average.</summary>
\tLinearWeighted,
}

/// <summary>
/// Converted version of the Plateau expert advisor.
/// The strategy monitors fast and slow moving averages together with the lower Bollinger Band.
/// When a bullish crossover occurs below the lower band a long position is opened, while a bearish crossover above the lower band triggers a short entry.
/// Optional stop loss, take profit and trailing stop are applied in pips, and position sizing can rely on fixed lots or risk percentage.
/// </summary>
public class PlateauStrategy : Strategy
{
\tprivate const int HistoryCapacity = 16;

\tprivate readonly StrategyParam<decimal> _stopLossPips;
\tprivate readonly StrategyParam<decimal> _takeProfitPips;
\tprivate readonly StrategyParam<decimal> _trailingStopPips;
\tprivate readonly StrategyParam<decimal> _trailingStepPips;
\tprivate readonly StrategyParam<PlateauMoneyManagementMode> _moneyMode;
\tprivate readonly StrategyParam<decimal> _moneyValue;
\tprivate readonly StrategyParam<int> _fastMaPeriod;
\tprivate readonly StrategyParam<int> _slowMaPeriod;
\tprivate readonly StrategyParam<int> _maShift;
\tprivate readonly StrategyParam<PlateauMovingAverageMethod> _maMethod;
\tprivate readonly StrategyParam<PlateauAppliedPrice> _maAppliedPrice;
\tprivate readonly StrategyParam<int> _bandsPeriod;
\tprivate readonly StrategyParam<int> _bandsShift;
\tprivate readonly StrategyParam<decimal> _bandsDeviation;
\tprivate readonly StrategyParam<PlateauAppliedPrice> _bandsAppliedPrice;
\tprivate readonly StrategyParam<bool> _reverseSignals;
\tprivate readonly StrategyParam<bool> _closeOpposite;
\tprivate readonly StrategyParam<bool> _printLog;
\tprivate readonly StrategyParam<DataType> _candleType;

\tprivate IIndicator _fastMa = null!;
\tprivate IIndicator _slowMa = null!;
\tprivate BollingerBands _bollinger = null!;

\tprivate readonly List<decimal> _fastHistory = new();
\tprivate readonly List<decimal> _slowHistory = new();
\tprivate readonly List<decimal> _lowerBandHistory = new();
\tprivate readonly List<decimal> _closeHistory = new();

\tprivate decimal _pipSize;
\tprivate decimal _stopLossOffset;
\tprivate decimal _takeProfitOffset;
\tprivate decimal _trailingStopOffset;
\tprivate decimal _trailingStepOffset;

\tprivate decimal? _entryPrice;
\tprivate decimal? _activeStopPrice;
\tprivate decimal? _activeTakePrice;

\t/// <summary>
\t/// Stop loss distance expressed in pips.
\t/// </summary>
\tpublic decimal StopLossPips
\t{
\t\tget => _stopLossPips.Value;
\t\tset => _stopLossPips.Value = value;
\t}

\t/// <summary>
\t/// Take profit distance expressed in pips.
\t/// </summary>
\tpublic decimal TakeProfitPips
\t{
\t\tget => _takeProfitPips.Value;
\t\tset => _takeProfitPips.Value = value;
\t}

\t/// <summary>
\t/// Trailing stop distance in pips.
\t/// </summary>
\tpublic decimal TrailingStopPips
\t{
\t\tget => _trailingStopPips.Value;
\t\tset => _trailingStopPips.Value = value;
\t}

\t/// <summary>
\t/// Minimal trailing step in pips.
\t/// </summary>
\tpublic decimal TrailingStepPips
\t{
\t\tget => _trailingStepPips.Value;
\t\tset => _trailingStepPips.Value = value;
\t}

\t/// <summary>
\t/// Money management mode (fixed lot or risk percentage).
\t/// </summary>
\tpublic PlateauMoneyManagementMode MoneyMode
\t{
\t\tget => _moneyMode.Value;
\t\tset => _moneyMode.Value = value;
\t}

\t/// <summary>
\t/// Fixed lot size or risk percentage depending on <see cref="MoneyMode"/>.
\t/// </summary>
\tpublic decimal MoneyValue
\t{
\t\tget => _moneyValue.Value;
\t\tset => _moneyValue.Value = value;
\t}

\t/// <summary>
\t/// Fast moving average period.
\t/// </summary>
\tpublic int FastMaPeriod
\t{
\t\tget => _fastMaPeriod.Value;
\t\tset => _fastMaPeriod.Value = value;
\t}

\t/// <summary>
\t/// Slow moving average period.
\t/// </summary>
\tpublic int SlowMaPeriod
\t{
\t\tget => _slowMaPeriod.Value;
\t\tset => _slowMaPeriod.Value = value;
\t}

\t/// <summary>
\t/// Horizontal shift applied to both moving averages.
\t/// </summary>
\tpublic int MaShift
\t{
\t\tget => _maShift.Value;
\t\tset => _maShift.Value = value;
\t}

\t/// <summary>
\t/// Method used to calculate the moving averages.
\t/// </summary>
\tpublic PlateauMovingAverageMethod MaMethod
\t{
\t\tget => _maMethod.Value;
\t\tset => _maMethod.Value = value;
\t}

\t/// <summary>
\t/// Applied price for the moving averages.
\t/// </summary>
\tpublic PlateauAppliedPrice MaAppliedPrice
\t{
\t\tget => _maAppliedPrice.Value;
\t\tset => _maAppliedPrice.Value = value;
\t}

\t/// <summary>
\t/// Bollinger Bands averaging period.
\t/// </summary>
\tpublic int BandsPeriod
\t{
\t\tget => _bandsPeriod.Value;
\t\tset => _bandsPeriod.Value = value;
\t}

\t/// <summary>
\t/// Horizontal shift applied to Bollinger Bands values.
\t/// </summary>
\tpublic int BandsShift
\t{
\t\tget => _bandsShift.Value;
\t\tset => _bandsShift.Value = value;
\t}

\t/// <summary>
\t/// Bollinger Bands standard deviation multiplier.
\t/// </summary>
\tpublic decimal BandsDeviation
\t{
\t\tget => _bandsDeviation.Value;
\t\tset => _bandsDeviation.Value = value;
\t}

\t/// <summary>
\t/// Applied price used for Bollinger Bands calculations.
\t/// </summary>
\tpublic PlateauAppliedPrice BandsAppliedPrice
\t{
\t\tget => _bandsAppliedPrice.Value;
\t\tset => _bandsAppliedPrice.Value = value;
\t}

\t/// <summary>
\t/// Reverse signal logic flag.
\t/// </summary>
\tpublic bool ReverseSignals
\t{
\t\tget => _reverseSignals.Value;
\t\tset => _reverseSignals.Value = value;
\t}

\t/// <summary>
\t/// Close opposite positions before opening a new trade.
\t/// </summary>
\tpublic bool CloseOpposite
\t{
\t\tget => _closeOpposite.Value;
\t\tset => _closeOpposite.Value = value;
\t}

\t/// <summary>
\t/// Enable verbose logging similar to the original script.
\t/// </summary>
\tpublic bool PrintLog
\t{
\t\tget => _printLog.Value;
\t\tset => _printLog.Value = value;
\t}

\t/// <summary>
\t/// Candle type used by the strategy.
\t/// </summary>
\tpublic DataType CandleType
\t{
\t\tget => _candleType.Value;
\t\tset => _candleType.Value = value;
\t}

\t/// <summary>
\t/// Create Plateau strategy with default parameters matching the original expert.
\t/// </summary>
\tpublic PlateauStrategy()
\t{
\t\t_stopLossPips = Param(nameof(StopLossPips), 50m)
\t\t\t.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")
\t\t\t.SetCanOptimize(true);

\t\t_takeProfitPips = Param(nameof(TakeProfitPips), 140m)
\t\t\t.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")
\t\t\t.SetCanOptimize(true);

\t\t_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
\t\t\t.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
\t\t\t.SetCanOptimize(true);

\t\t_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
\t\t\t.SetDisplay("Trailing Step", "Minimal trailing step in pips", "Risk")
\t\t\t.SetCanOptimize(true);

\t\t_moneyMode = Param(nameof(MoneyMode), PlateauMoneyManagementMode.RiskPercent)
\t\t\t.SetDisplay("Money Mode", "Choose between fixed lot or risk percent", "Money Management");

\t\t_moneyValue = Param(nameof(MoneyValue), 3m)
\t\t\t.SetDisplay("Lot / Risk", "Fixed lot when Money Mode=FixedLot or risk percent when Money Mode=RiskPercent", "Money Management")
\t\t\t.SetCanOptimize(true);

\t\t_fastMaPeriod = Param(nameof(FastMaPeriod), 9)
\t\t\t.SetDisplay("Fast MA", "Fast moving average period", "Indicators")
\t\t\t.SetCanOptimize(true);

\t\t_slowMaPeriod = Param(nameof(SlowMaPeriod), 24)
\t\t\t.SetDisplay("Slow MA", "Slow moving average period", "Indicators")
\t\t\t.SetCanOptimize(true);

\t\t_maShift = Param(nameof(MaShift), 0)
\t\t\t.SetDisplay("MA Shift", "Horizontal shift applied to moving averages", "Indicators");

\t\t_maMethod = Param(nameof(MaMethod), PlateauMovingAverageMethod.LinearWeighted)
\t\t\t.SetDisplay("MA Method", "Moving average smoothing method", "Indicators");

\t\t_maAppliedPrice = Param(nameof(MaAppliedPrice), PlateauAppliedPrice.Typical)
\t\t\t.SetDisplay("MA Price", "Applied price for moving averages", "Indicators");

\t\t_bandsPeriod = Param(nameof(BandsPeriod), 150)
\t\t\t.SetDisplay("Bands Period", "Bollinger Bands averaging period", "Indicators")
\t\t\t.SetCanOptimize(true);

\t\t_bandsShift = Param(nameof(BandsShift), 0)
\t\t\t.SetDisplay("Bands Shift", "Horizontal shift applied to Bollinger Bands", "Indicators");

\t\t_bandsDeviation = Param(nameof(BandsDeviation), 1m)
\t\t\t.SetDisplay("Bands Deviation", "Bollinger Bands deviation multiplier", "Indicators")
\t\t\t.SetCanOptimize(true);

\t\t_bandsAppliedPrice = Param(nameof(BandsAppliedPrice), PlateauAppliedPrice.Typical)
\t\t\t.SetDisplay("Bands Price", "Applied price for Bollinger Bands", "Indicators");

\t\t_reverseSignals = Param(nameof(ReverseSignals), false)
\t\t\t.SetDisplay("Reverse", "Invert trading signals", "General");

\t\t_closeOpposite = Param(nameof(CloseOpposite), false)
\t\t\t.SetDisplay("Close Opposite", "Close opposite exposure before opening a new trade", "General");

\t\t_printLog = Param(nameof(PrintLog), false)
\t\t\t.SetDisplay("Verbose Log", "Print diagnostic messages", "General");

\t\t_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
\t\t\t.SetDisplay("Candle Type", "Data series used for calculations", "General");
\t}

\t/// <inheritdoc />
\tpublic override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
\t{
\t\treturn [(Security, CandleType)];
\t}

\t/// <inheritdoc />
\tprotected override void OnReseted()
\t{
\t\tbase.OnReseted();

\t\t_fastHistory.Clear();
\t\t_slowHistory.Clear();
\t\t_lowerBandHistory.Clear();
\t\t_closeHistory.Clear();

\t\t_entryPrice = null;
\t\t_activeStopPrice = null;
\t\t_activeTakePrice = null;
\t}

\t/// <inheritdoc />
\tprotected override void OnStarted(DateTimeOffset time)
\t{
\t\tbase.OnStarted(time);

\t\tif (TrailingStopPips > 0m && TrailingStepPips <= 0m)
\t\t\tthrow new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

\t\t_fastMa = CreateMovingAverage(MaMethod, FastMaPeriod);
\t\t_slowMa = CreateMovingAverage(MaMethod, SlowMaPeriod);
\t\t_bollinger = new BollingerBands
\t\t{
\t\t\tLength = BandsPeriod,
\t\t\tWidth = BandsDeviation
\t\t};

\t\t_pipSize = CalculatePipSize();
\t\t_stopLossOffset = StopLossPips * _pipSize;
\t\t_takeProfitOffset = TakeProfitPips * _pipSize;
\t\t_trailingStopOffset = TrailingStopPips * _pipSize;
\t\t_trailingStepOffset = TrailingStepPips * _pipSize;

\t\tif (MoneyMode == PlateauMoneyManagementMode.FixedLot && MoneyValue > 0m)
\t\t\tVolume = MoneyValue;

\t\tStartProtection();

\t\tvar subscription = SubscribeCandles(CandleType);
\t\tsubscription
\t\t\t.Bind(ProcessCandle)
\t\t\t.Start();

\t\tvar area = CreateChartArea();
\t\tif (area != null)
\t\t{
\t\t\tDrawCandles(area, subscription);
\t\t\tif (_fastMa is Indicator fastIndicator)
\t\t\t\tDrawIndicator(area, fastIndicator);
\t\t\tif (_slowMa is Indicator slowIndicator)
\t\t\t\tDrawIndicator(area, slowIndicator);
\t\t\tDrawIndicator(area, _bollinger);
\t\t\tDrawOwnTrades(area);
\t\t}
\t}

\tprivate void ProcessCandle(ICandleMessage candle)
\t{
\t\tif (candle.State != CandleStates.Finished)
\t\t\treturn;

\t\t// Manage existing protection before looking for new opportunities.
\t\tManageActivePosition(candle);

\t\tvar maInput = GetAppliedPrice(candle, MaAppliedPrice);
\t\tvar fastValue = _fastMa.Process(new DecimalIndicatorValue(_fastMa, maInput, candle.OpenTime)).ToNullableDecimal();
\t\tvar slowValue = _slowMa.Process(new DecimalIndicatorValue(_slowMa, maInput, candle.OpenTime)).ToNullableDecimal();

\t\tvar bandsInput = GetAppliedPrice(candle, BandsAppliedPrice);
\t\tvar bandValue = (BollingerBandsValue)_bollinger.Process(new DecimalIndicatorValue(_bollinger, bandsInput, candle.OpenTime));

\t\tif (fastValue is not decimal fast || slowValue is not decimal slow)
\t\t{
\t\t\tUpdateHistory(fastValue, slowValue, bandValue.LowBand, candle.ClosePrice);
\t\t\treturn;
\t\t}

\t\tif (bandValue.LowBand is not decimal lowerBand)
\t\t{
\t\t\tUpdateHistory(fast, slow, bandValue.LowBand, candle.ClosePrice);
\t\t\treturn;
\t\t}

\t\tUpdateHistory(fast, slow, lowerBand, candle.ClosePrice);

\t\tif (!IsFormedAndOnlineAndAllowTrading())
\t\t\treturn;

\t\tvar (buySignal, sellSignal) = EvaluateSignals();

\t\tif (ReverseSignals)
\t\t{
\t\t\t(buySignal, sellSignal) = (sellSignal, buySignal);
\t\t}

\t\tif (buySignal)
\t\t\tTryEnterLong(candle.ClosePrice);
\t\telse if (sellSignal)
\t\t\tTryEnterShort(candle.ClosePrice);
\t}

\tprivate (bool buy, bool sell) EvaluateSignals()
\t{
\tvar fastPrev1 = GetHistoryValue(_fastHistory, 1 + MaShift);
\tvar fastPrev2 = GetHistoryValue(_fastHistory, 2 + MaShift);
\tvar slowPrev1 = GetHistoryValue(_slowHistory, 1 + MaShift);
\tvar slowPrev2 = GetHistoryValue(_slowHistory, 2 + MaShift);
\tvar lowerPrev1 = GetHistoryValue(_lowerBandHistory, 1 + BandsShift);
\tvar closePrev1 = GetHistoryValue(_closeHistory, 1);

\tif (fastPrev1 is null || fastPrev2 is null || slowPrev1 is null || slowPrev2 is null || lowerPrev1 is null || closePrev1 is null)
\treturn (false, false);

\tvar buySignal = fastPrev2 < slowPrev2 && fastPrev1 > slowPrev1 && closePrev1 < lowerPrev1;
\tvar sellSignal = fastPrev2 > slowPrev2 && fastPrev1 < slowPrev1 && closePrev1 > lowerPrev1;

\treturn (buySignal, sellSignal);
\t}

\tprivate void TryEnterLong(decimal entryPrice)
\t{
\tif (Position > 0m)
\treturn;

\tvar volume = CalculateOrderVolume();
\tif (volume <= 0m)
\treturn;

\tif (CloseOpposite && Position < 0m)
\tBuyMarket(-Position);

\tBuyMarket(volume);
\tSetProtectionLevels(entryPrice, true);
\t}

\tprivate void TryEnterShort(decimal entryPrice)
\t{
\tif (Position < 0m)
\treturn;

\tvar volume = CalculateOrderVolume();
\tif (volume <= 0m)
\treturn;

\tif (CloseOpposite && Position > 0m)
\tSellMarket(Position);

\tSellMarket(volume);
\tSetProtectionLevels(entryPrice, false);
\t}

\tprivate void ManageActivePosition(ICandleMessage candle)
\t{
\tif (Position > 0m)
\t{
\tif (_activeStopPrice is decimal stop && candle.LowPrice <= stop)
\t{
\tSellMarket(Position);
\tResetProtection();
\treturn;
\t}

\tif (_activeTakePrice is decimal take && candle.HighPrice >= take)
\t{
\tSellMarket(Position);
\tResetProtection();
\treturn;
\t}

\tUpdateTrailingForLong(candle.ClosePrice);
\t}
\telse if (Position < 0m)
\t{
\tvar volume = Math.Abs(Position);

\tif (_activeStopPrice is decimal stop && candle.HighPrice >= stop)
\t{
\tBuyMarket(volume);
\tResetProtection();
\treturn;
\t}

\tif (_activeTakePrice is decimal take && candle.LowPrice <= take)
\t{
\tBuyMarket(volume);
\tResetProtection();
\treturn;
\t}

\tUpdateTrailingForShort(candle.ClosePrice);
\t}
\telse
\t{
\tResetProtection();
\t}
\t}

\tprivate void UpdateTrailingForLong(decimal price)
\t{
\tif (_entryPrice is not decimal entry || _trailingStopOffset <= 0m || _trailingStepOffset <= 0m)
\treturn;

\tvar distance = price - entry;
\tif (distance <= _trailingStopOffset + _trailingStepOffset)
\treturn;

\tvar newStop = price - _trailingStopOffset;
\tif (_activeStopPrice is decimal currentStop && newStop - currentStop < _trailingStepOffset)
\treturn;

\t_activeStopPrice = newStop;

\tif (PrintLog)
\tLogInfo($"Trailing long stop adjusted to {newStop:0.#####}");
\t}

\tprivate void UpdateTrailingForShort(decimal price)
\t{
\tif (_entryPrice is not decimal entry || _trailingStopOffset <= 0m || _trailingStepOffset <= 0m)
\treturn;

\tvar distance = entry - price;
\tif (distance <= _trailingStopOffset + _trailingStepOffset)
\treturn;

\tvar newStop = price + _trailingStopOffset;
\tif (_activeStopPrice is decimal currentStop && currentStop - newStop < _trailingStepOffset)
\treturn;

\t_activeStopPrice = newStop;

\tif (PrintLog)
\tLogInfo($"Trailing short stop adjusted to {newStop:0.#####}");
\t}

\tprivate void SetProtectionLevels(decimal entryPrice, bool isLong)
\t{
\t_entryPrice = entryPrice;

\tif (isLong)
\t{
\t_activeStopPrice = _stopLossOffset > 0m ? entryPrice - _stopLossOffset : null;
\t_activeTakePrice = _takeProfitOffset > 0m ? entryPrice + _takeProfitOffset : null;
\t}
\telse
\t{
\t_activeStopPrice = _stopLossOffset > 0m ? entryPrice + _stopLossOffset : null;
\t_activeTakePrice = _takeProfitOffset > 0m ? entryPrice - _takeProfitOffset : null;
\t}

\tif (PrintLog)
\tLogInfo($"Entry at {entryPrice:0.#####}, stop={_activeStopPrice?.ToString("0.#####") ?? "n/a"}, take={_activeTakePrice?.ToString("0.#####") ?? "n/a"}");
\t}

\tprivate void ResetProtection()
\t{
\t_entryPrice = null;
\t_activeStopPrice = null;
\t_activeTakePrice = null;
\t}

\tprivate decimal CalculateOrderVolume()
\t{
\tif (MoneyMode == PlateauMoneyManagementMode.FixedLot)
\treturn MoneyValue;

\tif (MoneyMode != PlateauMoneyManagementMode.RiskPercent)
\treturn Volume;

\tif (MoneyValue <= 0m || _stopLossOffset <= 0m)
\treturn Volume;

\tvar portfolio = Portfolio;
\tif (portfolio is null)
\treturn Volume;

\tvar equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
\tif (equity <= 0m)
\treturn Volume;

\tvar priceStep = Security?.PriceStep ?? 0m;
\tvar stepPrice = Security?.StepPrice ?? 0m;

\tdecimal perUnitRisk;
\tif (priceStep > 0m && stepPrice > 0m)
\t{
\tperUnitRisk = _stopLossOffset / priceStep * stepPrice;
\t}
\telse
\t{
\tperUnitRisk = _stopLossOffset;
\t}

\tif (perUnitRisk <= 0m)
\treturn Volume;

\tvar riskAmount = equity * MoneyValue / 100m;
\tif (riskAmount <= 0m)
\treturn Volume;

\tvar rawVolume = riskAmount / perUnitRisk;
\tvar volumeStep = Security?.VolumeStep ?? 0m;

\tif (volumeStep > 0m)
\t{
\tvar steps = Math.Max(1m, Math.Floor(rawVolume / volumeStep));
\treturn steps * volumeStep;
\t}

\treturn Math.Max(rawVolume, 0m);
\t}

\tprivate void UpdateHistory(decimal? fast, decimal? slow, decimal? lowerBand, decimal closePrice)
\t{
\tvoid AddValue(List<decimal> list, decimal? value)
\t{
\tif (value is not decimal decimalValue)
\treturn;

\tlist.Insert(0, decimalValue);
\tif (list.Count > HistoryCapacity)
\tlist.RemoveAt(list.Count - 1);
\t}

\tAddValue(_fastHistory, fast);
\tAddValue(_slowHistory, slow);
\tAddValue(_lowerBandHistory, lowerBand);

\t_closeHistory.Insert(0, closePrice);
\tif (_closeHistory.Count > HistoryCapacity)
\t_closeHistory.RemoveAt(_closeHistory.Count - 1);
\t}

\tprivate static decimal? GetHistoryValue(List<decimal> list, int index)
\t{
\tif (index < 0 || index >= list.Count)
\treturn null;

\treturn list[index];
\t}

\tprivate decimal CalculatePipSize()
\t{
\tvar step = Security?.PriceStep ?? 0m;
\tif (step <= 0m)
\treturn 0.0001m;

\tvar decimals = GetDecimalPlaces(step);
\tif (decimals == 3 || decimals == 5)
\treturn step * 10m;

\treturn step;
\t}

\tprivate static int GetDecimalPlaces(decimal value)
\t{
\tvar bits = decimal.GetBits(value);
\treturn (bits[3] >> 16) & 0xFF;
\t}

\tprivate static decimal GetAppliedPrice(ICandleMessage candle, PlateauAppliedPrice price)
\t{
\treturn price switch
\t{
\tPlateauAppliedPrice.Close => candle.ClosePrice,
\tPlateauAppliedPrice.Open => candle.OpenPrice,
\tPlateauAppliedPrice.High => candle.HighPrice,
\tPlateauAppliedPrice.Low => candle.LowPrice,
\tPlateauAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
\tPlateauAppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
\tPlateauAppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
\t_ => candle.ClosePrice,
\t};
\t}

\tprivate static IIndicator CreateMovingAverage(PlateauMovingAverageMethod method, int period)
\t{
\treturn method switch
\t{
\tPlateauMovingAverageMethod.Simple => new SimpleMovingAverage { Length = Math.Max(1, period) },
\tPlateauMovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = Math.Max(1, period) },
\tPlateauMovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = Math.Max(1, period) },
\tPlateauMovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = Math.Max(1, period) },
\t_ => new WeightedMovingAverage { Length = Math.Max(1, period) },
\t};
\t}
}
