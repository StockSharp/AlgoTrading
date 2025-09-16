using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a double smoothed Detrended Price Oscillator.
/// </summary>
public class ColorXXDPOStrategy : Strategy
{
	private readonly StrategyParam<int> _firstLength;
	private readonly StrategyParam<int> _secondLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SimpleMovingAverage _ma1 = new();
	private readonly SimpleMovingAverage _ma2 = new();

	private decimal _prev1;
	private decimal _prev2;
	private bool _isInitialized;

	public ColorXXDPOStrategy()
	{
		_firstLength = Param(nameof(FirstLength), 21)
			.SetDisplay("First MA Length", "Length for the first smoothing stage.", "Indicators");

		_secondLength = Param(nameof(SecondLength), 5)
			.SetDisplay("Second MA Length", "Length for the second smoothing stage.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation.", "General");
	}

	public int FirstLength
	{
		get => _firstLength.Value;
		set => _firstLength.Value = value;
	}

	public int SecondLength
	{
		get => _secondLength.Value;
		set => _secondLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ma1.Reset();
		_ma2.Reset();
		_prev1 = 0m;
		_prev2 = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma1.Length = FirstLength;
		_ma2.Length = SecondLength;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var ma1 = _ma1.Process(candle.ClosePrice).ToDecimal();
		var dpo = candle.ClosePrice - ma1;
		var xxdpo = _ma2.Process(dpo).ToDecimal();

		if (!_isInitialized)
		{
			_prev2 = xxdpo;
			_prev1 = xxdpo;
			_isInitialized = true;
			return;
		}

		var slopeUp = _prev1 < _prev2;
		var slopeDown = _prev1 > _prev2;

		if (slopeUp && Position < 0)
			BuyMarket(Math.Abs(Position));

		if (slopeDown && Position > 0)
			SellMarket(Math.Abs(Position));

		if (slopeUp && xxdpo > _prev1 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (slopeDown && xxdpo < _prev1 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prev2 = _prev1;
		_prev1 = xxdpo;
	}
}
