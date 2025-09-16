using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the MetaTrader KDJ Expert Advisor logic.
/// Uses the KDJ oscillator to detect momentum reversals and opens a single position with fixed take-profit and stop-loss levels.
/// </summary>
public class KdjExpertAdvisorStrategy : Strategy
{
	private readonly StrategyParam<int> _kdjPeriod;
	private readonly StrategyParam<int> _smoothK;
	private readonly StrategyParam<int> _smoothD;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousK;
	private decimal? _previousKdc;
	private decimal _pipSize;

	/// <summary>
	/// Main lookback period used to calculate RSV for the KDJ oscillator.
	/// </summary>
	public int KdjPeriod
	{
		get => _kdjPeriod.Value;
		set => _kdjPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period applied to the %K line.
	/// </summary>
	public int SmoothK
	{
		get => _smoothK.Value;
		set => _smoothK.Value = value;
	}

	/// <summary>
	/// Smoothing period applied to the %D line.
	/// </summary>
	public int SmoothD
	{
		get => _smoothD.Value;
		set => _smoothD.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Volume applied to every market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="KdjExpertAdvisorStrategy"/> class.
	/// </summary>
	public KdjExpertAdvisorStrategy()
	{
		_kdjPeriod = Param(nameof(KdjPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("KDJ Length", "Lookback period for KDJ RSV calculation", "KDJ")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_smoothK = Param(nameof(SmoothK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smooth %K", "Smoothing length for %K", "KDJ")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_smoothD = Param(nameof(SmoothD), 6)
			.SetGreaterThanZero()
			.SetDisplay("Smooth %D", "Smoothing length for %D", "KDJ")
			.SetCanOptimize(true)
			.SetOptimize(1, 15, 1);

		_stopLossPips = Param(nameof(StopLossPips), 25)
			.SetGreaterOrEqual(0)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 100, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 45)
			.SetGreaterOrEqual(0)
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 150, 5);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Quantity used for entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for KDJ calculation", "Data");
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

		_previousK = null;
		_previousKdc = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var stopLossUnit = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;
		var takeProfitUnit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;

		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			useMarketOrders: true);

		var kdj = new Stochastic
		{
			Length = KdjPeriod,
			KPeriod = SmoothK,
			DPeriod = SmoothD
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(kdj, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, kdj);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue kdjValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stochastic = (StochasticValue)kdjValue;
		if (stochastic.K is not decimal k || stochastic.D is not decimal d)
			return;

		var kdc = k - d;

		var buySignal = false;
		var sellSignal = false;

		if (_previousKdc.HasValue)
		{
			buySignal |= _previousKdc.Value < 0m && kdc > 0m;
			sellSignal |= _previousKdc.Value > 0m && kdc < 0m;
		}

		if (_previousK.HasValue)
		{
			buySignal |= kdc > 0m && _previousK.Value < k;
			sellSignal |= kdc < 0m && _previousK.Value > k;
		}

		if (buySignal || sellSignal)
		{
			if (IsFormedAndOnlineAndAllowTrading() && Position == 0)
			{
				if (buySignal)
				{
					LogInfo($"Buy signal at {candle.ClosePrice}: K={k:F2}, D={d:F2}, K-D={kdc:F2}");
					BuyMarket(OrderVolume);
				}
				else if (sellSignal)
				{
					LogInfo($"Sell signal at {candle.ClosePrice}: K={k:F2}, D={d:F2}, K-D={kdc:F2}");
					SellMarket(OrderVolume);
				}
			}
		}

		_previousK = k;
		_previousKdc = kdc;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 1m;

		var step = security.PriceStep ?? 1m;
		var decimals = security.Decimals;
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;

		return step * multiplier;
	}
}
