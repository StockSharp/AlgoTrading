using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI Failure Swing strategy.
/// A trough of CCI below OversoldLevel that is higher than the previous such trough arms a long, which opens as CCI turns up from it;
/// peaks above OverboughtLevel arm shorts the same way. A position closes when CCI crosses back through the swing that armed it,
/// and a percent stop limits the loss.
/// </summary>
public class CciFailureSwingStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
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

	/// <summary>
	/// Period for CCI.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// CCI level below which troughs count.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// CCI level above which peaks count.
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
	public CciFailureSwingStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), -100m)
			.SetDisplay("Oversold Level", "CCI level below which troughs count", "Levels");

		_overboughtLevel = Param(nameof(OverboughtLevel), 100m)
			.SetDisplay("Overbought Level", "CCI level above which peaks count", "Levels");

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
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var oscillator = new CommodityChannelIndex { Length = CciPeriod };

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
		if (candle.State != CandleStates.Finished || !value.IsFormed || value.IsEmpty)
			return;

		var current = value.GetValue<decimal>();
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

			var longTrigger = current > last;
			var shortTrigger = current < last;
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
	}
}
