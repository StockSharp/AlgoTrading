using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fisher transform strategy with level-based signals.
/// Opens long positions when Fisher rises above a negative threshold
/// and opens short positions when Fisher falls below a positive threshold.
/// </summary>
public class FisherOrgSignStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private FisherTransform _fisher;
	private decimal _prevFisher;

	/// <summary>
	/// Fisher transform period length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Upper level used to generate sell signals.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower level used to generate buy signals.
	/// </summary>
	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	/// <summary>
	/// Type of candles for indicator calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FisherOrgSignStrategy"/>.
	/// </summary>
	public FisherOrgSignStrategy()
	{
		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fisher Length", "Period for Fisher Transform", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_upLevel = Param(nameof(UpLevel), 1.5m)
			.SetDisplay("Upper Level", "Sell signal level", "General");

		_downLevel = Param(nameof(DownLevel), -1.5m)
			.SetDisplay("Lower Level", "Buy signal level", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

		var longCondition = _prevFisher <= DownLevel && fisherValue > DownLevel;
		var shortCondition = _prevFisher >= UpLevel && fisherValue < UpLevel;

		if (longCondition && Position <= 0)
		{
			var volume = Volume;
			if (Position < 0)
				volume *= 2;
			BuyMarket(volume);
		}

		if (shortCondition && Position >= 0)
		{
			var volume = Volume;
			if (Position > 0)
				volume *= 2;
			SellMarket(volume);
		}

		_prevFisher = fisherValue;
	}
}
