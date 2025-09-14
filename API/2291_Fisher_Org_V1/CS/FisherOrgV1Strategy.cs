using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fisher Org v1 strategy.
/// Detects local minima and maxima of the Fisher Transform indicator.
/// Buys on local minima and sells on local maxima.
/// </summary>
public class FisherOrgV1Strategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private FisherTransform _fisher;
	private decimal _prev;
	private decimal _prevPrev;
	private int _valueCount;

	/// <summary>
	/// Fisher transform period length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FisherOrgV1Strategy"/>.
	/// </summary>
	public FisherOrgV1Strategy()
	{
		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fisher Length", "Period for Fisher Transform", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
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
		_prev = 0m;
		_prevPrev = 0m;
		_valueCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_fisher = new FisherTransform
		{
			Length = Length
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fisher, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fisher);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fisherValue)
	{
		if (candle.State != CandleStates.Finished || !_fisher.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_valueCount < 2)
		{
			if (_valueCount == 0)
				_prev = fisherValue;
			else
			{
				_prevPrev = _prev;
				_prev = fisherValue;
			}

			_valueCount++;
			return;
		}

		var isLongSignal = _prevPrev > _prev && _prev <= fisherValue;
		var isShortSignal = _prevPrev < _prev && _prev >= fisherValue;

		if (isLongSignal && Position <= 0)
			BuyMarket();

		if (isShortSignal && Position >= 0)
			SellMarket();

		_prevPrev = _prev;
		_prev = fisherValue;
	}
}
