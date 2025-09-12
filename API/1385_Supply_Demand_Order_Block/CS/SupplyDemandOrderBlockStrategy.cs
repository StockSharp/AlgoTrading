using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supply and demand breakout strategy with trailing stop.
/// </summary>
public class SupplyDemandOrderBlockStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _trailingStartTicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _slLevel;
	private decimal? _trailingStart;
	private decimal? _trailingSl;

	/// <summary>
	/// Period for support and resistance levels.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Trailing start in ticks.
	/// </summary>
	public int TrailingStartTicks
	{
		get => _trailingStartTicks.Value;
		set => _trailingStartTicks.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the strategy.
	/// </summary>
	public SupplyDemandOrderBlockStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetDisplay("Length", "Lookback period for zones", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
			.SetDisplay("SL Ticks", "Stop loss in ticks", "General")
			.SetCanOptimize(true)
			.SetOptimize(500, 2000, 500);

		_trailingStartTicks = Param(nameof(TrailingStartTicks), 2000)
			.SetDisplay("Trail Start", "Trailing start in ticks", "General")
			.SetCanOptimize(true)
			.SetOptimize(1000, 4000, 500);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_slLevel = null;
		_trailingStart = null;
		_trailingSl = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var donchian = new DonchianChannel { Length = Length };
		var ema = new ExponentialMovingAverage { Length = 50 };
		var volumeSma = new SimpleMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(donchian, ema, volumeSma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal upper, decimal lower, decimal emaValue, decimal volumeAvg)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trendUp = candle.ClosePrice > emaValue;
		var trendDown = candle.ClosePrice < emaValue;
		var volumeSpike = candle.TotalVolume > volumeAvg * 1.5m;

		var longBreakout = candle.LowPrice <= lower && candle.ClosePrice > lower && trendUp && volumeSpike;
		var shortBreakout = candle.HighPrice >= upper && candle.ClosePrice < upper && trendDown && volumeSpike;

		if (longBreakout)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_slLevel = candle.ClosePrice - StopLossTicks * Security.PriceStep;
			_trailingStart = candle.ClosePrice + TrailingStartTicks * Security.PriceStep;
			_trailingSl = null;
		}
		else if (shortBreakout)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_slLevel = candle.ClosePrice + StopLossTicks * Security.PriceStep;
			_trailingStart = candle.ClosePrice - TrailingStartTicks * Security.PriceStep;
			_trailingSl = null;
		}

		if (Position > 0)
		{
			if (_trailingStart != null && candle.ClosePrice > _trailingStart)
				_trailingSl = Math.Max(_trailingSl ?? (candle.ClosePrice - StopLossTicks * Security.PriceStep), candle.ClosePrice - StopLossTicks * Security.PriceStep);

			if ((_slLevel != null && candle.LowPrice <= _slLevel) || (_trailingSl != null && candle.LowPrice <= _trailingSl))
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (_trailingStart != null && candle.ClosePrice < _trailingStart)
				_trailingSl = Math.Min(_trailingSl ?? (candle.ClosePrice + StopLossTicks * Security.PriceStep), candle.ClosePrice + StopLossTicks * Security.PriceStep);

			if ((_slLevel != null && candle.HighPrice >= _slLevel) || (_trailingSl != null && candle.HighPrice >= _trailingSl))
				BuyMarket(Math.Abs(Position));
		}
	}
}
