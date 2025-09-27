using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demo GPT - Day Trading Scalping strategy.
/// </summary>
public class DemoGptDayTradingScalpingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private ICandleMessage _prevCandle;
	private decimal _avgVolume;
	private int _volumeCounter;

	private decimal _highLevel;
	private decimal _lowLevel;
	private decimal _s2;
	private decimal _s3;
	private decimal _r2;
	private decimal _r3;


	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume average period.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	/// <summary>
	/// Start date for trading.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date for trading.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DemoGptDayTradingScalpingStrategy"/>.
	/// </summary>
	public DemoGptDayTradingScalpingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume SMA Period", "Period for volume average", "Parameters");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Start date", "Parameters");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2069, 12, 31, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "End date", "Parameters");
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

		_prevCandle = null;
		_avgVolume = 0m;
		_volumeCounter = 0;
		_highLevel = 0m;
		_lowLevel = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema20 = new EMA { Length = 20 };
		var vwap = new VWAP();
		var highest = new Highest { Length = 14 };
		var lowest = new Lowest { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema20, vwap, ProcessCandle)
			.Start();

		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame())
			.Bind(highest, lowest, ProcessDaily)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema20);
			DrawIndicator(area, vwap);
		}
	}

	private void ProcessDaily(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highLevel = high;
		_lowLevel = low;
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema20, decimal vwap)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleTime = candle.OpenTime;
		if (candleTime < StartDate || candleTime > EndDate)
		{
			if (Position != 0)
				SellMarket(Position);
			return;
		}

		var currentVolume = candle.TotalVolume;
		if (_volumeCounter < VolumeAvgPeriod)
		{
			_volumeCounter++;
			_avgVolume = ((_avgVolume * (_volumeCounter - 1)) + currentVolume) / _volumeCounter;
		}
		else
		{
			_avgVolume = (_avgVolume * (VolumeAvgPeriod - 1) + currentVolume) / VolumeAvgPeriod;
		}

		var goodVolume = currentVolume > _avgVolume;
		var range = _highLevel - _lowLevel;
		_s2 = _lowLevel + range * 1.5m / 2m;
		_s3 = _lowLevel + range * 1.1m / 2m;
		_r2 = _highLevel - range * 1.5m / 2m;
		_r3 = _highLevel - range * 1.1m / 2m;


		if (_prevCandle != null && goodVolume)
		{
			var greenCandle1 = _prevCandle.ClosePrice > _prevCandle.OpenPrice;
			var greenCandle2 = candle.ClosePrice > candle.OpenPrice;
			var redCandle1 = _prevCandle.ClosePrice < _prevCandle.OpenPrice;
			var redCandle2 = candle.ClosePrice < candle.OpenPrice;

			var buyCondition = greenCandle1 && greenCandle2 && candle.OpenPrice > _prevCandle.ClosePrice;
			var sellCondition = redCandle1 && redCandle2 && candle.OpenPrice < _prevCandle.ClosePrice;

			if (buyCondition && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (sellCondition && Position > 0)
			{
				SellMarket(Position);
			}
		}

		_prevCandle = candle;
	}
}
