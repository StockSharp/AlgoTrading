using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on divergence between fast and slow SMA.
/// Opens long when previous fast SMA exceeds slow SMA within specified range.
/// Opens short when previous fast SMA is below slow SMA within specified range.
/// Stop-loss and take-profit are optional and defined in price units.
/// </summary>
public class DivergenceTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _dvBuySell;
	private readonly StrategyParam<decimal> _dvStayOut;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Minimum divergence required for entry.
	/// </summary>
	public decimal DvBuySell
	{
		get => _dvBuySell.Value;
		set => _dvBuySell.Value = value;
	}

	/// <summary>
	/// Maximum divergence allowed for entry.
	/// </summary>
	public decimal DvStayOut
	{
		get => _dvStayOut.Value;
		set => _dvStayOut.Value = value;
	}

	/// <summary>
	/// Stop-loss in price units (0 disables).
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit in price units (0 disables).
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type to use for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DivergenceTraderStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast SMA length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 88)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow SMA length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 120, 5);

		_dvBuySell = Param(nameof(DvBuySell), 0.0011m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("DV Buy/Sell", "Minimum divergence for entry", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.0005m, 0.005m, 0.0005m);

		_dvStayOut = Param(nameof(DvStayOut), 0.0079m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("DV Stay Out", "Maximum divergence for entry", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.02m, 0.001m);

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Stop-loss in price units", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 1000m, 50m);

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Take-profit in price units", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 1000m, 50m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastSma = new SMA { Length = FastPeriod };
		var slowSma = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);

		decimal previousFast = 0m;
		decimal previousSlow = 0m;
		var isInitialized = false;

		subscription
			.Bind(fastSma, slowSma, (candle, fastValue, slowValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!isInitialized)
				{
					previousFast = fastValue;
					previousSlow = slowValue;
					isInitialized = true;
					return;
				}

				// Divergence is the previous difference between fast and slow SMA
				var divergence = previousFast - previousSlow;

				if (divergence >= DvBuySell && divergence <= DvStayOut)
				{
					if (Position <= 0)
						BuyMarket(Volume + Math.Abs(Position));
				}
				else if (divergence <= -DvBuySell && divergence >= -DvStayOut)
				{
					if (Position >= 0)
						SellMarket(Volume + Math.Abs(Position));
				}

				previousFast = fastValue;
				previousSlow = slowValue;
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}
}
