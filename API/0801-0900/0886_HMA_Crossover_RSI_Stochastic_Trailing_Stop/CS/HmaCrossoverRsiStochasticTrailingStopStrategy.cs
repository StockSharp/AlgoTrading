using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// HMA Crossover RSI Stochastic Trailing Stop strategy.
/// Goes long when the fast HMA crosses above the slow HMA while RSI is below RsiBuyLevel and the smoothed Stochastic is below
/// StochBuyLevel. Goes short when the fast HMA crosses below the slow HMA while RSI is above RsiSellLevel and the smoothed
/// Stochastic is above StochSellLevel. An opposite signal reverses the position and a percent trailing stop manages exits.
/// </summary>
public class HmaCrossoverRsiStochasticTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _fastHmaLength;
	private readonly StrategyParam<int> _slowHmaLength;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _stochSmooth;
	private readonly StrategyParam<decimal> _stochBuyLevel;
	private readonly StrategyParam<decimal> _stochSellLevel;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Fast HMA length.
	/// </summary>
	public int FastHmaLength
	{
		get => _fastHmaLength.Value;
		set => _fastHmaLength.Value = value;
	}

	/// <summary>
	/// Slow HMA length.
	/// </summary>
	public int SlowHmaLength
	{
		get => _slowHmaLength.Value;
		set => _slowHmaLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI must be below this level for a long.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// RSI must be above this level for a short.
	/// </summary>
	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// Stochastic %K length.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Smoothing period of the Stochastic.
	/// </summary>
	public int StochSmooth
	{
		get => _stochSmooth.Value;
		set => _stochSmooth.Value = value;
	}

	/// <summary>
	/// Smoothed Stochastic must be below this level for a long.
	/// </summary>
	public decimal StochBuyLevel
	{
		get => _stochBuyLevel.Value;
		set => _stochBuyLevel.Value = value;
	}

	/// <summary>
	/// Smoothed Stochastic must be above this level for a short.
	/// </summary>
	public decimal StochSellLevel
	{
		get => _stochSellLevel.Value;
		set => _stochSellLevel.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in percent.
	/// </summary>
	public decimal TrailingPercent
	{
		get => _trailingPercent.Value;
		set => _trailingPercent.Value = value;
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
	public HmaCrossoverRsiStochasticTrailingStopStrategy()
	{
		_fastHmaLength = Param(nameof(FastHmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast HMA", "Fast HMA length", "Indicators");

		_slowHmaLength = Param(nameof(SlowHmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow HMA", "Slow HMA length", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 45m)
			.SetDisplay("RSI Buy Level", "RSI must be below this level for a long", "Signals");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 60m)
			.SetDisplay("RSI Sell Level", "RSI must be above this level for a short", "Signals");

		_stochLength = Param(nameof(StochLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Length", "Stochastic %K length", "Indicators");

		_stochSmooth = Param(nameof(StochSmooth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Smooth", "Smoothing period of the Stochastic", "Indicators");

		_stochBuyLevel = Param(nameof(StochBuyLevel), 39m)
			.SetDisplay("Stoch Buy Level", "Smoothed Stochastic must be below this level for a long", "Signals");

		_stochSellLevel = Param(nameof(StochSellLevel), 63m)
			.SetDisplay("Stoch Sell Level", "Smoothed Stochastic must be above this level for a short", "Signals");

		_trailingPercent = Param(nameof(TrailingPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing %", "Trailing stop distance in percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fastHma = new HullMovingAverage { Length = FastHmaLength };
		var slowHma = new HullMovingAverage { Length = SlowHmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochLength;
		stochastic.D.Length = StochSmooth;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastHma, slowHma, rsi, stochastic, ProcessCandle)
			.Start();

		StartProtection(new Unit(), TrailingPercent > 0 ? new Unit(TrailingPercent, UnitTypes.Percent) : new Unit(), isStopTrailing: true, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastHma);
			DrawIndicator(area, slowHma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, stochastic);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue rsiValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		if (!rsiValue.IsFormed || !stochValue.IsFormed || stochValue is not IStochasticOscillatorValue { D: decimal stoch })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var crossUp = pf <= ps && fast > slow;
		var crossDown = pf >= ps && fast < slow;

		if (crossUp && rsi < RsiBuyLevel && stoch < StochBuyLevel && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && rsi > RsiSellLevel && stoch > StochSellLevel && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
