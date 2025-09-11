using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Hammer and Shooting Star candlestick patterns.
/// Opens long after a hammer and short after a shooting star.
/// Exit orders are placed at the signal candle high and low.
/// </summary>
public class HammerShootingStarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _wickFactor;
	private readonly StrategyParam<decimal> _maxOppositeWickFactor;
	private readonly StrategyParam<decimal> _minBodyRangePct;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevHammer;
	private bool _prevShootingStar;
	private decimal _prevHigh;
	private decimal _prevLow;

	/// <summary>
	/// Minimum wick to body ratio for the main wick.
	/// </summary>
	public decimal WickFactor
	{
		get => _wickFactor.Value;
		set => _wickFactor.Value = value;
	}

	/// <summary>
	/// Maximum ratio for the opposite wick to body.
	/// </summary>
	public decimal MaxOppositeWickFactor
	{
		get => _maxOppositeWickFactor.Value;
		set => _maxOppositeWickFactor.Value = value;
	}

	/// <summary>
	/// Minimum body as percentage of total bar range.
	/// </summary>
	public decimal MinBodyRangePct
	{
		get => _minBodyRangePct.Value;
		set => _minBodyRangePct.Value = value;
	}

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HammerShootingStarStrategy"/> class.
	/// </summary>
	public HammerShootingStarStrategy()
	{
		_wickFactor = Param(nameof(WickFactor), 0.9m)
			.SetRange(0.5m, 2m)
			.SetDisplay("Wick Factor", "Min wick to body ratio", "Pattern")
			.SetCanOptimize(true);

		_maxOppositeWickFactor = Param(nameof(MaxOppositeWickFactor), 0.45m)
			.SetRange(0.1m, 1m)
			.SetDisplay("Opposite Wick Factor", "Max opposite wick to body", "Pattern")
			.SetCanOptimize(true);

		_minBodyRangePct = Param(nameof(MinBodyRangePct), 0.2m)
			.SetRange(0.1m, 0.5m)
			.SetDisplay("Min Body Range %", "Min body as percent of bar range", "Pattern")
			.SetCanOptimize(true);

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

		_prevHammer = false;
		_prevShootingStar = false;
		_prevHigh = 0m;
		_prevLow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevHammer && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			SellStop(volume, _prevLow);
			SellLimit(volume, _prevHigh);
		}
		else if (_prevShootingStar && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			BuyStop(volume, _prevHigh);
			BuyLimit(volume, _prevLow);
		}

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var barRange = candle.HighPrice - candle.LowPrice;
		var upperWick = candle.HighPrice - Math.Max(candle.ClosePrice, candle.OpenPrice);
		var lowerWick = Math.Min(candle.ClosePrice, candle.OpenPrice) - candle.LowPrice;
		var bodyNonZero = barRange > 0m && body > 0m;

		_prevHammer = bodyNonZero && candle.OpenPrice > candle.ClosePrice &&
			lowerWick >= WickFactor * body &&
			upperWick <= MaxOppositeWickFactor * body &&
			body / barRange >= MinBodyRangePct;

		_prevShootingStar = bodyNonZero && candle.OpenPrice < candle.ClosePrice &&
			upperWick >= WickFactor * body &&
			lowerWick <= MaxOppositeWickFactor * body &&
			body / barRange >= MinBodyRangePct;

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
