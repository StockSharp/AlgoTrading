using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume delta SMA strategy with 1-year high/low thresholds.
/// Buys when delta SMA was very low and crosses above zero.
/// Exits when delta SMA drops below 60% of its 1-year high after a 70% cross.
/// </summary>
public class DeltaSma1YearHighLowStrategy : Strategy
{
	private const int LookbackBars = 365;

	private readonly StrategyParam<int> _deltaSmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _deltaSma;
	private Highest _highest;
	private Lowest _lowest;

	private bool _wasVeryLow;
	private bool _crossedAbove70;
	private decimal _previousDeltaSma;

	/// <summary>
	/// Delta SMA period length.
	/// </summary>
	public int DeltaSmaLength
	{
		get => _deltaSmaLength.Value;
		set => _deltaSmaLength.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="DeltaSma1YearHighLowStrategy"/>.
	/// </summary>
	public DeltaSma1YearHighLowStrategy()
	{
		_deltaSmaLength = Param(nameof(DeltaSmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Delta SMA Length", "Period for delta SMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

		_deltaSma = default;
		_highest = default;
		_lowest = default;
		_wasVeryLow = false;
		_crossedAbove70 = false;
		_previousDeltaSma = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_deltaSma = new SMA { Length = DeltaSmaLength };
		_highest = new Highest { Length = LookbackBars };
		_lowest = new Lowest { Length = LookbackBars };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _deltaSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volume = candle.TotalVolume ?? 0m;
		var delta = 0m;
		if (candle.ClosePrice > candle.OpenPrice)
			delta = volume;
		else if (candle.ClosePrice < candle.OpenPrice)
			delta = -volume;

		var deltaSmaValue = _deltaSma.Process(new DecimalIndicatorValue(_deltaSma, delta, candle.ServerTime)).ToDecimal();
		var highestValue = _highest.Process(new DecimalIndicatorValue(_highest, deltaSmaValue, candle.ServerTime)).ToDecimal();
		var lowestValue = _lowest.Process(new DecimalIndicatorValue(_lowest, deltaSmaValue, candle.ServerTime)).ToDecimal();

		if (!_deltaSma.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
		{
			_previousDeltaSma = deltaSmaValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousDeltaSma = deltaSmaValue;
			return;
		}

		var veryLowThreshold = lowestValue * 0.7m;
		var above70Threshold = highestValue * 0.9m;
		var below60Threshold = highestValue * 0.5m;

		if (deltaSmaValue < veryLowThreshold)
			_wasVeryLow = true;

		var crossedAboveZero = _previousDeltaSma <= 0 && deltaSmaValue > 0;
		if (crossedAboveZero)
		{
			if (_wasVeryLow && Position <= 0)
				BuyMarket();

			_wasVeryLow = false;
		}

		var crossedAbove70 = _previousDeltaSma <= above70Threshold && deltaSmaValue > above70Threshold;
		if (crossedAbove70)
			_crossedAbove70 = true;

		if (_crossedAbove70 && deltaSmaValue < below60Threshold * 0.5m)
			_crossedAbove70 = false;

		if (_crossedAbove70 && deltaSmaValue < below60Threshold && Position > 0)
			SellMarket(Math.Abs(Position));

		_previousDeltaSma = deltaSmaValue;
	}
}
