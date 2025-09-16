namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Currencyprofits strategy that trades trend pullbacks into the recent channel extremes.
/// </summary>
public class CurrencyprofitsHighLowChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<CandlePrice> _priceSource;
	private readonly StrategyParam<MovingAverageTypeEnum> _fastMaType;
	private readonly StrategyParam<MovingAverageTypeEnum> _slowMaType;

	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal? _previousHighest;
	private decimal? _previousLowest;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private int _processedCandles;

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public CandlePrice PriceSource
	{
		get => _priceSource.Value;
		set => _priceSource.Value = value;
	}

	public MovingAverageTypeEnum FastMaType
	{
		get => _fastMaType.Value;
		set => _fastMaType.Value = value;
	}

	public MovingAverageTypeEnum SlowMaType
	{
		get => _slowMaType.Value;
		set => _slowMaType.Value = value;
	}

	private int RequiredBars => Math.Max(Math.Max(FastLength, SlowLength), ChannelLength) + 1;

	public CurrencyprofitsHighLowChannelStrategy()
	{
		_fastLength = Param(nameof(FastLength), 32)
			.SetDisplay("Fast MA Length", "Length of the fast moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 120, 2);

		_slowLength = Param(nameof(SlowLength), 86)
			.SetDisplay("Slow MA Length", "Length of the slow moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 2);

		_channelLength = Param(nameof(ChannelLength), 6)
			.SetDisplay("Channel Lookback", "Number of previous candles for high/low channel", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_stopLossPoints = Param(nameof(StopLossPoints), 170m)
			.SetDisplay("Stop Loss (points)", "Distance to stop loss expressed in price steps", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 0.14m)
			.SetDisplay("Risk Fraction", "Fraction of portfolio capital risked per trade", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for calculations", "General");

		_priceSource = Param(nameof(PriceSource), CandlePrice.Close)
			.SetDisplay("MA Price Source", "Price source used by both moving averages", "Indicators");

		_fastMaType = Param(nameof(FastMaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("Fast MA Type", "Moving average algorithm for the fast line", "Indicators");

		_slowMaType = Param(nameof(SlowMaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("Slow MA Type", "Moving average algorithm for the slow line", "Indicators");
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

		_previousFast = null;
		_previousSlow = null;
		_previousHighest = null;
		_previousLowest = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_processedCandles = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = CreateMovingAverage(FastMaType, FastLength, PriceSource);
		var slowMa = CreateMovingAverage(SlowMaType, SlowLength, PriceSource);
		var highest = new Highest { Length = ChannelLength, CandlePrice = CandlePrice.High };
		var lowest = new Lowest { Length = ChannelLength, CandlePrice = CandlePrice.Low };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal channelHigh, decimal channelLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_processedCandles++;

		if (_processedCandles <= RequiredBars)
		{
			// Collect enough history before taking any decisions.
			_previousFast = fast;
			_previousSlow = slow;
			_previousHighest = channelHigh;
			_previousLowest = channelLow;
			return;
		}

		if (_previousFast is null || _previousSlow is null || _previousHighest is null || _previousLowest is null)
		{
			_previousFast = fast;
			_previousSlow = slow;
			_previousHighest = channelHigh;
			_previousLowest = channelLow;
			return;
		}

		if (Position > 0)
		{
			// Exit long trades when price breaks the opposite channel or the protective stop.
			var exitByChannel = candle.ClosePrice >= _previousHighest.Value;
			var exitByStop = _stopPrice > 0m && candle.LowPrice <= _stopPrice;

			if (exitByChannel || exitByStop)
			{
				ClosePosition();
				ResetTradeState();
			}
		}
		else if (Position < 0)
		{
			// Exit short trades when price hits the lower boundary or the stop.
			var exitByChannel = candle.ClosePrice <= _previousLowest.Value;
			var exitByStop = _stopPrice > 0m && candle.HighPrice >= _stopPrice;

			if (exitByChannel || exitByStop)
			{
				ClosePosition();
				ResetTradeState();
			}
		}
		else if (IsFormedAndOnlineAndAllowTrading())
		{
			var stopDistance = GetStopDistance();

			if (stopDistance > 0m)
			{
				// Long entries require a bullish trend and a pullback to the recent low channel.
				if (_previousFast.Value > _previousSlow.Value && candle.LowPrice <= _previousLowest.Value)
				{
					var volume = CalculatePositionSize(stopDistance);

					if (volume > 0m)
					{
						BuyMarket(volume);
						_entryPrice = candle.ClosePrice;
						_stopPrice = _entryPrice - stopDistance;
					}
				}
				// Short entries require a bearish trend and a retest of the recent high channel.
				else if (_previousFast.Value < _previousSlow.Value && candle.HighPrice >= _previousHighest.Value)
				{
					var volume = CalculatePositionSize(stopDistance);

					if (volume > 0m)
					{
						SellMarket(volume);
						_entryPrice = candle.ClosePrice;
						_stopPrice = _entryPrice + stopDistance;
					}
				}
			}
		}

		_previousFast = fast;
		_previousSlow = slow;
		_previousHighest = channelHigh;
		_previousLowest = channelLow;
	}

	private decimal GetStopDistance()
	{
		if (StopLossPoints <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep > 0m)
			return StopLossPoints * priceStep;

		return StopLossPoints;
	}

	private decimal CalculatePositionSize(decimal stopDistance)
	{
		var defaultVolume = AdjustVolume(Volume);

		if (stopDistance <= 0m)
			return defaultVolume;

		var portfolioValue = Portfolio?.CurrentValue;

		if (portfolioValue is null || portfolioValue <= 0m || RiskPercent <= 0m)
			return defaultVolume;

		var riskCapital = portfolioValue.Value * RiskPercent;
		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		decimal riskPerContract;

		if (priceStep > 0m && stepPrice > 0m)
		{
			// Convert the stop distance into cash risk per contract using exchange specifications.
			riskPerContract = stopDistance / priceStep * stepPrice;
		}
		else
		{
			// Fallback when the security does not expose step metadata.
			riskPerContract = stopDistance;
		}

		if (riskPerContract <= 0m)
			return defaultVolume;

		var desiredVolume = riskCapital / riskPerContract;
		return AdjustVolume(desiredVolume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;

		if (step > 0m)
		{
			var steps = decimal.Floor(volume / step);
			volume = steps * step;
		}

		var minVolume = Security?.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security?.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private void ResetTradeState()
	{
		// Clear cached execution details after a position has been closed.
		_entryPrice = 0m;
		_stopPrice = 0m;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageTypeEnum type, int length, CandlePrice price)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length, CandlePrice = price },
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length, CandlePrice = price },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length, CandlePrice = price },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length, CandlePrice = price },
			_ => new SimpleMovingAverage { Length = length, CandlePrice = price },
		};
	}

	public enum MovingAverageTypeEnum
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
	}
}
