using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the MetaTrader Money Fixed Margin sample using StockSharp.
/// It demonstrates fixed percentage risk sizing for long trades.
/// </summary>
public class MoneyFixedMarginStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _checkInterval;
	private readonly StrategyParam<DataType> _candleType;

	private int _barCount;
	private decimal _pipSize;

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Portfolio percentage risked on each trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Number of finished candles between trade attempts.
	/// </summary>
	public int CheckInterval
	{
		get => _checkInterval.Value;
		set => _checkInterval.Value = value;
	}

	/// <summary>
	/// Candle series used to time entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MoneyFixedMarginStrategy"/>.
	/// </summary>
	public MoneyFixedMarginStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Percent", "Percent of equity risked per trade", "Risk");

		_checkInterval = Param(nameof(CheckInterval), 980)
			.SetGreaterThanZero()
			.SetDisplay("Check Interval", "Completed candles between trades", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_barCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;

		_pipSize = priceStep * adjust;
		if (_pipSize <= 0m)
			_pipSize = priceStep > 0m ? priceStep : 1m;

		// Attach a protective stop using the pip-based distance converted to price units.
		StartProtection(
			stopLoss: new Unit(StopLossPips * _pipSize, UnitTypes.Absolute),
			useMarketOrders: true);

		// Subscribe to the candle stream that emulates the tick counter from the MQL example.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Count finished candles to mirror the tick counter from the original script.
		_barCount++;

		if (_barCount < CheckInterval)
			return;

		var entryPrice = candle.ClosePrice;

		if (entryPrice <= 0m)
		{
			AddWarningLog("Skip trade because entry price is not positive. Close={0}", entryPrice);
			return;
		}

		var riskAmount = CalculateRiskAmount();
		if (riskAmount <= 0m)
		{
			AddWarningLog("Skip trade because risk amount is not positive. Portfolio value={0}", riskAmount);
			return;
		}

		var stopDistance = StopLossPips * _pipSize;
		var stopPrice = entryPrice - stopDistance;

		var volumeWithoutStop = CalculateFixedMarginVolume(entryPrice, 0m, riskAmount);
		var volumeWithStop = CalculateFixedMarginVolume(entryPrice, stopPrice, riskAmount);

		this.AddInfoLog(
			"StopLoss=0 -> volume {0:0.####}; StopLoss={1:0.#####} -> volume {2:0.####}; Portfolio={3:0.##}",
			volumeWithoutStop,
			stopPrice,
			volumeWithStop,
			GetPortfolioValue());

		if (volumeWithStop <= 0m)
		{
			// Mimic the MQL behavior where zero size prevents order submission.
			return;
		}

		var order = BuyMarket(volumeWithStop);

		if (order is null)
		{
			AddWarningLog("Buy order was not sent by the broker model.");
			return;
		}

		// Reset the counter only after successfully sending an order.
		_barCount = 0;
	}

	private decimal CalculateRiskAmount()
	{
		var portfolioValue = GetPortfolioValue();
		return portfolioValue > 0m ? portfolioValue * RiskPercent / 100m : 0m;
	}

	private decimal GetPortfolioValue()
	{
		var current = Portfolio?.CurrentValue ?? 0m;
		if (current > 0m)
			return current;

		var begin = Portfolio?.BeginValue ?? 0m;
		return begin > 0m ? begin : current;
	}

	private decimal CalculateFixedMarginVolume(decimal entryPrice, decimal stopPrice, decimal riskAmount)
	{
		if (riskAmount <= 0m || entryPrice <= 0m || stopPrice <= 0m)
			return 0m;

		var stopDistance = entryPrice - stopPrice;
		if (stopDistance <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var stepPrice = Security?.StepPrice ?? 0m;
		if (stepPrice <= 0m)
			stepPrice = priceStep;

		var stepsCount = stopDistance / priceStep;
		if (stepsCount <= 0m)
			return 0m;

		var riskPerVolume = stepsCount * stepPrice;
		if (riskPerVolume <= 0m)
			return 0m;

		var rawVolume = riskAmount / riskPerVolume;
		return NormalizeVolume(rawVolume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		if (Security?.VolumeStep is decimal step && step > 0m)
		{
			volume = Math.Ceiling(volume / step) * step;
		}

		return volume;
	}
}
