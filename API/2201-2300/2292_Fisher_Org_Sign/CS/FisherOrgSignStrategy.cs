using System;
using System.Collections.Generic;

using Ecng.Common;

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

	private EhlersFisherTransform _fisher;
	private decimal? _prevFisher;

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public FisherOrgSignStrategy()
	{
		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fisher Length", "Period for Fisher Transform", "General")
			.SetOptimize(5, 20, 1);

		_upLevel = Param(nameof(UpLevel), 0.1m)
			.SetDisplay("Upper Level", "Sell signal level", "General");

		_downLevel = Param(nameof(DownLevel), -0.1m)
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
		_prevFisher = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFisher = null;

		_fisher = new EhlersFisherTransform
		{
			Length = Length
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_fisher, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fisher);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fisherVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (fisherVal is not IEhlersFisherTransformValue typed || typed.MainLine is not decimal fisherValue)
			return;

		if (_prevFisher is null)
		{
			_prevFisher = fisherValue;
			return;
		}

		var longCondition = _prevFisher <= DownLevel && fisherValue > DownLevel;
		var shortCondition = _prevFisher >= UpLevel && fisherValue < UpLevel;

		if (longCondition && Position <= 0)
			BuyMarket();

		if (shortCondition && Position >= 0)
			SellMarket();

		_prevFisher = fisherValue;
	}
}
