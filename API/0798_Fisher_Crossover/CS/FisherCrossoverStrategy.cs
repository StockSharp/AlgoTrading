using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fisher crossover strategy.
/// Buys when Fisher Transform crosses above its previous value below 1.
/// Closes position when Fisher crosses below its previous value above 1.
/// </summary>
public class FisherCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private FisherTransform _fisher;
	private decimal _prevFisher;
	private decimal _prevPrevFisher;
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
	/// Initializes a new instance of <see cref="FisherCrossoverStrategy"/>.
	/// </summary>
	public FisherCrossoverStrategy()
	{
		_length = Param(nameof(Length), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fisher Length", "Period for Fisher Transform", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

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
		_prevFisher = 0m;
		_prevPrevFisher = 0m;
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
				_prevFisher = fisherValue;
			else
			{
				_prevPrevFisher = _prevFisher;
				_prevFisher = fisherValue;
			}

			_valueCount++;
			return;
		}

		var trigger = _prevFisher;

		var longCondition = _prevFisher <= _prevPrevFisher && fisherValue > trigger && fisherValue < 1m;
		var exitCondition = _prevFisher >= _prevPrevFisher && fisherValue < trigger && fisherValue > 1m;

		if (longCondition && Position == 0)
			BuyMarket();

		if (exitCondition && Position > 0)
			SellMarket();

		_prevPrevFisher = _prevFisher;
		_prevFisher = fisherValue;
	}
}
