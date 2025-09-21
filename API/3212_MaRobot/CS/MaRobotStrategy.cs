namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Moving average crossover strategy with daily ADX/RSI filters.
/// </summary>
public class MaRobotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _fastPeriodParam;
	private readonly StrategyParam<int> _slowPeriodParam;
	private readonly StrategyParam<decimal> _adxThresholdParam;
	private readonly StrategyParam<decimal> _rsiThresholdParam;
	private readonly StrategyParam<decimal> _takeProfitRatioParam;
	private readonly StrategyParam<int> _stopLossPointsParam;
	private readonly StrategyParam<decimal> _protectThresholdParam;
	private readonly StrategyParam<int> _backCloseParam;
	private readonly StrategyParam<int> _dailyAdxPeriodParam;
	private readonly StrategyParam<int> _dailyRsiPeriodParam;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private Lowest _lowestLow = null!;
	private Highest _highestHigh = null!;
	private AverageDirectionalIndex _dailyAdx = null!;
	private RelativeStrengthIndex _dailyRsi = null!;

	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal? _latestDailyAdx;
	private decimal? _latestDailyRsi;
	private decimal? _stopLossLevel;
	private decimal? _protectStopPrice;
	private decimal? _entryPrice;
	private decimal _pointSize;

	public MaRobotStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the crossover", "General");

		_fastPeriodParam = Param(nameof(FastPeriod), 10)
			.SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
			.SetGreaterThanZero();

		_slowPeriodParam = Param(nameof(SlowPeriod), 23)
			.SetDisplay("Slow MA Period", "Length of the slow simple moving average", "Indicators")
			.SetGreaterThanZero();

		_adxThresholdParam = Param(nameof(AdxThreshold), 30m)
			.SetDisplay("ADX Threshold", "Maximum allowed daily ADX value", "Filters")
			.SetNotNegative();

		_rsiThresholdParam = Param(nameof(RsiThreshold), 38m)
			.SetDisplay("RSI Threshold", "Daily RSI level for longs; mirrored for shorts", "Filters")
			.SetNotNegative();

		_takeProfitRatioParam = Param(nameof(TakeProfitRatio), 0.038m)
			.SetDisplay("Take Profit Ratio", "Fractional profit target relative to entry price", "Risk")
			.SetNotNegative();

		_stopLossPointsParam = Param(nameof(StopLossPoints), 10)
			.SetDisplay("Protective Stop Points", "Distance for the protective stop in price points", "Risk")
			.SetNotNegative();

		_protectThresholdParam = Param(nameof(ProtectThreshold), 0.001m)
			.SetDisplay("Protect Threshold", "Minimum profit ratio before arming the protective stop", "Risk")
			.SetNotNegative();

		_backCloseParam = Param(nameof(BackClose), 12)
			.SetDisplay("Swing Lookback", "Number of candles for swing high/low stop selection", "Risk")
			.SetGreaterThanZero();

		_dailyAdxPeriodParam = Param(nameof(DailyAdxPeriod), 14)
			.SetDisplay("Daily ADX Period", "Length of the daily Average Directional Index", "Filters")
			.SetGreaterThanZero();

		_dailyRsiPeriodParam = Param(nameof(DailyRsiPeriod), 14)
			.SetDisplay("Daily RSI Period", "Length of the daily Relative Strength Index", "Filters")
			.SetGreaterThanZero();
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriodParam.Value;
		set => _fastPeriodParam.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriodParam.Value;
		set => _slowPeriodParam.Value = value;
	}

	public decimal AdxThreshold
	{
		get => _adxThresholdParam.Value;
		set => _adxThresholdParam.Value = value;
	}

	public decimal RsiThreshold
	{
		get => _rsiThresholdParam.Value;
		set => _rsiThresholdParam.Value = value;
	}

	public decimal TakeProfitRatio
	{
		get => _takeProfitRatioParam.Value;
		set => _takeProfitRatioParam.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPointsParam.Value;
		set => _stopLossPointsParam.Value = value;
	}

	public decimal ProtectThreshold
	{
		get => _protectThresholdParam.Value;
		set => _protectThresholdParam.Value = value;
	}

	public int BackClose
	{
		get => _backCloseParam.Value;
		set => _backCloseParam.Value = value;
	}

	public int DailyAdxPeriod
	{
		get => _dailyAdxPeriodParam.Value;
		set => _dailyAdxPeriodParam.Value = value;
	}

	public int DailyRsiPeriod
	{
		get => _dailyRsiPeriodParam.Value;
		set => _dailyRsiPeriodParam.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousFast = null;
		_previousSlow = null;
		_latestDailyAdx = null;
		_latestDailyRsi = null;
		_stopLossLevel = null;
		_protectStopPrice = null;
		_entryPrice = null;
		_pointSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = Security?.PriceStep ?? 0m;

		_fastMa = new SimpleMovingAverage { Length = FastPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowPeriod };
		_lowestLow = new Lowest { Length = BackClose, CandlePrice = CandlePrice.Low };
		_highestHigh = new Highest { Length = BackClose, CandlePrice = CandlePrice.High };

		_dailyAdx = new AverageDirectionalIndex { Length = DailyAdxPeriod };
		_dailyRsi = new RelativeStrengthIndex { Length = DailyRsiPeriod };

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_fastMa, _slowMa, _lowestLow, _highestHigh, ProcessMainCandle)
			.Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription
			.Bind(_dailyAdx, _dailyRsi, ProcessDailyCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle, decimal adxValue, decimal rsiValue)
	{
		// Store daily indicator values only when the bar is finished to match the MT4 behaviour.
		if (candle.State != CandleStates.Finished)
		return;

		_latestDailyAdx = adxValue;
		_latestDailyRsi = rsiValue;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal lowestValue, decimal highestValue)
	{
		// Trade decisions are made on completed candles only, just like the original EA.
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
		_previousFast = fastValue;
		_previousSlow = slowValue;
		return;
		}

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_lowestLow.IsFormed || !_highestHigh.IsFormed)
		{
		_previousFast = fastValue;
		_previousSlow = slowValue;
		return;
		}

		if (_latestDailyAdx is null || _latestDailyRsi is null)
		{
		_previousFast = fastValue;
		_previousSlow = slowValue;
		return;
		}

		if (Position > 0m)
		{
		ManageLongPosition(candle, fastValue, slowValue);
		}
		else if (Position < 0m)
		{
		ManageShortPosition(candle, fastValue, slowValue);
		}
		else if (_latestDailyAdx <= AdxThreshold)
		{
		TryOpenPositions(candle, fastValue, slowValue, lowestValue, highestValue);
		}

		_previousFast = fastValue;
		_previousSlow = slowValue;
	}

	private void TryOpenPositions(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal lowestValue, decimal highestValue)
	{
		var rsi = _latestDailyRsi ?? 0m;

		// Long entry: fast MA crosses above slow MA and daily RSI shows an oversold condition.
		if (_previousFast is decimal prevFastLong && prevFastLong < slowValue && fastValue > slowValue && rsi < RsiThreshold)
		{
		_entryPrice = candle.ClosePrice;
		_stopLossLevel = lowestValue;
		_protectStopPrice = null;
		BuyMarket(Volume);
		return;
		}

		// Short entry: fast MA crosses below slow MA and daily RSI is above the mirrored threshold.
		if (_previousFast is decimal prevFastShort && prevFastShort > slowValue && fastValue < slowValue && rsi > 100m - RsiThreshold)
		{
		_entryPrice = candle.ClosePrice;
		_stopLossLevel = highestValue;
		_protectStopPrice = null;
		SellMarket(Volume);
		}
	}

	private void ManageLongPosition(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		var entryPrice = _entryPrice ?? candle.ClosePrice;
		var closePrice = candle.ClosePrice;

		// Calculate the current profit ratio relative to the entry price.
		var profitRatio = entryPrice != 0m ? (closePrice - entryPrice) / entryPrice : 0m;

		if (TakeProfitRatio > 0m && profitRatio >= TakeProfitRatio)
		{
		SellMarket(Position);
		return;
		}

		if (_stopLossLevel is decimal stopLoss && closePrice <= stopLoss)
		{
		SellMarket(Position);
		return;
		}

		if (_previousFast is decimal prevFast && prevFast > slowValue && fastValue < slowValue)
		{
		SellMarket(Position);
		return;
		}

		if (_protectStopPrice is decimal protectPrice)
		{
		if (closePrice <= protectPrice)
		{
		SellMarket(Position);
		}
		}
		else if (ProtectThreshold > 0m && profitRatio > ProtectThreshold && StopLossPoints > 0 && _pointSize > 0m)
		{
		var newStop = RoundPrice(entryPrice + StopLossPoints * _pointSize);
		if (newStop < closePrice)
		_protectStopPrice = newStop;
		}
	}

	private void ManageShortPosition(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		var entryPrice = _entryPrice ?? candle.ClosePrice;
		var closePrice = candle.ClosePrice;

		var profitRatio = entryPrice != 0m ? (entryPrice - closePrice) / entryPrice : 0m;

		if (TakeProfitRatio > 0m && profitRatio >= TakeProfitRatio)
		{
		BuyMarket(Math.Abs(Position));
		return;
		}

		if (_stopLossLevel is decimal stopLoss && closePrice >= stopLoss)
		{
		BuyMarket(Math.Abs(Position));
		return;
		}

		if (_previousFast is decimal prevFast && prevFast < slowValue && fastValue > slowValue)
		{
		BuyMarket(Math.Abs(Position));
		return;
		}

		if (_protectStopPrice is decimal protectPrice)
		{
		if (closePrice >= protectPrice)
		{
		BuyMarket(Math.Abs(Position));
		}
		}
		else if (ProtectThreshold > 0m && profitRatio > ProtectThreshold && StopLossPoints > 0 && _pointSize > 0m)
		{
		var newStop = RoundPrice(entryPrice - StopLossPoints * _pointSize);
		if (newStop > closePrice)
		_protectStopPrice = newStop;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
		_stopLossLevel = null;
		_protectStopPrice = null;
		_entryPrice = null;
		}
	}

	private decimal RoundPrice(decimal price)
	{
		// Align price levels with the instrument tick size before using them in risk logic.
		var step = _pointSize;
		if (step <= 0m)
		return price;

		var ratio = price / step;
		var rounded = Math.Round(ratio, MidpointRounding.AwayFromZero);
		return rounded * step;
	}
}
