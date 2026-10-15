using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// K Failure Swing strategy.
/// A trough of %K below OversoldLevel that is higher than the previous such trough arms a long, which opens when %K then crosses
/// above %D; peaks above OverboughtLevel arm shorts the same way. A position closes when %K crosses back through the swing that armed it,
/// and a percent stop limits the loss.
/// </summary>
public class StochasticFailureSwingStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	// The last two values, the last trough and peak in the extreme zones, and the swings that arm an entry.
	private decimal? _last;
	private decimal? _beforeLast;
	private decimal? _lastTrough;
	private decimal? _lastPeak;
	private decimal? _armedLong;
	private decimal? _armedShort;
	private decimal _exitLevel;
	private decimal? _prevK;
	private decimal? _prevD;

	/// <summary>
	/// Period for %K.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Period for %D.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// %K level below which troughs count.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// %K level above which peaks count.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
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
	public StochasticFailureSwingStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Period for %K", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Period for %D", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetDisplay("Oversold Level", "%K level below which troughs count", "Levels");

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetDisplay("Overbought Level", "%K level above which peaks count", "Levels");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_last = null;
		_beforeLast = null;
		_lastTrough = null;
		_lastPeak = null;
		_armedLong = null;
		_armedShort = null;
		_exitLevel = default;
		_prevK = null;
		_prevD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var oscillator = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(oscillator, ProcessCandle)
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
			DrawIndicator(area, oscillator);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished || !value.IsFormed || value.IsEmpty || value is not IStochasticOscillatorValue { K: decimal current, D: decimal signal })
			return;

		if (_last is decimal last)
		{
			// The previous value is a trough or a peak once the current one turns away from it.
			if (_beforeLast is decimal beforeLast)
			{
				if (last < beforeLast && last < current && last < OversoldLevel)
				{
					_armedLong = _lastTrough is decimal trough && last > trough ? last : null;
					_lastTrough = last;
				}
				else if (last > beforeLast && last > current && last > OverboughtLevel)
				{
					_armedShort = _lastPeak is decimal peak && last < peak ? last : null;
					_lastPeak = last;
				}
			}

			var longTrigger = _prevK is decimal pk && _prevD is decimal pd && pk <= pd && current > signal;
			var shortTrigger = _prevK is decimal pk2 && _prevD is decimal pd2 && pk2 >= pd2 && current < signal;
			var armedLong = longTrigger ? _armedLong : null;
			var armedShort = shortTrigger ? _armedShort : null;

			if (longTrigger)
				_armedLong = null;

			if (shortTrigger)
				_armedShort = null;

			if (IsFormedAndOnlineAndAllowTrading())
			{
				if (Position > 0)
				{
					if (current < _exitLevel)
						SellMarket(Position);
				}
				else if (Position < 0)
				{
					if (current > _exitLevel)
						BuyMarket(-Position);
				}
				else if (armedLong is decimal longSwing)
				{
					BuyMarket(Volume);
					_exitLevel = longSwing;
				}
				else if (armedShort is decimal shortSwing)
				{
					SellMarket(Volume);
					_exitLevel = shortSwing;
				}
			}
		}

		_beforeLast = _last;
		_last = current;
		_prevK = current;
		_prevD = signal;
	}
}
