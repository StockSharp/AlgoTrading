using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with risk management via StartProtection.
/// </summary>
public class MovingUpStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<bool> _useTp, _useSl, _useTs;
	private readonly StrategyParam<decimal> _trailingStop;
	private Order _pending;
	private bool _isInitialized;
	private bool _wasFastBelowSlow;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public bool UseTakeProfit { get => _useTp.Value; set => _useTp.Value = value; }
	public bool UseStopLoss { get => _useSl.Value; set => _useSl.Value = value; }
	public bool UseTrailingStop { get => _useTs.Value; set => _useTs.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public MovingUpStrategy()
	{
		_fastLength = Param(nameof(FastLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast MA period", "MA");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow MA period", "MA");

		_stopLoss = Param(nameof(StopLoss), 250m)
			.SetGreaterThanZero()
			.SetDisplay("SL", "Stop loss distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetGreaterThanZero()
			.SetDisplay("TP", "Take profit distance", "Risk");

		_useTp = Param(nameof(UseTakeProfit), true);
		_useSl = Param(nameof(UseStopLoss), true);
		_useTs = Param(nameof(UseTrailingStop), false);
		_trailingStop = Param(nameof(TrailingStop), 200m).SetNotNegative();
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle", "Candle type", "General");
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
		_isInitialized = default;
		_wasFastBelowSlow = default;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_pending = null;

		var fastMa = new SimpleMovingAverage { Length = FastLength };
		var slowMa = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(UseTakeProfit ? TakeProfit : 0m, UnitTypes.Absolute),
			stopLoss: new Unit(UseStopLoss ? StopLoss : 0m, UnitTypes.Absolute), useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_wasFastBelowSlow = fast < slow;
			_isInitialized = true;
			return;
		}

		var isFastBelowSlow = fast < slow;

		if (_wasFastBelowSlow != isFastBelowSlow && (_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed))
		{
			if (!isFastBelowSlow && Position <= 0)
			{
				_pending = BuyMarket(Volume + Math.Abs(Math.Min(0m, Position)));
			}
			else if (isFastBelowSlow && Position >= 0)
			{
				_pending = SellMarket(Volume + Math.Max(0m, Position));
			}

			_wasFastBelowSlow = isFastBelowSlow;
		}
	}
}
