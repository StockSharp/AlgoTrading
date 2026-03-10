using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Uses Awesome Oscillator filtered by SMA to follow trend direction.
/// Buys when filtered AO crosses above zero, sells when it crosses below zero.
/// </summary>
public class F2aAoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _filterLength;
	private decimal _previousAo = decimal.MinValue;
	private decimal _previousFilteredAo = decimal.MinValue;
	private int _barsSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int FilterLength { get => _filterLength.Value; set => _filterLength.Value = value; }

	public F2aAoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetDisplay("AO Fast", "Fast period for Awesome Oscillator", "Awesome Oscillator");

		_slowPeriod = Param(nameof(SlowPeriod), 34)
			.SetDisplay("AO Slow", "Slow period for Awesome Oscillator", "Awesome Oscillator");

		_filterLength = Param(nameof(FilterLength), 3)
			.SetDisplay("Filter", "SMA length for AO filter", "Awesome Oscillator");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousAo = decimal.MinValue;
		_previousFilteredAo = decimal.MinValue;
		_barsSinceTrade = 20;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousAo = decimal.MinValue;
		_previousFilteredAo = decimal.MinValue;
		_barsSinceTrade = 20;

		var ao = new AwesomeOscillator();
		ao.ShortMa.Length = FastPeriod;
		ao.LongMa.Length = SlowPeriod;

		var filter = new SimpleMovingAverage { Length = FilterLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ao, filter, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ao);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue, decimal filteredAo)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceTrade++;

		if (_previousAo == decimal.MinValue)
		{
			_previousAo = aoValue;
			_previousFilteredAo = filteredAo;
			return;
		}

		var crossedUp = _previousAo <= 0m && aoValue > 0m;
		var crossedDown = _previousAo >= 0m && aoValue < 0m;

		if (_barsSinceTrade >= 10 && filteredAo > _previousFilteredAo && crossedUp && Position <= 0)
		{
			BuyMarket();
			_barsSinceTrade = 0;
		}
		else if (_barsSinceTrade >= 10 && filteredAo < _previousFilteredAo && crossedDown && Position >= 0)
		{
			SellMarket();
			_barsSinceTrade = 0;
		}

		_previousAo = aoValue;
		_previousFilteredAo = filteredAo;
	}
}
