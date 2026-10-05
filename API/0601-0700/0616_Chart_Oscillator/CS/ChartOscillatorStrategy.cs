using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chart Oscillator strategy.
/// Trades a selectable oscillator. With Stochastic, a %K cross above %D while %K is below Oversold buys and a %K cross below %D
/// while %K is above Overbought sells. With RSI or MFI, a reading below Oversold buys and a reading above Overbought sells.
/// An opposite signal reverses the position and a percent stop limits the loss.
/// </summary>
public class ChartOscillatorStrategy : Strategy
{
	/// <summary>
	/// Oscillator choices.
	/// </summary>
	public enum OscillatorChoice
	{
		/// <summary>
		/// Stochastic %K/%D.
		/// </summary>
		Stochastic,

		/// <summary>
		/// Relative Strength Index.
		/// </summary>
		Rsi,

		/// <summary>
		/// Money Flow Index.
		/// </summary>
		Mfi,
	}

	private readonly StrategyParam<OscillatorChoice> _choice;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _smoothK;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private SimpleMovingAverage _kSmoother;
	private SimpleMovingAverage _dAverage;
	private decimal? _prevK;
	private decimal? _prevD;

	/// <summary>
	/// Oscillator used for signals.
	/// </summary>
	public OscillatorChoice Choice
	{
		get => _choice.Value;
		set => _choice.Value = value;
	}

	/// <summary>
	/// Period of RSI and MFI.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Stochastic %K lookback.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K smoothing.
	/// </summary>
	public int SmoothK
	{
		get => _smoothK.Value;
		set => _smoothK.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
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
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ChartOscillatorStrategy()
	{
		_choice = Param(nameof(Choice), OscillatorChoice.Stochastic)
			.SetDisplay("Oscillator", "Oscillator used for signals", "Indicators");

		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Period of RSI and MFI", "Indicators");

		_kPeriod = Param(nameof(KPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic %K lookback", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Stochastic %D smoothing", "Indicators");

		_smoothK = Param(nameof(SmoothK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smooth %K", "Stochastic %K smoothing", "Indicators");

		_overbought = Param(nameof(Overbought), 80m)
			.SetDisplay("Overbought", "Overbought level", "Signals");

		_oversold = Param(nameof(Oversold), 20m)
			.SetDisplay("Oversold", "Oversold level", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");
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
		_kSmoother = null;
		_dAverage = null;
		_prevK = null;
		_prevD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_prevD = null;

		var rawK = new StochasticK { Length = KPeriod };
		var rsi = new RelativeStrengthIndex { Length = Length };
		var mfi = new MoneyFlowIndex { Length = Length };
		_kSmoother = new SimpleMovingAverage { Length = SmoothK };
		_dAverage = new SimpleMovingAverage { Length = DPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rawK, rsi, mfi, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
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
			{
				switch (Choice)
				{
					case OscillatorChoice.Rsi:
						DrawIndicator(oscillators, rsi);
						break;
					case OscillatorChoice.Mfi:
						DrawIndicator(oscillators, mfi);
						break;
					default:
						DrawIndicator(oscillators, rawK);
						break;
				}
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rawKValue, IIndicatorValue rsiValue, IIndicatorValue mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		bool buy;
		bool sell;

		switch (Choice)
		{
			case OscillatorChoice.Rsi:
			case OscillatorChoice.Mfi:
			{
				var value = Choice == OscillatorChoice.Rsi ? rsiValue : mfiValue;

				if (!value.IsFormed)
					return;

				var oscillator = value.ToDecimal();
				buy = oscillator < Oversold;
				sell = oscillator > Overbought;
				break;
			}
			default:
			{
				if (!rawKValue.IsFormed)
					return;

				var kValue = _kSmoother.Process(rawKValue.ToDecimal(), candle.OpenTime, true);

				if (!kValue.IsFormed)
					return;

				var k = kValue.ToDecimal();
				var dValue = _dAverage.Process(k, candle.OpenTime, true);

				if (!dValue.IsFormed)
					return;

				var d = dValue.ToDecimal();
				var prevK = _prevK;
				var prevD = _prevD;
				_prevK = k;
				_prevD = d;

				if (prevK is not decimal pk || prevD is not decimal pd)
					return;

				buy = pk <= pd && k > d && k < Oversold;
				sell = pk >= pd && k < d && k > Overbought;
				break;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (buy && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sell && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
