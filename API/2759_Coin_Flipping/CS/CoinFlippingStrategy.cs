using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Randomized coin flipping strategy that alternates between buying and selling based on a pseudo-random generator.
/// Mimics the original MetaTrader expert advisor by opening a single position at a time with symmetric risk controls.
/// </summary>
public class CoinFlippingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<DataType> _candleType;

	private Random _random;
	private decimal _priceStep;
	private decimal _takeProfitDistance;
	private decimal _stopLossDistance;

	/// <summary>
	/// Portfolio share allocated to every trade in percent.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Candle type used for scheduling trade attempts.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public CoinFlippingStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Portfolio percentage allocated per trade", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 20)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Target distance expressed in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_stopLossPips = Param(nameof(StopLossPips), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for trade timing", "Data");
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

		// Reset cached state when the strategy is reset.
		_random = null;
		_priceStep = 0m;
		_takeProfitDistance = 0m;
		_stopLossDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Seed the pseudo-random generator similarly to the MQL expert.
		_random = new Random(Environment.TickCount);

		// Determine price step information for translating pips into price units.
		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_takeProfitDistance = TakeProfitPips * _priceStep;
		_stopLossDistance = StopLossPips * _priceStep;

		var takeProfitUnit = _takeProfitDistance > 0m
			? new Unit(_takeProfitDistance, UnitTypes.Price)
			: new Unit();

		var stopLossUnit = _stopLossDistance > 0m
			? new Unit(_stopLossDistance, UnitTypes.Price)
			: new Unit();

		// Attach protective orders so every position has an exit target and stop.
		StartProtection(takeProfitUnit, stopLossUnit);

		// Subscribe to candle data to trigger decision making once per bar.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only use completed candles to avoid duplicate executions while a bar is forming.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// The strategy maintains at most one position at a time.
		if (Position != 0)
			return;

		if (_random == null)
			return;

		var entryPrice = candle.ClosePrice;
		if (entryPrice <= 0m)
			return;

		var volume = CalculateOrderVolume(entryPrice);
		if (volume <= 0m)
			return;

		var isBuy = _random.Next(0, 2) == 0;
		if (isBuy)
		{
			LogInfo($"Opening long position at {entryPrice} with volume {volume}.");
			BuyMarket(volume);
		}
		else
		{
			LogInfo($"Opening short position at {entryPrice} with volume {volume}.");
			SellMarket(volume);
		}
	}

	private decimal CalculateOrderVolume(decimal entryPrice)
	{
		var balance = Portfolio?.CurrentValue ?? 0m;
		if (balance <= 0m)
			return 0m;

		var riskAmount = balance * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return 0m;

		var stopDistance = _stopLossDistance;
		if (stopDistance <= 0m)
		{
			stopDistance = StopLossPips * _priceStep;
		}

		if (stopDistance <= 0m)
			return 0m;

		// Risk per unit equals the stop distance; divide to get the number of contracts.
		var rawVolume = riskAmount / stopDistance;
		var volume = NormalizeVolume(rawVolume);

		if (volume <= 0m)
		{
			volume = Volume > 0m ? Volume : 1m;
			volume = NormalizeVolume(volume);
		}

		return volume;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		if (Security?.VolumeStep is decimal step && step > 0m)
		{
			volume = Math.Floor(volume / step) * step;
		}

		if (Security?.MinVolume is decimal minVolume && minVolume > 0m && volume < minVolume)
		{
			volume = minVolume;
		}

		if (Security?.MaxVolume is decimal maxVolume && maxVolume > 0m && volume > maxVolume)
		{
			volume = maxVolume;
		}

		return volume;
	}
}
