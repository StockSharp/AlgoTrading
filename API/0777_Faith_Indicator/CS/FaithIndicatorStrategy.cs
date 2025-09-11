using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Faith Indicator strategy.
/// Uses volume expansion and price movement to gauge market dominance.
/// </summary>
public class FaithIndicatorStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _volumeHighest;
	private SMA _maUp;
	private SMA _maDown;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevVolume;
	private decimal _prevDif;

	/// <summary>
	/// Period for averaging.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
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
	/// Initializes a new instance of <see cref="FaithIndicatorStrategy"/>.
	/// </summary>
	public FaithIndicatorStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Averaging Period", "Periods for averaging volume", "Faith");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_prevHigh = 0;
		_prevLow = 0;
		_prevVolume = 0;
		_prevDif = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_volumeHighest = new Highest { Length = 30 };
		_maUp = new SMA { Length = Period };
		_maDown = new SMA { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highestValue = _volumeHighest.Process(candle.TotalVolume);
		if (!highestValue.IsFinal)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevVolume = candle.TotalVolume;
			return;
		}

		var highestVolume = (decimal)highestValue;
		if (highestVolume == 0)
			return;

		var vproc = candle.TotalVolume / highestVolume * 200m;

		var up = candle.HighPrice > _prevHigh && candle.TotalVolume > _prevVolume;
		var down = candle.LowPrice < _prevLow && candle.TotalVolume > _prevVolume;

		var volUp = up ? vproc : 0m;
		var volDown = down ? vproc : 0m;

		var maUpValue = _maUp.Process(volUp);
		var maDownValue = _maDown.Process(volDown);

		if (!_maUp.IsFormed || !_maDown.IsFormed)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevVolume = candle.TotalVolume;
			return;
		}

		var maup = (decimal)maUpValue;
		var madown = (decimal)maDownValue;

		var difVol = maup - madown;

		var dif = 0m;
		if (difVol > 60m)
			dif += 8m;
		else if (difVol > 40m)
			dif += 6m;
		else if (difVol > 20m)
			dif += 4m;
		else if (difVol > 10m)
			dif += 2m;
		else if (difVol > 0m)
			dif += 1m;

		if (difVol < -60m)
			dif -= 8m;
		else if (difVol < -40m)
			dif -= 6m;
		else if (difVol < -20m)
			dif -= 4m;
		else if (difVol < -10m)
			dif -= 2m;
		else if (difVol < 0m)
			dif -= 1m;

		if (_prevDif <= 0m && dif > 0m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_prevDif >= 0m && dif < 0m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevDif = dif;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevVolume = candle.TotalVolume;
	}
}
