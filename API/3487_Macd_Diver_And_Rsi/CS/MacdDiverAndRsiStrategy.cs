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
/// Conversion of the "Macd diver and rsi" expert advisor from MQL5.
/// Combines RSI extremes with MACD histogram reversals to trade both sides of the market.
/// </summary>
public class MacdDiverAndRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<int> _longRsiPeriod;
	private readonly StrategyParam<decimal> _longRsiThreshold;
	private readonly StrategyParam<int> _longMacdFastLength;
	private readonly StrategyParam<int> _longMacdSlowLength;
	private readonly StrategyParam<int> _longMacdSignalLength;
	private readonly StrategyParam<decimal> _longVolume;
	private readonly StrategyParam<decimal> _longStopLossPips;
	private readonly StrategyParam<decimal> _longTakeProfitPips;

	private readonly StrategyParam<int> _shortRsiPeriod;
	private readonly StrategyParam<decimal> _shortRsiThreshold;
	private readonly StrategyParam<int> _shortMacdFastLength;
	private readonly StrategyParam<int> _shortMacdSlowLength;
	private readonly StrategyParam<int> _shortMacdSignalLength;
	private readonly StrategyParam<decimal> _shortVolume;
	private readonly StrategyParam<decimal> _shortStopLossPips;
	private readonly StrategyParam<decimal> _shortTakeProfitPips;

	private RelativeStrengthIndex _longRsi = null!;
	private RelativeStrengthIndex _shortRsi = null!;
	private MovingAverageConvergenceDivergenceSignal _longMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _shortMacd = null!;

	private decimal? _previousLongHistogram;
	private decimal? _previousShortHistogram;

	private decimal _pipSize;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private bool _isLongPosition;

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdDiverAndRsiStrategy"/> class.
	/// </summary>
	public MacdDiverAndRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal calculation", "General");

		_longRsiPeriod = Param(nameof(LongRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Long RSI Period", "Length of RSI for bullish setups", "Long")
			.SetCanOptimize(true);

		_longRsiThreshold = Param(nameof(LongRsiThreshold), 30m)
			.SetDisplay("Long RSI Threshold", "Oversold threshold that enables long signals", "Long")
			.SetCanOptimize(true);

		_longMacdFastLength = Param(nameof(LongMacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Long MACD Fast", "Fast EMA length for bullish MACD", "Long")
			.SetCanOptimize(true);

		_longMacdSlowLength = Param(nameof(LongMacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Long MACD Slow", "Slow EMA length for bullish MACD", "Long")
			.SetCanOptimize(true);

		_longMacdSignalLength = Param(nameof(LongMacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Long MACD Signal", "Signal EMA length for bullish MACD", "Long")
			.SetCanOptimize(true);

		_longVolume = Param(nameof(LongVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Long Volume", "Order size used when opening long trades", "Long")
			.SetCanOptimize(true);

		_longStopLossPips = Param(nameof(LongStopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Long Stop Loss (pips)", "Distance of the protective stop for long trades", "Long")
			.SetCanOptimize(true);

		_longTakeProfitPips = Param(nameof(LongTakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Long Take Profit (pips)", "Distance of the profit target for long trades", "Long")
			.SetCanOptimize(true);

		_shortRsiPeriod = Param(nameof(ShortRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Short RSI Period", "Length of RSI for bearish setups", "Short")
			.SetCanOptimize(true);

		_shortRsiThreshold = Param(nameof(ShortRsiThreshold), 70m)
			.SetDisplay("Short RSI Threshold", "Overbought threshold that enables short signals", "Short")
			.SetCanOptimize(true);

		_shortMacdFastLength = Param(nameof(ShortMacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Short MACD Fast", "Fast EMA length for bearish MACD", "Short")
			.SetCanOptimize(true);

		_shortMacdSlowLength = Param(nameof(ShortMacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Short MACD Slow", "Slow EMA length for bearish MACD", "Short")
			.SetCanOptimize(true);

		_shortMacdSignalLength = Param(nameof(ShortMacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short MACD Signal", "Signal EMA length for bearish MACD", "Short")
			.SetCanOptimize(true);

		_shortVolume = Param(nameof(ShortVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Short Volume", "Order size used when opening short trades", "Short")
			.SetCanOptimize(true);

		_shortStopLossPips = Param(nameof(ShortStopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Short Stop Loss (pips)", "Distance of the protective stop for short trades", "Short")
			.SetCanOptimize(true);

		_shortTakeProfitPips = Param(nameof(ShortTakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Short Take Profit (pips)", "Distance of the profit target for short trades", "Short")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI length for long setups.
	/// </summary>
	public int LongRsiPeriod
	{
		get => _longRsiPeriod.Value;
		set => _longRsiPeriod.Value = value;
	}

	/// <summary>
	/// Oversold threshold for long signals.
	/// </summary>
	public decimal LongRsiThreshold
	{
		get => _longRsiThreshold.Value;
		set => _longRsiThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length of the bullish MACD.
	/// </summary>
	public int LongMacdFastLength
	{
		get => _longMacdFastLength.Value;
		set => _longMacdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length of the bullish MACD.
	/// </summary>
	public int LongMacdSlowLength
	{
		get => _longMacdSlowLength.Value;
		set => _longMacdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length of the bullish MACD.
	/// </summary>
	public int LongMacdSignalLength
	{
		get => _longMacdSignalLength.Value;
		set => _longMacdSignalLength.Value = value;
	}

	/// <summary>
	/// Trade volume for long entries.
	/// </summary>
	public decimal LongVolume
	{
		get => _longVolume.Value;
		set => _longVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips for long trades.
	/// </summary>
	public decimal LongStopLossPips
	{
		get => _longStopLossPips.Value;
		set => _longStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips for long trades.
	/// </summary>
	public decimal LongTakeProfitPips
	{
		get => _longTakeProfitPips.Value;
		set => _longTakeProfitPips.Value = value;
	}

	/// <summary>
	/// RSI length for short setups.
	/// </summary>
	public int ShortRsiPeriod
	{
		get => _shortRsiPeriod.Value;
		set => _shortRsiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought threshold for short signals.
	/// </summary>
	public decimal ShortRsiThreshold
	{
		get => _shortRsiThreshold.Value;
		set => _shortRsiThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length of the bearish MACD.
	/// </summary>
	public int ShortMacdFastLength
	{
		get => _shortMacdFastLength.Value;
		set => _shortMacdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length of the bearish MACD.
	/// </summary>
	public int ShortMacdSlowLength
	{
		get => _shortMacdSlowLength.Value;
		set => _shortMacdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length of the bearish MACD.
	/// </summary>
	public int ShortMacdSignalLength
	{
		get => _shortMacdSignalLength.Value;
		set => _shortMacdSignalLength.Value = value;
	}

	/// <summary>
	/// Trade volume for short entries.
	/// </summary>
	public decimal ShortVolume
	{
		get => _shortVolume.Value;
		set => _shortVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips for short trades.
	/// </summary>
	public decimal ShortStopLossPips
	{
		get => _shortStopLossPips.Value;
		set => _shortStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips for short trades.
	/// </summary>
	public decimal ShortTakeProfitPips
	{
		get => _shortTakeProfitPips.Value;
		set => _shortTakeProfitPips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_longRsi = null!;
		_shortRsi = null!;
		_longMacd = null!;
		_shortMacd = null!;
		_previousLongHistogram = null;
		_previousShortHistogram = null;
		_pipSize = 0m;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longRsi = new RelativeStrengthIndex { Length = LongRsiPeriod };
		_shortRsi = new RelativeStrengthIndex { Length = ShortRsiPeriod };
		_longMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = LongMacdFastLength },
				LongMa = { Length = LongMacdSlowLength }
			},
			SignalMa = { Length = LongMacdSignalLength }
		};
		_shortMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = ShortMacdFastLength },
				LongMa = { Length = ShortMacdSlowLength }
			},
			SignalMa = { Length = ShortMacdSignalLength }
		};

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_longRsi, _longMacd, _shortRsi, _shortMacd, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);

			var macdArea = CreateChartArea();
			if (macdArea != null)
			{
				DrawIndicator(macdArea, _longMacd);
				DrawIndicator(macdArea, _shortMacd);
			}

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
			{
				DrawIndicator(rsiArea, _longRsi);
				DrawIndicator(rsiArea, _shortRsi);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);
		if (Position == 0)
		{
			ResetPositionState();
		}
		else
		{
			_isLongPosition = Position > 0;
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue longRsiValue,
		IIndicatorValue longMacdValue,
		IIndicatorValue shortRsiValue,
		IIndicatorValue shortMacdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!longRsiValue.IsFinal || !longMacdValue.IsFinal || !shortRsiValue.IsFinal || !shortMacdValue.IsFinal)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_pipSize == 0m)
		{
			_pipSize = CalculatePipSize();
		if (_pipSize == 0m)
		return;
		}

		if (ManagePosition(candle))
		return;

		var longRsi = longRsiValue.ToDecimal();
		var shortRsi = shortRsiValue.ToDecimal();

		var longMacdData = (MovingAverageConvergenceDivergenceSignalValue)longMacdValue;
		if (longMacdData.Histogram is not decimal longHistogram)
		{
		_previousLongHistogram = null;
		return;
		}

		var shortMacdData = (MovingAverageConvergenceDivergenceSignalValue)shortMacdValue;
		if (shortMacdData.Histogram is not decimal shortHistogram)
		{
		_previousShortHistogram = null;
		return;
		}

		var longHistogramPositive = longHistogram > 0m;
		var shortHistogramNegative = shortHistogram < 0m;

		var longCrossUp = longHistogramPositive && (!_previousLongHistogram.HasValue || _previousLongHistogram.Value <= 0m);
		var shortCrossDown = shortHistogramNegative && (!_previousShortHistogram.HasValue || _previousShortHistogram.Value >= 0m);

		_previousLongHistogram = longHistogram;
		_previousShortHistogram = shortHistogram;

		var longSignal = longRsi <= LongRsiThreshold && longCrossUp;
		if (longSignal && Position <= 0)
		{
		var volume = LongVolume;
		if (Position < 0)
		volume += Math.Abs(Position);

		if (volume > 0m)
		{
		BuyMarket(volume);
		InitializePositionState(candle.ClosePrice, true, LongStopLossPips, LongTakeProfitPips);
		}
		return;
		}

		var shortSignal = shortRsi >= ShortRsiThreshold && shortCrossDown;
		if (shortSignal && Position >= 0)
		{
		var volume = ShortVolume;
		if (Position > 0)
		volume += Math.Abs(Position);

		if (volume > 0m)
		{
		SellMarket(volume);
		InitializePositionState(candle.ClosePrice, false, ShortStopLossPips, ShortTakeProfitPips);
		}
		}
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position == 0 || _entryPrice is null)
		return false;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return false;

		if (_isLongPosition)
		{
		if (_stopPrice is decimal longStop && candle.LowPrice <= longStop)
		{
		SellMarket(volume);
		ResetPositionState();
		return true;
		}

		if (_takePrice is decimal longTake && candle.HighPrice >= longTake)
		{
		SellMarket(volume);
		ResetPositionState();
		return true;
		}
		}
		else
		{
		if (_stopPrice is decimal shortStop && candle.HighPrice >= shortStop)
		{
		BuyMarket(volume);
		ResetPositionState();
		return true;
		}

		if (_takePrice is decimal shortTake && candle.LowPrice <= shortTake)
		{
		BuyMarket(volume);
		ResetPositionState();
		return true;
		}
		}

		return false;
	}

	private void InitializePositionState(decimal price, bool isLong, decimal stopLossPips, decimal takeProfitPips)
	{
		_entryPrice = price;
		_isLongPosition = isLong;

		_stopPrice = stopLossPips > 0m
		? price + (isLong ? -1m : 1m) * stopLossPips * _pipSize
		: null;

		_takePrice = takeProfitPips > 0m
		? price + (isLong ? 1m : -1m) * takeProfitPips * _pipSize
		: null;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_isLongPosition = false;
	}

	private decimal CalculatePipSize()
	{
	var step = Security?.PriceStep ?? 0.0001m;
	var decimals = Security?.Decimals ?? 0;
	var adjust = (decimals == 3 || decimals == 5) ? 10m : 1m;
	var pip = step * adjust;
	return pip == 0m ? 0.0001m : pip;
	}
}

