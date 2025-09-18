using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on adaptive renko movements based on ATR volatility.
/// </summary>
public class AdaptiveRenkoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _minBrick;
	private readonly StrategyParam<DataType> _candleType;

	private readonly AverageTrueRange _atr = new();

	private decimal _lastBrickPrice;
	private bool _hasBrick;

	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	public int VolatilityPeriod
	{
		get => _volatilityPeriod.Value;
		set => _volatilityPeriod.Value = value;
	}

	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	public decimal MinBrickSize
	{
		get => _minBrick.Value;
		set => _minBrick.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public AdaptiveRenkoStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Period", "ATR calculation period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_multiplier = Param(nameof(Multiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_minBrick = Param(nameof(MinBrickSize), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Min Brick", "Minimum brick size", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)))
			.SetDisplay("Candle Type", "Time frame for ATR calculation", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr.Length = VolatilityPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var brick = Math.Max(atr * Multiplier, MinBrickSize);

		if (!_hasBrick)
		{
			_lastBrickPrice = candle.ClosePrice;
			_hasBrick = true;
			return;
		}

		var diff = candle.ClosePrice - _lastBrickPrice;

		if (diff >= brick)
		{
			if (Position <= 0)
				BuyMarket(Volume);

			_lastBrickPrice = candle.ClosePrice;
		}
		else if (diff <= -brick)
		{
			if (Position >= 0)
				SellMarket(Volume);

			_lastBrickPrice = candle.ClosePrice;
		}
	}
}
