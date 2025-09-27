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
/// Port of the MetaTrader strategy MeanReversion.mq5.
/// Buys when price sets a fresh lookback low and targets the mid-point of the recent range,
/// or sells at a new high aiming for the same reversion level.
/// Position size is determined from the percentage risk and the stop distance.
/// </summary>
public class MeanReversionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _riskPercent;

	private DonchianChannels _donchian = null!;

	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private Sides? _activeSide;

	/// <summary>
	/// Candle type and timeframe used for the Donchian channel calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Amount of candles included in the high/low range.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Percent of portfolio equity risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MeanReversionStrategy"/>.
	/// </summary>
	public MeanReversionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to analyze", "General");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 200)
		.SetDisplay("Lookback", "Number of candles used for range detection", "Signals")
		.SetRange(20, 500)
		.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetDisplay("Risk %", "Percentage of equity risked per entry", "Money Management")
		.SetRange(0.25m, 5m)
		.SetCanOptimize(true);
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

		_stopPrice = null;
		_takeProfitPrice = null;
		_activeSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_donchian = new DonchianChannels { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_donchian, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		ManageOpenPosition(candle);

		if (Position != 0)
		return;

		var channel = (DonchianChannelsValue)donchianValue;

		if (channel.UpperBand is not decimal upperBand || channel.LowerBand is not decimal lowerBand || channel.Middle is not decimal midBand)
		return;

		GenerateSignals(candle, lowerBand, upperBand, midBand);
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0 && _activeSide == Sides.Buy)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (Position < 0 && _activeSide == Sides.Sell)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(-Position);
				ResetPositionState();
			}
		}

		if (Position == 0 && _activeSide != null)
		{
			ResetPositionState();
		}
	}

	private void GenerateSignals(ICandleMessage candle, decimal lowerBand, decimal upperBand, decimal midBand)
	{
		var closePrice = candle.ClosePrice;

		if (candle.LowPrice <= lowerBand)
		{
			var stopPrice = 2m * closePrice - midBand;
			var volume = CalculateRiskAdjustedVolume(closePrice, stopPrice);
			if (volume > 0m && stopPrice < closePrice)
			{
				BuyMarket(volume);
				_stopPrice = stopPrice;
				_takeProfitPrice = midBand;
				_activeSide = Sides.Buy;
			}
		}
		else if (candle.HighPrice >= upperBand)
		{
			var stopPrice = 2m * closePrice - midBand;
			var volume = CalculateRiskAdjustedVolume(closePrice, stopPrice);
			if (volume > 0m && stopPrice > closePrice)
			{
				SellMarket(volume);
				_stopPrice = stopPrice;
				_takeProfitPrice = midBand;
				_activeSide = Sides.Sell;
			}
		}
	}

	private decimal CalculateRiskAdjustedVolume(decimal entryPrice, decimal stopPrice)
	{
		var perUnitRisk = Math.Abs(entryPrice - stopPrice);
		if (perUnitRisk <= 0m)
		return 0m;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		var riskBudget = portfolioValue > 0m ? portfolioValue * (RiskPercent / 100m) : 0m;

		if (riskBudget <= 0m)
		{
			return GetMinimalVolume();
		}

		var rawVolume = riskBudget / perUnitRisk;
		var normalized = NormalizeVolume(rawVolume);
		var minimal = GetMinimalVolume();

		if (normalized < minimal)
		normalized = minimal;

		return normalized;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
		return volume;

		var normalized = Math.Floor(volume / step) * step;

		var max = Security?.VolumeMax ?? 0m;
		if (max > 0m && normalized > max)
		normalized = max;

		return normalized;
	}

	private decimal GetMinimalVolume()
	{
		var min = Security?.VolumeMin ?? 0m;
		if (min > 0m)
		return min;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		return step;

		return Volume > 0m ? Volume : 1m;
	}

	private void ResetPositionState()
	{
		_stopPrice = null;
		_takeProfitPrice = null;
		_activeSide = null;
	}
}

