using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Elliott Wave Supertrend Exit strategy.
/// A candle whose low is the lowest low of the last WaveLength candles is a local low and goes long; a candle whose high
/// is the highest high of the last WaveLength candles is a local high and goes short, reversing an opposite position.
/// A long closes when the Supertrend turns down and a short when it turns up, and a percent stop limits the loss.
/// </summary>
public class ElliottWaveSupertrendExitStrategy : Strategy
{
	private readonly StrategyParam<int> _waveLength;
	private readonly StrategyParam<int> _supertrendLength;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private bool? _prevIsUpTrend;

	/// <summary>
	/// Candles that define a local high or low.
	/// </summary>
	public int WaveLength
	{
		get => _waveLength.Value;
		set => _waveLength.Value = value;
	}

	/// <summary>
	/// Supertrend ATR length.
	/// </summary>
	public int SupertrendLength
	{
		get => _supertrendLength.Value;
		set => _supertrendLength.Value = value;
	}

	/// <summary>
	/// Supertrend ATR multiplier.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ElliottWaveSupertrendExitStrategy()
	{
		_waveLength = Param(nameof(WaveLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Wave Length", "Candles that define a local high or low", "Indicators");

		_supertrendLength = Param(nameof(SupertrendLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Length", "Supertrend ATR length", "Indicators");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 10m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevIsUpTrend = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevIsUpTrend = null;

		_highest = new Highest { Length = WaveLength };
		_lowest = new Lowest { Length = WaveLength };
		var supertrend = new SuperTrend { Length = SupertrendLength, Multiplier = SupertrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, ProcessCandle)
			.Start();

		StartProtection(new Unit(), StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highValue = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, candle.OpenTime) { IsFinal = true });
		var lowValue = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, candle.OpenTime) { IsFinal = true });

		if (!supertrendValue.IsFormed || !highValue.IsFormed || !lowValue.IsFormed)
			return;

		var isUpTrend = ((SuperTrendIndicatorValue)supertrendValue).IsUpTrend;
		var prevIsUpTrend = _prevIsUpTrend;
		_prevIsUpTrend = isUpTrend;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var localHigh = candle.HighPrice >= highValue.GetValue<decimal>();
		var localLow = candle.LowPrice <= lowValue.GetValue<decimal>();

		// A candle that is both the highest and the lowest of the window gives no direction.
		if (localLow && !localHigh && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (localHigh && !localLow && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && prevIsUpTrend == true && !isUpTrend)
			SellMarket(Position);
		else if (Position < 0 && prevIsUpTrend == false && isUpTrend)
			BuyMarket(-Position);
	}
}
