using System;
using System.Linq;
using System.Collections.Generic;
using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Anchored Momentum Candle indicator.
/// Compares open-based and close-based momentum to determine candle color.
/// </summary>
public class AnchoredMomentumCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _momPeriod;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _openQueue = new();
	private readonly Queue<decimal> _closeQueue = new();
	private decimal _sumOpen, _sumClose;
	private decimal _emaOpen, _emaClose;
	private bool _emaInit;
	private decimal? _prevColor;

	public int MomPeriod { get => _momPeriod.Value; set => _momPeriod.Value = value; }
	public int SmoothPeriod { get => _smoothPeriod.Value; set => _smoothPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AnchoredMomentumCandleStrategy()
	{
		_momPeriod = Param(nameof(MomPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "SMA length", "Parameters")
			.SetOptimize(2, 20, 1);

		_smoothPeriod = Param(nameof(SmoothPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Period", "EMA length", "Parameters")
			.SetOptimize(2, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
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
		_openQueue.Clear();
		_closeQueue.Clear();
		_sumOpen = _sumClose = 0m;
		_emaOpen = _emaClose = 0m;
		_emaInit = false;
		_prevColor = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		_sumOpen += open;
		_openQueue.Enqueue(open);
		if (_openQueue.Count > MomPeriod)
			_sumOpen -= _openQueue.Dequeue();

		_sumClose += close;
		_closeQueue.Enqueue(close);
		if (_closeQueue.Count > MomPeriod)
			_sumClose -= _closeQueue.Dequeue();

		var k = 2m / (SmoothPeriod + 1);

		if (!_emaInit)
		{
			_emaOpen = open;
			_emaClose = close;
			_emaInit = true;
		}
		else
		{
			_emaOpen = k * open + (1 - k) * _emaOpen;
			_emaClose = k * close + (1 - k) * _emaClose;
		}

		if (_openQueue.Count < MomPeriod)
			return;

		var smaOpen = _sumOpen / MomPeriod;
		var smaClose = _sumClose / MomPeriod;

		var openMomentum = smaOpen == 0m ? 0m : 100m * (_emaOpen / smaOpen - 1m);
		var closeMomentum = smaClose == 0m ? 0m : 100m * (_emaClose / smaClose - 1m);

		var color = openMomentum < closeMomentum ? 2m : openMomentum > closeMomentum ? 0m : 1m;

		if (_prevColor == null)
		{
			_prevColor = color;
			return;
		}

		if (color == 2m && _prevColor != 2m)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (color == 0m && _prevColor != 0m)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevColor = color;
	}
}
