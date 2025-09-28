using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor FT_TrendFollower.
/// Combines a Guppy MMA fan, Laguerre trigger, MACD filter, and EMA crossover confirmation.
/// Includes optional pivot- and channel-based exit modules with staged profit taking.
/// </summary>
public class FtTrendFollowerStrategy : Strategy
{
	private readonly StrategyParam<int> _startGmmaPeriod;
	private readonly StrategyParam<int> _endGmmaPeriod;
	private readonly StrategyParam<int> _bandsPerGroup;
	private readonly StrategyParam<int> _fastSignalLength;
	private readonly StrategyParam<int> _slowSignalLength;
	private readonly StrategyParam<int> _tradeShift;
	private readonly StrategyParam<bool> _useSwingStop;
	private readonly StrategyParam<decimal> _swingStopPips;
	private readonly StrategyParam<bool> _useFixedStop;
	private readonly StrategyParam<decimal> _fixedStopPips;
	private readonly StrategyParam<bool> _enablePivotExit;
	private readonly StrategyParam<bool> _enablePivotRangeExit;
	private readonly StrategyParam<bool> _enableChannelExit;
	private readonly StrategyParam<decimal> _laguerreOversold;
	private readonly StrategyParam<decimal> _laguerreOverbought;
	private readonly StrategyParam<decimal> _laguerreGamma;
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage[] _gmmaIndicators = Array.Empty<ExponentialMovingAverage>();
	private decimal?[] _gmmaPreviousValues = Array.Empty<decimal?>();
	private ExponentialMovingAverage _emaFast = null!;
	private ExponentialMovingAverage _emaSlow = null!;
	private AdaptiveLaguerreFilter _laguerre = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private HullMovingAverage _hma = null!;
	private SimpleMovingAverage _channelHigh = null!;
	private SimpleMovingAverage _channelLow = null!;

	private decimal? _pivot;
	private decimal? _resistance1;
	private decimal? _resistance2;
	private decimal? _support1;
	private decimal? _support2;

	private bool _closeArmedLong;
	private bool _closeArmedShort;
	private bool _laguerreArmedLong;
	private bool _laguerreArmedShort;
	private bool _emaArmedLong;
	private bool _emaArmedShort;

	private int _longPartialStage;
	private int _shortPartialStage;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;


	/// <summary>
	/// First period in the Guppy MMA fan.
	/// </summary>
	public int StartGmmaPeriod
	{
		get => _startGmmaPeriod.Value;
		set => _startGmmaPeriod.Value = value;
	}

	/// <summary>
	/// Last period in the Guppy MMA fan.
	/// </summary>
	public int EndGmmaPeriod
	{
		get => _endGmmaPeriod.Value;
		set => _endGmmaPeriod.Value = value;
	}

	/// <summary>
	/// Number of EMAs per Guppy band group (mirrors CountLine).
	/// </summary>
	public int BandsPerGroup
	{
		get => _bandsPerGroup.Value;
		set => _bandsPerGroup.Value = value;
	}

	/// <summary>
	/// Fast EMA length used for crossover confirmation.
	/// </summary>
	public int FastSignalLength
	{
		get => _fastSignalLength.Value;
		set => _fastSignalLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length used for crossover confirmation.
	/// </summary>
	public int SlowSignalLength
	{
		get => _slowSignalLength.Value;
		set => _slowSignalLength.Value = value;
	}

	/// <summary>
	/// Candle shift used by the original expert (0 or 1).
	/// </summary>
	public int TradeShift
	{
		get => _tradeShift.Value;
		set => _tradeShift.Value = value;
	}

	/// <summary>
	/// Enables stop placement beneath the previous candle extremum.
	/// </summary>
	public bool UseSwingStop
	{
		get => _useSwingStop.Value;
		set => _useSwingStop.Value = value;
	}

	/// <summary>
	/// Swing stop distance in points (MQL pips).
	/// </summary>
	public decimal SwingStopPips
	{
		get => _swingStopPips.Value;
		set => _swingStopPips.Value = value;
	}

	/// <summary>
	/// Enables fixed-distance protective stops.
	/// </summary>
	public bool UseFixedStop
	{
		get => _useFixedStop.Value;
		set => _useFixedStop.Value = value;
	}

	/// <summary>
	/// Fixed stop distance in points (MQL pips).
	/// </summary>
	public decimal FixedStopPips
	{
		get => _fixedStopPips.Value;
		set => _fixedStopPips.Value = value;
	}

	/// <summary>
	/// Enables pivot-based staged exit module (Quit).
	/// </summary>
	public bool EnablePivotExit
	{
		get => _enablePivotExit.Value;
		set => _enablePivotExit.Value = value;
	}

	/// <summary>
	/// Enables the pivot range exit module (Quit1).
	/// </summary>
	public bool EnablePivotRangeExit
	{
		get => _enablePivotRangeExit.Value;
		set => _enablePivotRangeExit.Value = value;
	}

	/// <summary>
	/// Enables the channel-based exit module (Quit2).
	/// </summary>
	public bool EnableChannelExit
	{
		get => _enableChannelExit.Value;
		set => _enableChannelExit.Value = value;
	}

	/// <summary>
	/// Laguerre oversold trigger level.
	/// </summary>
	public decimal LaguerreOversold
	{
		get => _laguerreOversold.Value;
		set => _laguerreOversold.Value = value;
	}

	/// <summary>
	/// Laguerre overbought trigger level.
	/// </summary>
	public decimal LaguerreOverbought
	{
		get => _laguerreOverbought.Value;
		set => _laguerreOverbought.Value = value;
	}

	/// <summary>
	/// Laguerre smoothing factor.
	/// </summary>
	public decimal LaguerreGamma
	{
		get => _laguerreGamma.Value;
		set => _laguerreGamma.Value = value;
	}

	/// <summary>
	/// Hull moving average period used by the exit module.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the high/low channel averages.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Candle type driving the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FtTrendFollowerStrategy"/> class.
	/// </summary>
	public FtTrendFollowerStrategy()
	{

		_startGmmaPeriod = Param(nameof(StartGmmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("GMMA Start", "Smallest EMA length in the Guppy fan", "GMMA");

		_endGmmaPeriod = Param(nameof(EndGmmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("GMMA End", "Largest EMA length in the Guppy fan", "GMMA");

		_bandsPerGroup = Param(nameof(BandsPerGroup), 5)
			.SetRange(1, 10)
			.SetDisplay("Bands Per Group", "Number of GMMA lines sampled per group", "GMMA");

		_fastSignalLength = Param(nameof(FastSignalLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length for crossover filter", "Signals");

		_slowSignalLength = Param(nameof(SlowSignalLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length for crossover filter", "Signals");

		_tradeShift = Param(nameof(TradeShift), 1)
			.SetRange(0, 1)
			.SetDisplay("Trade Shift", "Bar shift used by the original EA (0 or 1)", "Signals");

		_useSwingStop = Param(nameof(UseSwingStop), true)
			.SetDisplay("Swing Stop", "Enable stop under/above prior candle extremum", "Risk");

		_swingStopPips = Param(nameof(SwingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Swing Stop Points", "Swing stop distance expressed in points", "Risk");

		_useFixedStop = Param(nameof(UseFixedStop), false)
			.SetDisplay("Fixed Stop", "Enable fixed-distance stop", "Risk");

		_fixedStopPips = Param(nameof(FixedStopPips), 25m)
			.SetNotNegative()
			.SetDisplay("Fixed Stop Points", "Fixed stop distance expressed in points", "Risk");

		_enablePivotExit = Param(nameof(EnablePivotExit), true)
			.SetDisplay("Pivot Exit", "Enable pivot-based staged exit", "Exits");

		_enablePivotRangeExit = Param(nameof(EnablePivotRangeExit), false)
			.SetDisplay("Pivot Range Exit", "Enable second pivot range exit module", "Exits");

		_enableChannelExit = Param(nameof(EnableChannelExit), false)
			.SetDisplay("Channel Exit", "Enable channel-based exit module", "Exits");

		_laguerreOversold = Param(nameof(LaguerreOversold), 0.15m)
			.SetRange(0m, 1m)
			.SetDisplay("Laguerre Oversold", "Laguerre threshold that arms long trades", "Signals");

		_laguerreOverbought = Param(nameof(LaguerreOverbought), 0.75m)
			.SetRange(0m, 1m)
			.SetDisplay("Laguerre Overbought", "Laguerre threshold that arms short trades", "Signals");

		_laguerreGamma = Param(nameof(LaguerreGamma), 0.7m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay("Laguerre Gamma", "Laguerre smoothing factor", "Signals");

		_hmaPeriod = Param(nameof(HmaPeriod), 80)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Hull MA period for the exit module", "Exits");

		_channelPeriod = Param(nameof(ChannelPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Length of the high/low channel averages", "Exits");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used by the strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromDays(1).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_gmmaIndicators = Array.Empty<ExponentialMovingAverage>();
		_gmmaPreviousValues = Array.Empty<decimal?>();
		_emaFast = null!;
		_emaSlow = null!;
		_laguerre = null!;
		_macd = null!;
		_hma = null!;
		_channelHigh = null!;
		_channelLow = null!;

		_pivot = null;
		_resistance1 = null;
		_resistance2 = null;
		_support1 = null;
		_support2 = null;

		_closeArmedLong = false;
		_closeArmedShort = false;
		_laguerreArmedLong = false;
		_laguerreArmedShort = false;
		_emaArmedLong = false;
		_emaArmedShort = false;
		_longPartialStage = 0;
		_shortPartialStage = 0;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ValidateParameters();

		_gmmaIndicators = BuildGmmaIndicators();
		_gmmaPreviousValues = new decimal?[_gmmaIndicators.Length];
		_emaFast = new ExponentialMovingAverage { Length = FastSignalLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowSignalLength };
		_laguerre = new AdaptiveLaguerreFilter { Gamma = LaguerreGamma };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = 5 },
			LongMa = { Length = 35 },
			SignalMa = { Length = 5 }
		};
		_hma = new HullMovingAverage { Length = HmaPeriod };
		_channelHigh = new SimpleMovingAverage { Length = ChannelPeriod, CandlePrice = CandlePrice.High };
		_channelLow = new SimpleMovingAverage { Length = ChannelPeriod, CandlePrice = CandlePrice.Low };

	var primaryIndicators = new List<IIndicator>(_gmmaIndicators.Length + 7);
		primaryIndicators.AddRange(_gmmaIndicators);
		primaryIndicators.Add(_emaFast);
		primaryIndicators.Add(_emaSlow);
		primaryIndicators.Add(_laguerre);
		primaryIndicators.Add(_macd);
		primaryIndicators.Add(_hma);
		primaryIndicators.Add(_channelHigh);
		primaryIndicators.Add(_channelLow);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(primaryIndicators, ProcessCandle)
			.Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			foreach (var indicator in _gmmaIndicators)
			{
				DrawIndicator(area, indicator);
			}
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawIndicator(area, _laguerre);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var pivot = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var range = candle.HighPrice - candle.LowPrice;

		_pivot = pivot;
		_resistance1 = 2m * pivot - candle.LowPrice;
		_support1 = 2m * pivot - candle.HighPrice;
		_resistance2 = pivot + range;
		_support2 = pivot - range;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var gmmaCount = _gmmaIndicators.Length;
		if (values.Length < gmmaCount + 7)
			return;

		var gmmaValues = new decimal[gmmaCount];
		for (var i = 0; i < gmmaCount; i++)
		{
			if (!values[i].IsFinal)
				return;
			gmmaValues[i] = values[i].ToDecimal();
		}

		var emaFastValue = values[gmmaCount].ToDecimal();
		var emaSlowValue = values[gmmaCount + 1].ToDecimal();
		var laguerreValue = values[gmmaCount + 2].ToDecimal();
		var macdValue = values[gmmaCount + 3];
_ = values[gmmaCount + 4].ToDecimal();
var channelHigh = values[gmmaCount + 5].ToDecimal();
var channelLow = values[gmmaCount + 6].ToDecimal();

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_laguerre.IsFormed || !_macd.IsFormed || !_channelHigh.IsFormed || !_channelLow.IsFormed)
		{
			UpdateGmmaHistory(gmmaValues);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateGmmaHistory(gmmaValues);
			return;
		}

		if (!_gmmaPreviousValues.All(v => v.HasValue))
		{
			UpdateGmmaHistory(gmmaValues);
			return;
		}

		if (macdValue is not MovingAverageConvergenceDivergenceValue macdData || macdData.Macd is not decimal macdMain)
		{
			UpdateGmmaHistory(gmmaValues);
			return;
		}

		var slowestGmma = gmmaValues[^1];
		var fastestGmma = gmmaValues[0];
		var trend = DetermineTrend(gmmaValues);

		UpdateCloseStates(candle.ClosePrice, fastestGmma, slowestGmma);
		UpdateLaguerreStates(trend, laguerreValue, candle.ClosePrice, candle.LowPrice, candle.HighPrice, fastestGmma, slowestGmma);
		UpdateEmaStates(trend, emaFastValue, emaSlowValue);

		var (upSlopeCount, downSlopeCount) = CountGmmaSlopes(gmmaValues);
		var slopeThreshold = gmmaCount / 2;

		ProcessStops(candle);
ProcessExitModules(candle, channelHigh, channelLow);

		TryEnterLong(candle, laguerreValue, emaFastValue, emaSlowValue, macdMain, upSlopeCount, slopeThreshold);
		TryEnterShort(candle, laguerreValue, emaFastValue, emaSlowValue, macdMain, downSlopeCount, slopeThreshold);

		UpdateGmmaHistory(gmmaValues);
	}

	private void ProcessStops(ICandleMessage candle)
	{
		if (Position > 0 && _longStopPrice is decimal longStop && candle.LowPrice <= longStop)
		{
			SellMarket(Math.Abs(Position));
			ResetLongState();
		}
		else if (Position < 0 && _shortStopPrice is decimal shortStop && candle.HighPrice >= shortStop)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
		}

		if (Position <= 0 && _longPartialStage != 0)
			ResetLongState();
		if (Position >= 0 && _shortPartialStage != 0)
			ResetShortState();
	}

	private void ProcessExitModules(ICandleMessage candle, decimal channelHigh, decimal channelLow)
	{
		var point = GetPoint();
		var hasPivot = _pivot.HasValue && _resistance1.HasValue && _support1.HasValue;

		if (Position > 0 && Volume > 0m && hasPivot)
		{
			var closePrice = candle.ClosePrice;
			var entryProfit = closePrice - _longEntryPrice;
			var profitable = entryProfit > point;

			if (_longPartialStage == 0 && profitable)
			{
				if (EnablePivotExit && closePrice > _resistance1)
				{
					CloseHalfLong();
					_longPartialStage = 1;
				}
				else if (EnablePivotRangeExit && closePrice > _resistance1)
				{
					CloseHalfLong();
					_longPartialStage = 1;
				}
				else if (EnableChannelExit && closePrice > _resistance1)
				{
					CloseHalfLong();
					_longPartialStage = 1;
				}
			}

			if (_longPartialStage == 1 && Math.Abs(Position) > 0m)
			{
				if (EnablePivotExit && _hma.IsFormed)
				{
					SellMarket(Math.Abs(Position));
					ResetLongState();
				}
				else if (EnablePivotRangeExit && _resistance2.HasValue && closePrice > _resistance2)
				{
					SellMarket(Math.Abs(Position));
					ResetLongState();
				}
				else if (EnableChannelExit && candle.OpenPrice < channelLow)
				{
					SellMarket(Math.Abs(Position));
					ResetLongState();
				}
			}
		}
		else if (Position < 0 && Volume > 0m && hasPivot)
		{
			var closePrice = candle.ClosePrice;
			var entryProfit = _shortEntryPrice - closePrice;
			var profitable = entryProfit > point;

			if (_shortPartialStage == 0 && profitable)
			{
				if (EnablePivotExit && closePrice < _support1)
				{
					CloseHalfShort();
					_shortPartialStage = 1;
				}
				else if (EnablePivotRangeExit && closePrice < _support1)
				{
					CloseHalfShort();
					_shortPartialStage = 1;
				}
				else if (EnableChannelExit && closePrice < _support1)
				{
					CloseHalfShort();
					_shortPartialStage = 1;
				}
			}

			if (_shortPartialStage == 1 && Math.Abs(Position) > 0m)
			{
				if (EnablePivotExit && _hma.IsFormed)
				{
					BuyMarket(Math.Abs(Position));
					ResetShortState();
				}
				else if (EnablePivotRangeExit && _support2.HasValue && closePrice < _support2)
				{
					BuyMarket(Math.Abs(Position));
					ResetShortState();
				}
				else if (EnableChannelExit && candle.OpenPrice > channelHigh)
				{
					BuyMarket(Math.Abs(Position));
					ResetShortState();
				}
			}
		}
	}

	private void TryEnterLong(ICandleMessage candle, decimal laguerreValue, decimal emaFastValue, decimal emaSlowValue, decimal macdMain, int slopeCount, int slopeThreshold)
	{
		if (Position > 0)
			return;

		if (!_laguerreArmedLong || !_emaArmedLong)
			return;

		if (laguerreValue <= LaguerreOversold)
			return;

		if (emaFastValue <= emaSlowValue)
			return;

		if (macdMain <= 0m)
			return;

		if (slopeCount <= slopeThreshold)
			return;

		var volume = Volume + Math.Max(0m, -Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_longEntryPrice = candle.ClosePrice;
		_longStopPrice = CalculateLongStop(candle.ClosePrice, candle.LowPrice);
		_longPartialStage = 0;
		_closeArmedLong = false;
		_laguerreArmedLong = false;
		_emaArmedLong = false;
		_shortPartialStage = 0;
		_shortStopPrice = null;
		_shortEntryPrice = 0m;
	}

	private void TryEnterShort(ICandleMessage candle, decimal laguerreValue, decimal emaFastValue, decimal emaSlowValue, decimal macdMain, int slopeCount, int slopeThreshold)
	{
		if (Position < 0)
			return;

		if (!_laguerreArmedShort || !_emaArmedShort)
			return;

		if (laguerreValue >= LaguerreOverbought)
			return;

		if (emaFastValue >= emaSlowValue)
			return;

		if (macdMain >= 0m)
			return;

		if (slopeCount <= slopeThreshold)
			return;

		var volume = Volume + Math.Max(0m, Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_shortEntryPrice = candle.ClosePrice;
		_shortStopPrice = CalculateShortStop(candle.ClosePrice, candle.HighPrice);
		_shortPartialStage = 0;
		_closeArmedShort = false;
		_laguerreArmedShort = false;
		_emaArmedShort = false;
		_longPartialStage = 0;
		_longStopPrice = null;
		_longEntryPrice = 0m;
	}

	private void UpdateCloseStates(decimal close, decimal fastestGmma, decimal slowestGmma)
	{
		if (close < slowestGmma)
			_closeArmedLong = true;
		if (close > slowestGmma)
			_closeArmedShort = true;

		if (_closeArmedLong && close > fastestGmma)
		{
			_closeArmedLong = false;
			_laguerreArmedLong = false;
			_emaArmedLong = false;
		}

		if (_closeArmedShort && close < fastestGmma)
		{
			_closeArmedShort = false;
			_laguerreArmedShort = false;
			_emaArmedShort = false;
		}
	}

	private void UpdateLaguerreStates(TrendDirections trend, decimal laguerreValue, decimal close, decimal low, decimal high, decimal fastestGmma, decimal slowestGmma)
	{
		if (trend == TrendDirections.Up && close > slowestGmma && low < fastestGmma && laguerreValue < LaguerreOversold)
			_laguerreArmedLong = true;

		if (trend == TrendDirections.Down && close < slowestGmma && high > fastestGmma && laguerreValue > LaguerreOverbought)
			_laguerreArmedShort = true;
	}

	private void UpdateEmaStates(TrendDirections trend, decimal emaFastValue, decimal emaSlowValue)
	{
		if (trend == TrendDirections.Up && emaFastValue < emaSlowValue)
			_emaArmedLong = true;
		if (trend == TrendDirections.Down && emaFastValue > emaSlowValue)
			_emaArmedShort = true;
	}

	private (int upCount, int downCount) CountGmmaSlopes(decimal[] currentValues)
	{
		var up = 0;
		var down = 0;
		var total = currentValues.Length;
		var group = BandsPerGroup;

		for (var i = group; i >= 0; i--)
		{
			var longIndex = total - (group + i);
			if (longIndex >= 0 && longIndex < total && _gmmaPreviousValues[longIndex] is decimal prevLong)
			{
				var value = currentValues[longIndex];
				if (value > prevLong)
					up++;
				else if (value < prevLong)
					down++;
			}

			var midIndex = total - (group + group + i - 1);
			if (midIndex >= 0 && midIndex < total && _gmmaPreviousValues[midIndex] is decimal prevMid)
			{
				var value = currentValues[midIndex];
				if (value > prevMid)
					up++;
				else if (value < prevMid)
					down++;
			}

			var shortIndex = i;
			if (shortIndex >= 0 && shortIndex < total && _gmmaPreviousValues[shortIndex] is decimal prevShort)
			{
				var value = currentValues[shortIndex];
				if (value > prevShort)
					up++;
				else if (value < prevShort)
					down++;
			}
		}

		return (up, down);
	}

	private TrendDirections DetermineTrend(decimal[] gmmaValues)
	{
		var total = gmmaValues.Length;
		var group = BandsPerGroup;
		var aIndex = total - (group + group);
		var bIndex = total - group;

		if (aIndex < 0 || bIndex < 0 || aIndex >= total || bIndex >= total)
			return TrendDirections.Flat;

		var slow = gmmaValues[bIndex];
		var slower = gmmaValues[aIndex];

		if (slower > slow)
			return TrendDirections.Up;
		if (slower < slow)
			return TrendDirections.Down;
		return TrendDirections.Flat;
	}

	private void UpdateGmmaHistory(decimal[] currentValues)
	{
		for (var i = 0; i < currentValues.Length; i++)
		{
			_gmmaPreviousValues[i] = currentValues[i];
		}
	}

	private decimal GetPoint()
	{
		var step = Security?.PriceStep ?? Security?.MinPriceStep ?? 0.0001m;
		return step <= 0m ? 0.0001m : step;
	}

	private decimal? CalculateLongStop(decimal entryPrice, decimal candleLow)
	{
		decimal? stop = null;
		var point = GetPoint();
		var minimum = 15m * point;

		if (UseSwingStop)
		{
			var swingStop = candleLow - (SwingStopPips * point);
			if (entryPrice - swingStop < minimum)
				swingStop = entryPrice - minimum;
			stop = swingStop;
		}

		if (UseFixedStop)
		{
			var fixedStop = entryPrice - (FixedStopPips * point);
			stop = stop is null ? fixedStop : Math.Min(stop.Value, fixedStop);
		}

		return stop;
	}

	private decimal? CalculateShortStop(decimal entryPrice, decimal candleHigh)
	{
		decimal? stop = null;
		var point = GetPoint();
		var minimum = 15m * point;

		if (UseSwingStop)
		{
			var swingStop = candleHigh + (SwingStopPips * point);
			if (swingStop - entryPrice < minimum)
				swingStop = entryPrice + minimum;
			stop = swingStop;
		}

		if (UseFixedStop)
		{
			var fixedStop = entryPrice + (FixedStopPips * point);
			stop = stop is null ? fixedStop : Math.Max(stop.Value, fixedStop);
		}

		return stop;
	}

	private void CloseHalfLong()
	{
		var half = Volume / 2m;
		var amount = Math.Min(Math.Abs(Position), half);
		if (amount > 0m)
			SellMarket(amount);
	}

	private void CloseHalfShort()
	{
		var half = Volume / 2m;
		var amount = Math.Min(Math.Abs(Position), half);
		if (amount > 0m)
			BuyMarket(amount);
	}

	private void ResetLongState()
	{
		_longPartialStage = 0;
		_longStopPrice = null;
		_longEntryPrice = 0m;
	}

	private void ResetShortState()
	{
		_shortPartialStage = 0;
		_shortStopPrice = null;
		_shortEntryPrice = 0m;
	}

	private ExponentialMovingAverage[] BuildGmmaIndicators()
	{
		var periods = new List<int>();
		var totalBands = BandsPerGroup * 5;
		var step = Math.Max(1, (EndGmmaPeriod - StartGmmaPeriod) / Math.Max(1, totalBands));

		for (var period = StartGmmaPeriod; period <= EndGmmaPeriod; period += step)
		{
			periods.Add(period);
		}

		var indicators = new ExponentialMovingAverage[periods.Count];
		for (var i = 0; i < periods.Count; i++)
		{
			indicators[i] = new ExponentialMovingAverage { Length = periods[i] };
		}

		return indicators;
	}

	private void ValidateParameters()
	{
		if (StartGmmaPeriod >= EndGmmaPeriod)
			throw new InvalidOperationException("Start GMMA period must be less than end period.");

		if (BandsPerGroup <= 0 || BandsPerGroup > 10)
			throw new InvalidOperationException("Bands per group must be between 1 and 10.");

		if (BandsPerGroup > StartGmmaPeriod || BandsPerGroup > EndGmmaPeriod)
			throw new InvalidOperationException("Bands per group must be less than GMMA periods.");

		if (FastSignalLength > SlowSignalLength)
			throw new InvalidOperationException("Fast EMA length must not exceed slow EMA length.");

		if (UseSwingStop && UseFixedStop)
			throw new InvalidOperationException("Only one stop module may be active at a time.");

		if (TradeShift is < 0 or > 1)
			throw new InvalidOperationException("Trade shift may only be 0 or 1.");

		var exitModules = new[] { EnablePivotExit, EnablePivotRangeExit, EnableChannelExit };
		if (exitModules.Count(enabled => enabled) > 1)
			throw new InvalidOperationException("Enable only one exit module at a time.");
	}

	private enum TrendDirections
	{
		Flat,
		Up,
		Down
	}
}
