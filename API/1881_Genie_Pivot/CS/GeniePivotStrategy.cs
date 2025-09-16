using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Genie pivot reversal strategy.
/// Enters long after a sequence of falling lows followed by an upside breakout.
/// Enters short after a sequence of rising highs followed by a downside breakout.
/// Applies trailing stop and take-profit in absolute price units.
/// </summary>
public class GeniePivotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _lows = new decimal[8];
	private readonly decimal[] _highs = new decimal[8];
	private int _stored;

	/// <summary>
	/// Take-profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public GeniePivotStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Target profit in price units", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 100m);

		_trailingStop = Param(nameof(TrailingStop), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in price units", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 500m, 50m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
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
		_stored = 0;
		Array.Clear(_lows);
		Array.Clear(_highs);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(TrailingStop, UnitTypes.Absolute),
			isStopTrailing: true);

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		for (var i = 7; i > 0; i--)
		{
			_lows[i] = _lows[i - 1];
			_highs[i] = _highs[i - 1];
		}

		_lows[0] = candle.LowPrice;
		_highs[0] = candle.HighPrice;
		if (_stored < 8)
			_stored++;

		if (_stored < 8)
			return;

		var buySeq = true;
		for (var i = 7; i >= 2; i--)
		{
			if (_lows[i] <= _lows[i - 1])
			{
				buySeq = false;
				break;
			}
		}

		if (buySeq && _lows[1] < _lows[0] && _highs[1] < candle.ClosePrice)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			return;
		}

		var sellSeq = true;
		for (var i = 7; i >= 2; i--)
		{
			if (_highs[i] >= _highs[i - 1])
			{
				sellSeq = false;
				break;
			}
		}

		if (sellSeq && _highs[1] > _highs[0] && _lows[1] > candle.ClosePrice)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
	}
}
