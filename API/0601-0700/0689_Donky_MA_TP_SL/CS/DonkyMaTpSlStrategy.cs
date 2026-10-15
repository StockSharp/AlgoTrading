using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donky MA TP SL strategy.
/// The fast SMA crossing above the slow SMA goes long and crossing below goes short, reversing an opposite position.
/// Half of the position closes at the first take-profit level and the remainder at the second level or at the stop-loss.
/// The levels are fractions of the entry price (0.03 means 3%).
/// </summary>
public class DonkyMaTpSlStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfit1Pct;
	private readonly StrategyParam<decimal> _takeProfit2Pct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _entryPrice;
	private bool _firstTargetDone;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// First take-profit distance as a fraction of the entry price.
	/// </summary>
	public decimal TakeProfit1Pct
	{
		get => _takeProfit1Pct.Value;
		set => _takeProfit1Pct.Value = value;
	}

	/// <summary>
	/// Second take-profit distance as a fraction of the entry price.
	/// </summary>
	public decimal TakeProfit2Pct
	{
		get => _takeProfit2Pct.Value;
		set => _takeProfit2Pct.Value = value;
	}

	/// <summary>
	/// Stop-loss distance as a fraction of the entry price.
	/// </summary>
	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
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
	public DonkyMaTpSlStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA length", "Indicators");

		_takeProfit1Pct = Param(nameof(TakeProfit1Pct), 0.03m)
			.SetNotNegative()
			.SetDisplay("Take Profit 1", "First target as a fraction of the entry price", "Risk");

		_takeProfit2Pct = Param(nameof(TakeProfit2Pct), 0.06m)
			.SetNotNegative()
			.SetDisplay("Take Profit 2", "Second target as a fraction of the entry price", "Risk");

		_stopLossPct = Param(nameof(StopLossPct), 0.01m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop distance as a fraction of the entry price", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_entryPrice = 0m;
		_firstTargetDone = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fast = new SimpleMovingAverage { Length = FastLength };
		var slow = new SimpleMovingAverage { Length = SlowLength };

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

		if (ManagePosition(candle))
			return;

		if (prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		var crossUp = pf <= ps && fast > slow;
		var crossDown = pf >= ps && fast < slow;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_firstTargetDone = false;
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_firstTargetDone = false;
		}
	}

	// Returns true when the position was closed completely on this candle.
	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position == 0 || _entryPrice <= 0)
			return false;

		if (Position > 0)
		{
			var stop = _entryPrice * (1 - StopLossPct);
			var target1 = _entryPrice * (1 + TakeProfit1Pct);
			var target2 = _entryPrice * (1 + TakeProfit2Pct);

			if (StopLossPct > 0 && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				return true;
			}

			if (TakeProfit2Pct > 0 && candle.HighPrice >= target2)
			{
				SellMarket(Position);
				return true;
			}

			if (!_firstTargetDone && TakeProfit1Pct > 0 && candle.HighPrice >= target1)
			{
				_firstTargetDone = true;
				SellMarket(Position / 2);
			}
		}
		else
		{
			var stop = _entryPrice * (1 + StopLossPct);
			var target1 = _entryPrice * (1 - TakeProfit1Pct);
			var target2 = _entryPrice * (1 - TakeProfit2Pct);
			var volume = Math.Abs(Position);

			if (StopLossPct > 0 && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				return true;
			}

			if (TakeProfit2Pct > 0 && candle.LowPrice <= target2)
			{
				BuyMarket(volume);
				return true;
			}

			if (!_firstTargetDone && TakeProfit1Pct > 0 && candle.LowPrice <= target1)
			{
				_firstTargetDone = true;
				BuyMarket(volume / 2);
			}
		}

		return false;
	}
}
