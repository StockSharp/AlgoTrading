using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grease trap strategy.
/// The Length1 SMA crossing above the Length2 SMA goes long and crossing below goes short, reversing an opposite position. A long closes
/// when price reaches the entry plus LongProfit (a fraction of the entry price) and a short when it reaches the entry minus ShortProfit.
/// </summary>
public class GreaseTrapStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<decimal> _longProfit;
	private readonly StrategyParam<decimal> _shortProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _takePrice;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int Length1
	{
		get => _length1.Value;
		set => _length1.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int Length2
	{
		get => _length2.Value;
		set => _length2.Value = value;
	}

	/// <summary>
	/// Long profit target as a fraction of the entry price.
	/// </summary>
	public decimal LongProfit
	{
		get => _longProfit.Value;
		set => _longProfit.Value = value;
	}

	/// <summary>
	/// Short profit target as a fraction of the entry price.
	/// </summary>
	public decimal ShortProfit
	{
		get => _shortProfit.Value;
		set => _shortProfit.Value = value;
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
	/// Constructor.
	/// </summary>
	public GreaseTrapStrategy()
	{
		_length1 = Param(nameof(Length1), 9)
			.SetGreaterThanZero()
			.SetDisplay("Length 1", "Fast SMA length", "Indicators");

		_length2 = Param(nameof(Length2), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length 2", "Slow SMA length", "Indicators");

		_longProfit = Param(nameof(LongProfit), 0.02m)
			.SetNotNegative()
			.SetDisplay("Long Profit", "Long profit target as a fraction of the entry price", "Risk");

		_shortProfit = Param(nameof(ShortProfit), 0.02m)
			.SetNotNegative()
			.SetDisplay("Short Profit", "Short profit target as a fraction of the entry price", "Risk");

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
		_prevFast = null;
		_prevSlow = null;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;
		_takePrice = 0m;

		var fast = new SimpleMovingAverage { Length = Length1 };
		var slow = new SimpleMovingAverage { Length = Length2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		var close = candle.ClosePrice;
		var crossUp = pf <= ps && fast > slow;
		var crossDown = pf >= ps && fast < slow;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_takePrice = LongProfit > 0 ? close * (1 + LongProfit) : 0m;
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_takePrice = ShortProfit > 0 ? close * (1 - ShortProfit) : 0m;
		}
		else if (Position > 0 && _takePrice > 0 && candle.HighPrice >= _takePrice)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && _takePrice > 0 && candle.LowPrice <= _takePrice)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
