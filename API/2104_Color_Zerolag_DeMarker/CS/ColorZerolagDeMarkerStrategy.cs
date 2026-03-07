using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Zerolag DeMarker indicator.
/// Combines two DeMarker indicators into fast and slow trend lines.
/// Generates signals on line crossovers.
/// </summary>
public class ColorZerolagDeMarkerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private decimal _smoothConst;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int Smoothing { get => _smoothing.Value; set => _smoothing.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZerolagDeMarkerStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast DeMarker period", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow DeMarker period", "Indicator");

		_smoothing = Param(nameof(Smoothing), 15)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "Smoothing factor for slow line", "Indicator");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_prevFast = 0; _prevSlow = 0; _hasPrev = false;
		_smoothConst = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_smoothConst = (Smoothing - 1m) / Smoothing;

		var fast = new DeMarker { Length = FastPeriod };
		var slow = new DeMarker { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, (candle, fastVal, slowVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var fastLine = fastVal;
				var slowLine = fastLine / Smoothing + _prevSlow * _smoothConst;

				if (_hasPrev)
				{
					if (_prevFast < _prevSlow && fastLine > slowLine && Position <= 0)
					{
						if (Position < 0) BuyMarket();
						BuyMarket();
					}
					else if (_prevFast > _prevSlow && fastLine < slowLine && Position >= 0)
					{
						if (Position > 0) SellMarket();
						SellMarket();
					}
				}

				_prevFast = fastLine;
				_prevSlow = slowLine;
				_hasPrev = true;
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
