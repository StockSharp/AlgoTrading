using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// HMA crossover strategy with RSI, Stochastic and trailing stop.
/// </summary>
public class HmaCrossoverRsiStochasticTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _fastHmaLength;
	private readonly StrategyParam<int> _slowHmaLength;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _stochSmooth;
	private readonly StrategyParam<decimal> _stochBuyLevel;
	private readonly StrategyParam<decimal> _stochSellLevel;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isInitialized;
	private bool _wasFastBelow;
	private decimal _trailPrice;
	private decimal _extremePrice;

	/// <summary>
	/// Fast HMA period.
	/// </summary>
	public int FastHmaLength
	{
		get => _fastHmaLength.Value;
		set => _fastHmaLength.Value = value;
	}

	/// <summary>
	/// Slow HMA period.
	/// </summary>
	public int SlowHmaLength
	{
		get => _slowHmaLength.Value;
		set => _slowHmaLength.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level for long entries.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// RSI level for short entries.
	/// </summary>
	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// Stochastic lookback period.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Stochastic smoothing for %K line.
	/// </summary>
	public int StochSmooth
	{
		get => _stochSmooth.Value;
		set => _stochSmooth.Value = value;
	}

	/// <summary>
	/// Stochastic level for long entries.
	/// </summary>
	public decimal StochBuyLevel
	{
		get => _stochBuyLevel.Value;
		set => _stochBuyLevel.Value = value;
	}

	/// <summary>
	/// Stochastic level for short entries.
	/// </summary>
	public decimal StochSellLevel
	{
		get => _stochSellLevel.Value;
		set => _stochSellLevel.Value = value;
	}

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingPercent
	{
		get => _trailingPercent.Value;
		set => _trailingPercent.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// Initializes a new instance of the strategy.
	/// </summary>
	public HmaCrossoverRsiStochasticTrailingStopStrategy()
	{
		_fastHmaLength = Param(nameof(FastHmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast HMA Length", "Period of the fast HMA", "Indicators")
			.SetCanOptimize(true);

		_slowHmaLength = Param(nameof(SlowHmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow HMA Length", "Period of the slow HMA", "Indicators")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetCanOptimize(true);

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 45m)
			.SetDisplay("RSI Buy Level", "RSI level for long entries", "Indicators")
			.SetCanOptimize(true);

		_rsiSellLevel = Param(nameof(RsiSellLevel), 60m)
			.SetDisplay("RSI Sell Level", "RSI level for short entries", "Indicators")
			.SetCanOptimize(true);

		_stochLength = Param(nameof(StochLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Lookback period for Stochastic", "Indicators")
			.SetCanOptimize(true);

		_stochSmooth = Param(nameof(StochSmooth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Smooth", "%K smoothing length", "Indicators")
			.SetCanOptimize(true);

		_stochBuyLevel = Param(nameof(StochBuyLevel), 39m)
			.SetDisplay("Stochastic Buy Level", "Stochastic level for long entries", "Indicators")
			.SetCanOptimize(true);

		_stochSellLevel = Param(nameof(StochSellLevel), 63m)
			.SetDisplay("Stochastic Sell Level", "Stochastic level for short entries", "Indicators")
			.SetCanOptimize(true);

		_trailingPercent = Param(nameof(TrailingPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing %", "Trailing stop percent", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_isInitialized = false;
		_wasFastBelow = false;
		_trailPrice = 0m;
		_extremePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastHma = new HullMovingAverage { Length = FastHmaLength };
		var slowHma = new HullMovingAverage { Length = SlowHmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stochastic = new StochasticOscillator
		{
			Length = StochLength,
			K = { Length = StochSmooth },
			D = { Length = 1 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastHma, slowHma, rsi, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastHma);
			DrawIndicator(area, slowHma);
			DrawIndicator(area, rsi);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue rsiValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k)
			return;

		if (!_isInitialized)
		{
			_wasFastBelow = fast < slow;
			_isInitialized = true;
			return;
		}

		var isFastBelow = fast < slow;
		var crossedUp = _wasFastBelow && fast > slow;
		var crossedDown = !_wasFastBelow && fast < slow;
		_wasFastBelow = isFastBelow;

		if (crossedUp && rsi < RsiBuyLevel && k < StochBuyLevel && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_extremePrice = candle.ClosePrice;
			_trailPrice = _extremePrice * (1 - TrailingPercent / 100m);
		}
		else if (crossedDown && rsi > RsiSellLevel && k > StochSellLevel && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_extremePrice = candle.ClosePrice;
			_trailPrice = _extremePrice * (1 + TrailingPercent / 100m);
		}

		if (Position > 0)
		{
			if (candle.ClosePrice > _extremePrice)
			{
				_extremePrice = candle.ClosePrice;
				_trailPrice = _extremePrice * (1 - TrailingPercent / 100m);
			}

			if (candle.LowPrice <= _trailPrice)
			{
				SellMarket(Math.Abs(Position));
				_trailPrice = 0m;
				_extremePrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice < _extremePrice || _extremePrice == 0m)
			{
				_extremePrice = candle.ClosePrice;
				_trailPrice = _extremePrice * (1 + TrailingPercent / 100m);
			}

			if (candle.HighPrice >= _trailPrice && _trailPrice != 0m)
			{
				BuyMarket(Math.Abs(Position));
				_trailPrice = 0m;
				_extremePrice = 0m;
			}
		}
	}
}
