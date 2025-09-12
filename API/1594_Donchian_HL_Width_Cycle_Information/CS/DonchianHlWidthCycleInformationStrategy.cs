using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian HL Width Cycle Information strategy.
/// </summary>
public class DonchianHlWidthCycleInformationStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _meanSum;
	private int _barCount;

	private int _cycleCounter;
	private int _cycleTrend;
	private int _cycleCount;
	private decimal _avgCycleLength;
	private decimal _mean;

	/// <summary>
	/// Donchian channel period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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
	/// Initializes a new instance of <see cref="DonchianHlWidthCycleInformationStrategy"/>.
	/// </summary>
	public DonchianHlWidthCycleInformationStrategy()
	{
		_length = Param(nameof(Length), 28)
			.SetDisplay("Donchian Length", "Lookback for Donchian channel", "Donchian")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_meanSum = 0;
		_barCount = 0;
		_cycleCounter = 0;
		_cycleTrend = 1;
		_cycleCount = 0;
		_avgCycleLength = 0;
		_mean = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var donchian = new DonchianChannels
		{
			Length = Length
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dc = (DonchianChannelsValue)donchianValue;

		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower || upper == lower)
			return;

		var ph = 100m * (candle.HighPrice - upper) / (upper - lower);
		var pl = 100m * (candle.LowPrice - upper) / (upper - lower);
		var avg = (ph + pl) / 2m;

		_meanSum += avg;
		_barCount++;
		_mean = _meanSum / _barCount;

		_cycleCounter++;

		if (_cycleTrend < 0 && ph >= 0)
		{
			_avgCycleLength = (_avgCycleLength * _cycleCount + _cycleCounter) / (_cycleCount + 1);
			_cycleCounter = 0;
			_cycleTrend = 1;
			_cycleCount++;

			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_cycleTrend > 0 && pl <= -100m)
		{
			_avgCycleLength = (_avgCycleLength * _cycleCount + _cycleCounter) / (_cycleCount + 1);
			_cycleCounter = 0;
			_cycleTrend = -1;
			_cycleCount++;

			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
	}
}
