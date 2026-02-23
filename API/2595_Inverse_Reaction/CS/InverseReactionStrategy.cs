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
/// Strategy that reacts to large single-bar moves expecting a mean reversion.
/// </summary>
public class InverseReactionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _coefficient;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _absChanges = new();
	private decimal _entryPrice;

	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public decimal Coefficient { get => _coefficient.Value; set => _coefficient.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public InverseReactionStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 250m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take-profit distance in points", "Risk");

		_coefficient = Param(nameof(Coefficient), 1.618m)
			.SetGreaterThanZero()
			.SetDisplay("Coefficient", "Confidence coefficient", "Signal");

		_maPeriod = Param(nameof(MaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_absChanges.Clear();
		_entryPrice = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m) step = 1m;

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPoints * step, UnitTypes.Absolute),
			useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var change = candle.ClosePrice - candle.OpenPrice;
		var absChange = Math.Abs(change);

		_absChanges.Add(absChange);
		if (_absChanges.Count > MaPeriod)
			_absChanges.RemoveAt(0);

		if (_absChanges.Count < MaPeriod)
			return;

		var avg = 0m;
		for (int i = 0; i < _absChanges.Count; i++)
			avg += _absChanges[i];
		avg /= _absChanges.Count;

		var threshold = avg * Coefficient;

		if (Position != 0)
			return;

		if (absChange > threshold && absChange > 0m)
		{
			if (change < 0m)
			{
				// Large bearish bar => expect reversion upward
				BuyMarket();
			}
			else
			{
				// Large bullish bar => expect reversion downward
				SellMarket();
			}
		}
	}
}
