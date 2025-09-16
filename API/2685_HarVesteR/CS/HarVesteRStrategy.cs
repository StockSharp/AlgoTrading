using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend strategy that combines MACD momentum, moving average proximity and ADX filter with partial profit taking.
/// </summary>
public class HarVesteRStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _macdLookback;
	private readonly StrategyParam<int> _smaFastLength;
	private readonly StrategyParam<int> _smaSlowLength;
	private readonly StrategyParam<decimal> _minIndentation;
	private readonly StrategyParam<int> _stopLookback;
	private readonly StrategyParam<bool> _useAdx;
	private readonly StrategyParam<decimal> _adxBuyLevel;
	private readonly StrategyParam<decimal> _adxSellLevel;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _halfCloseRatio;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private SimpleMovingAverage _smaFast = null!;
	private SimpleMovingAverage _smaSlow = null!;
	private AverageDirectionalIndex _adx = null!;
	private Lowest _lowest = null!;
	private Highest _highest = null!;

	private readonly Queue<decimal> _macdHistory = new();
	private decimal? _lastLowest;
	private decimal? _lastHighest;

	private decimal? _longEntry;
	private decimal? _longStop;
	private bool _longStopMoved;

	private decimal? _shortEntry;
	private decimal? _shortStop;
	private bool _shortStopMoved;

	/// <summary>
	/// Fast period for MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow period for MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Number of bars used to confirm MACD sign change.
	/// </summary>
	public int MacdLookback
	{
		get => _macdLookback.Value;
		set => _macdLookback.Value = value;
	}

	/// <summary>
	/// Fast simple moving average length.
	/// </summary>
	public int SmaFastLength
	{
		get => _smaFastLength.Value;
		set => _smaFastLength.Value = value;
	}

	/// <summary>
	/// Slow simple moving average length.
	/// </summary>
	public int SmaSlowLength
	{
		get => _smaSlowLength.Value;
		set => _smaSlowLength.Value = value;
	}

	/// <summary>
	/// Minimum indentation measured in pips.
	/// </summary>
	public decimal MinIndentation
	{
		get => _minIndentation.Value;
		set => _minIndentation.Value = value;
	}

	/// <summary>
	/// Bars used to compute stop loss levels.
	/// </summary>
	public int StopLookback
	{
		get => _stopLookback.Value;
		set => _stopLookback.Value = value;
	}

	/// <summary>
	/// Enable ADX filter for entries.
	/// </summary>
	public bool UseAdxFilter
	{
		get => _useAdx.Value;
		set => _useAdx.Value = value;
	}

	/// <summary>
	/// Minimum ADX strength required to buy.
	/// </summary>
	public decimal AdxBuyLevel
	{
		get => _adxBuyLevel.Value;
		set => _adxBuyLevel.Value = value;
	}

	/// <summary>
	/// Minimum ADX strength required to sell.
	/// </summary>
	public decimal AdxSellLevel
	{
		get => _adxSellLevel.Value;
		set => _adxSellLevel.Value = value;
	}

	/// <summary>
	/// ADX indicator period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Ratio used to trigger half position exit.
	/// </summary>
	public int HalfCloseRatio
	{
		get => _halfCloseRatio.Value;
		set => _halfCloseRatio.Value = value;
	}

	/// <summary>
	/// Default order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// Initializes <see cref="HarVesteRStrategy"/>.
	/// </summary>
	public HarVesteRStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast EMA", "Short EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_macdSlow = Param(nameof(MacdSlow), 24)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow EMA", "Long EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal averaging period", "MACD")
			.SetCanOptimize(true);

		_macdLookback = Param(nameof(MacdLookback), 6)
			.SetGreaterThanZero()
			.SetDisplay("MACD Lookback", "Bars to confirm MACD sign change", "MACD")
			.SetCanOptimize(true);

		_smaFastLength = Param(nameof(SmaFastLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "First moving average length", "Moving Averages")
			.SetCanOptimize(true);

		_smaSlowLength = Param(nameof(SmaSlowLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Second moving average length", "Moving Averages")
			.SetCanOptimize(true);

		_minIndentation = Param(nameof(MinIndentation), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Indentation", "Distance from moving averages in pips", "Trading")
			.SetCanOptimize(true);

		_stopLookback = Param(nameof(StopLookback), 6)
			.SetGreaterThanZero()
			.SetDisplay("Stop Lookback", "Bars for stop loss calculation", "Risk")
			.SetCanOptimize(true);

		_useAdx = Param(nameof(UseAdxFilter), false)
			.SetDisplay("Use ADX", "Enable ADX trend filter", "ADX");

		_adxBuyLevel = Param(nameof(AdxBuyLevel), 50m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Buy Level", "Minimum ADX strength for longs", "ADX");

		_adxSellLevel = Param(nameof(AdxSellLevel), 50m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Sell Level", "Minimum ADX strength for shorts", "ADX");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX calculation length", "ADX")
			.SetCanOptimize(true);

		_halfCloseRatio = Param(nameof(HalfCloseRatio), 2)
			.SetGreaterThanZero()
			.SetDisplay("Half Close Ratio", "Multiplier applied to stop distance", "Risk")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_macdHistory.Clear();
		_lastLowest = null;
		_lastHighest = null;
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Configure indicators used by the strategy.
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		_smaFast = new SimpleMovingAverage { Length = SmaFastLength };
		_smaSlow = new SimpleMovingAverage { Length = SmaSlowLength };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_lowest = new Lowest { Length = StopLookback, CandlePrice = CandlePrice.Low };
		_highest = new Highest { Length = StopLookback, CandlePrice = CandlePrice.High };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _smaFast, _smaSlow, _adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smaFast);
			DrawIndicator(area, _smaSlow);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue smaFastValue, IIndicatorValue smaSlowValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update trailing stop helpers from recent highs and lows.
		var lowValue = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, candle.ServerTime));
		if (lowValue.IsFormed)
			_lastLowest = lowValue.ToDecimal();

		var highValue = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, candle.ServerTime));
		if (highValue.IsFormed)
			_lastHighest = highValue.ToDecimal();

		if (!macdValue.IsFinal || !smaFastValue.IsFinal || !smaSlowValue.IsFinal)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdMain)
			return;

		var smaFast = smaFastValue.ToDecimal();
		var smaSlow = smaSlowValue.ToDecimal();

		decimal? adxStrength = null;
		if (UseAdxFilter)
		{
			if (!adxValue.IsFinal)
				return;

			var adxTyped = (AverageDirectionalIndexValue)adxValue;
			adxStrength = adxTyped.MovingAverage;
			if (adxStrength is not decimal)
				return;
		}

		_macdHistory.Enqueue(macdMain);
		while (_macdHistory.Count > MacdLookback)
			_macdHistory.Dequeue();

		var indentation = GetIndentation();
		var close = candle.ClosePrice;

		if (macdMain == 0m || smaFast == 0m || smaSlow == 0m || close <= 0m)
			return;

		// Manage partial exits and break-even logic for open positions.
		ManageOpenPositions(close, smaFast, indentation);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_macdHistory.Count < MacdLookback)
			return;

		var hadNegative = HasNegativeMacd();
		var hadPositive = HasPositiveMacd();

		var adxBuyOk = !UseAdxFilter;
		var adxSellOk = !UseAdxFilter;
		if (UseAdxFilter && adxStrength is decimal adxValueDecimal)
		{
			adxBuyOk = adxValueDecimal >= AdxBuyLevel;
			adxSellOk = adxValueDecimal >= AdxSellLevel;
		}

		var okBuy = close < smaSlow;
		var okSell = close > smaSlow;

		if (macdMain > 0m && hadNegative && adxBuyOk && okBuy && close + indentation > smaFast && close + indentation > smaSlow && Position <= 0m && _lastLowest is decimal longStop)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_longEntry = close;
				_longStop = longStop;
				_longStopMoved = false;
				ResetShortState();
			}
		}
		else if (macdMain < 0m && hadPositive && adxSellOk && okSell && close - indentation < smaFast && close - indentation < smaSlow && Position >= 0m && _lastHighest is decimal shortStop)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				_shortEntry = close;
				_shortStop = shortStop;
				_shortStopMoved = false;
				ResetLongState();
			}
		}
	}

	private void ManageOpenPositions(decimal close, decimal smaFast, decimal indentation)
	{
		if (Position > 0m && _longEntry is decimal entry && _longStop is decimal stop)
		{
			var distance = Math.Abs(entry - stop);
			if (distance > 0m)
			{
				var target = entry + distance * HalfCloseRatio;
				if (!_longStopMoved && close > target)
				{
					var half = Position / 2m;
					if (half > 0m)
					{
						SellMarket(half);
						_longStop = entry;
						_longStopMoved = true;
					}
				}
				else if (_longStopMoved && smaFast > close - indentation)
				{
					SellMarket(Position);
					ResetLongState();
				}
			}
		}
		else if (Position <= 0m)
		{
			ResetLongState();
		}

		if (Position < 0m && _shortEntry is decimal entryShort && _shortStop is decimal stopShort)
		{
			var distance = Math.Abs(entryShort - stopShort);
			if (distance > 0m)
			{
				var target = entryShort - distance * HalfCloseRatio;
				if (!_shortStopMoved && close < target)
				{
					var half = -Position / 2m;
					if (half > 0m)
					{
						BuyMarket(half);
						_shortStop = entryShort;
						_shortStopMoved = true;
					}
				}
				else if (_shortStopMoved && smaFast < close - indentation)
				{
					BuyMarket(-Position);
					ResetShortState();
				}
			}
		}
		else if (Position >= 0m)
		{
			ResetShortState();
		}
	}

	private bool HasNegativeMacd()
	{
		foreach (var value in _macdHistory)
		{
			if (value < 0m)
				return true;
		}

		return false;
	}

	private bool HasPositiveMacd()
	{
		foreach (var value in _macdHistory)
		{
			if (value > 0m)
				return true;
		}

		return false;
	}

	private decimal GetIndentation()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return MinIndentation;

		var decimals = Security?.Decimals ?? 0;
		var factor = (decimals == 3 || decimals == 5) ? 10m : 1m;
		return MinIndentation * step * factor;
	}

	private void ResetLongState()
	{
		_longEntry = null;
		_longStop = null;
		_longStopMoved = false;
	}

	private void ResetShortState()
	{
		_shortEntry = null;
		_shortStop = null;
		_shortStopMoved = false;
	}
}
