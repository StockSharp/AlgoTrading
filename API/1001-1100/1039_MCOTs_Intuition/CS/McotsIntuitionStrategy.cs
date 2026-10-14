using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MCOTs Intuition strategy.
/// Momentum is the change of RSI from the previous candle and its standard deviation is measured over RsiPeriod values.
/// A long opens when momentum exceeds the deviation times StdDevMultiplier while staying below the previous momentum times
/// ExhaustionMultiplier (strong but fading); a short opens on the mirrored condition. Exits are a fixed profit target and
/// stop loss in ticks.
/// </summary>
public class McotsIntuitionStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stdDevMultiplier;
	private readonly StrategyParam<decimal> _exhaustionMultiplier;
	private readonly StrategyParam<int> _profitTargetTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _stdDev;
	private decimal? _prevRsi;
	private decimal? _prevMomentum;

	/// <summary>
	/// RSI period, also the window of the momentum standard deviation.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier of the momentum standard deviation.
	/// </summary>
	public decimal StdDevMultiplier
	{
		get => _stdDevMultiplier.Value;
		set => _stdDevMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier of the previous momentum that marks exhaustion.
	/// </summary>
	public decimal ExhaustionMultiplier
	{
		get => _exhaustionMultiplier.Value;
		set => _exhaustionMultiplier.Value = value;
	}

	/// <summary>
	/// Profit target in ticks.
	/// </summary>
	public int ProfitTargetTicks
	{
		get => _profitTargetTicks.Value;
		set => _profitTargetTicks.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
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
	public McotsIntuitionStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period and deviation window", "Indicators");

		_stdDevMultiplier = Param(nameof(StdDevMultiplier), 1m)
			.SetNotNegative()
			.SetDisplay("StdDev Multiplier", "Multiplier of the momentum standard deviation", "Indicators");

		_exhaustionMultiplier = Param(nameof(ExhaustionMultiplier), 1m)
			.SetNotNegative()
			.SetDisplay("Exhaustion Multiplier", "Multiplier of the previous momentum", "Indicators");

		_profitTargetTicks = Param(nameof(ProfitTargetTicks), 40)
			.SetNotNegative()
			.SetDisplay("Profit Target Ticks", "Profit target in ticks", "Risk");

		_stopLossTicks = Param(nameof(StopLossTicks), 160)
			.SetNotNegative()
			.SetDisplay("Stop Loss Ticks", "Stop loss in ticks", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_stdDev = null;
		_prevRsi = null;
		_prevMomentum = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = null;
		_prevMomentum = null;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_stdDev = new StandardDeviation { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			ProfitTargetTicks > 0 ? new Unit(ProfitTargetTicks * step, UnitTypes.Absolute) : new Unit(),
			StopLossTicks > 0 ? new Unit(StopLossTicks * step, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true, isLocalStop: true);

		// The target and stop have to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished || !rsiValue.IsFormed)
			return;

		var rsi = rsiValue.GetValue<decimal>();

		if (_prevRsi is not decimal prevRsi)
		{
			_prevRsi = rsi;
			return;
		}

		_prevRsi = rsi;

		var momentum = rsi - prevRsi;
		var stdDevValue = _stdDev.Process(new DecimalIndicatorValue(_stdDev, momentum, candle.OpenTime) { IsFinal = true });

		var prevMomentum = _prevMomentum;
		_prevMomentum = momentum;

		if (!_stdDev.IsFormed || prevMomentum is not decimal previous)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var band = stdDevValue.GetValue<decimal>() * StdDevMultiplier;
		var exhaustion = previous * ExhaustionMultiplier;

		if (momentum > band && momentum < exhaustion && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (momentum < -band && momentum > exhaustion && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
