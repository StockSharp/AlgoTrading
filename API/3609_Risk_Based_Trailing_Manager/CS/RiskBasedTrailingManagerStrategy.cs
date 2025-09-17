using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Risk management strategy that closes positions once profit or loss
/// reaches percentage based thresholds and applies a virtual trailing stop.
/// </summary>
public class RiskBasedTrailingManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercentage;
	private readonly StrategyParam<decimal> _profitPercentage;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Risk percentage of account balance used as a loss threshold.
	/// </summary>
	public decimal RiskPercentage
	{
		get => _riskPercentage.Value;
		set => _riskPercentage.Value = value;
	}

	/// <summary>
	/// Profit percentage of account balance required to close the position.
	/// </summary>
	public decimal ProfitPercentage
	{
		get => _profitPercentage.Value;
		set => _profitPercentage.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type used to trigger periodic evaluations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RiskBasedTrailingManagerStrategy"/> class.
	/// </summary>
	public RiskBasedTrailingManagerStrategy()
	{
		_riskPercentage = Param(nameof(RiskPercentage), 1m)
			.SetRange(0m, 10m)
			.SetDisplay("Risk Percentage", "Risk percentage of the balance used as a stop threshold", "Risk Management")
			.SetCanOptimize(true);

		_profitPercentage = Param(nameof(ProfitPercentage), 2m)
			.SetRange(0m, 20m)
			.SetDisplay("Profit Percentage", "Profit percentage of the balance required to secure gains", "Risk Management")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 50m)
			.SetRange(0m, 500m)
			.SetDisplay("Trailing Stop Points", "Trailing stop distance in price points", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used to evaluate open positions", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			return [];

		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio cannot be null.");

		if (Security == null)
			throw new InvalidOperationException("Security must be specified.");

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0m)
		{
			ResetTrailingStops();
			return;
		}

		var balance = Portfolio?.CurrentValue ?? 0m;

		if (balance <= 0m)
			return;

		var riskAmount = balance * RiskPercentage / 100m;
		var profitAmount = balance * ProfitPercentage / 100m;
		var averagePrice = Position.AveragePrice ?? 0m;

		if (averagePrice <= 0m)
			return;

		var priceDiff = candle.ClosePrice - averagePrice;
		var currentProfit = Position * priceDiff;

		if (currentProfit >= profitAmount)
		{
			ExitPosition();
			return;
		}

		if (currentProfit <= -riskAmount)
		{
			ExitPosition();
			return;
		}

		UpdateTrailingStops(candle, averagePrice);
	}

	private void UpdateTrailingStops(ICandleMessage candle, decimal averagePrice)
	{
		var step = Security?.PriceStep ?? 0m;

		if (TrailingStopPoints <= 0m || step <= 0m)
		{
			ResetTrailingStops();
			return;
		}

		var trailingDistance = TrailingStopPoints * step;

		if (Position > 0m)
		{
			var candidate = candle.ClosePrice - trailingDistance;

			if (candidate > 0m && candidate < averagePrice)
			{
				if (_longTrailingStop is null || candidate > _longTrailingStop.Value)
					_longTrailingStop = Security?.ShrinkPrice(candidate) ?? candidate;
			}

			if (_longTrailingStop is decimal trailing && candle.LowPrice <= trailing)
				ExitPosition();
		}
		else if (Position < 0m)
		{
			var candidate = candle.ClosePrice + trailingDistance;

			if (candidate > averagePrice)
			{
				if (_shortTrailingStop is null || candidate < _shortTrailingStop.Value)
					_shortTrailingStop = Security?.ShrinkPrice(candidate) ?? candidate;
			}

			if (_shortTrailingStop is decimal trailing && candle.HighPrice >= trailing)
				ExitPosition();
		}
	}

	private void ExitPosition()
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(-Position);

		ResetTrailingStops();
	}

	private void ResetTrailingStops()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}
}
