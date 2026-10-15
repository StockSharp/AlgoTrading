using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe Parabolic SAR strategy.
/// Parabolic SAR is calculated on the main, a higher and a lower timeframe. A long opens when the main candle closes above every SAR
/// selected by LongSource, a short when it closes below every SAR selected by ShortSource, reversing an opposite position.
/// A position closes when price crosses the main timeframe SAR the other way. Percent stop loss and take profit are handled by
/// protection and a percent trailing stop follows the best price since entry.
/// </summary>
public class MultiTimeframeParabolicSarStrategy : Strategy
{
	/// <summary>
	/// Which SAR levels must confirm an entry.
	/// </summary>
	public enum SarSources
	{
		/// <summary>
		/// Main timeframe SAR only.
		/// </summary>
		Current,

		/// <summary>
		/// Main and higher timeframe SAR.
		/// </summary>
		CurrentAndHigher,

		/// <summary>
		/// Main and lower timeframe SAR.
		/// </summary>
		CurrentAndLower,

		/// <summary>
		/// Main, higher and lower timeframe SAR.
		/// </summary>
		All,
	}

	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<SarSources> _longSource;
	private readonly StrategyParam<SarSources> _shortSource;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _lowerCandleType;

	private decimal? _higherSar;
	private decimal? _higherClose;
	private decimal? _lowerSar;
	private decimal? _lowerClose;
	private decimal _bestPrice;

	/// <summary>
	/// Initial acceleration factor.
	/// </summary>
	public decimal Acceleration
	{
		get => _acceleration.Value;
		set => _acceleration.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor.
	/// </summary>
	public decimal MaxAcceleration
	{
		get => _maxAcceleration.Value;
		set => _maxAcceleration.Value = value;
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
	/// Trailing stop percentage from the best price since entry.
	/// </summary>
	public decimal TrailingPercent
	{
		get => _trailingPercent.Value;
		set => _trailingPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// SAR levels price must be above for a long entry.
	/// </summary>
	public SarSources LongSource
	{
		get => _longSource.Value;
		set => _longSource.Value = value;
	}

	/// <summary>
	/// SAR levels price must be below for a short entry.
	/// </summary>
	public SarSources ShortSource
	{
		get => _shortSource.Value;
		set => _shortSource.Value = value;
	}

	/// <summary>
	/// Main candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Lower timeframe candle type.
	/// </summary>
	public DataType LowerCandleType
	{
		get => _lowerCandleType.Value;
		set => _lowerCandleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MultiTimeframeParabolicSarStrategy()
	{
		_acceleration = Param(nameof(Acceleration), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Acceleration", "Initial acceleration factor", "Indicators");

		_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Max Acceleration", "Maximum acceleration factor", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_trailingPercent = Param(nameof(TrailingPercent), 0.5m)
			.SetNotNegative()
			.SetDisplay("Trailing %", "Trailing stop percentage from the best price", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

		_longSource = Param(nameof(LongSource), SarSources.All)
			.SetDisplay("Long Source", "SAR levels price must be above for a long", "Signals");

		_shortSource = Param(nameof(ShortSource), SarSources.All)
			.SetDisplay("Short Source", "SAR levels price must be below for a short", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Main timeframe", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Higher Candle Type", "Higher timeframe", "General");

		_lowerCandleType = Param(nameof(LowerCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Lower Candle Type", "Lower timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherCandleType), (Security, LowerCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_higherSar = null;
		_higherClose = null;
		_lowerSar = null;
		_lowerClose = null;
		_bestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var sar = CreateSar();
		var higherSar = CreateSar();
		var lowerSar = CreateSar();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, ProcessCandle)
			.Start();

		SubscribeCandles(HigherCandleType)
			.Bind(higherSar, (candle, value) =>
			{
				if (candle.State != CandleStates.Finished || !higherSar.IsFormed)
					return;

				_higherSar = value;
				_higherClose = candle.ClosePrice;
			})
			.Start();

		SubscribeCandles(LowerCandleType)
			.Bind(lowerSar, (candle, value) =>
			{
				if (candle.State != CandleStates.Finished || !lowerSar.IsFormed)
					return;

				_lowerSar = value;
				_lowerClose = candle.ClosePrice;
			})
			.Start();

		StartProtection(
			TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true,
			isLocalStop: true);

		// The stops have to see prices between candles, not only at their close.
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
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private ParabolicSar CreateSar()
	{
		return new ParabolicSar
		{
			Acceleration = Acceleration,
			AccelerationStep = Acceleration,
			AccelerationMax = MaxAcceleration,
		};
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private bool IsAbove(SarSources source, decimal close, decimal sar)
	{
		if (close <= sar)
			return false;

		var useHigher = source is SarSources.CurrentAndHigher or SarSources.All;
		var useLower = source is SarSources.CurrentAndLower or SarSources.All;

		if (useHigher && (_higherSar is not decimal h || _higherClose is not decimal hc || hc <= h))
			return false;

		if (useLower && (_lowerSar is not decimal l || _lowerClose is not decimal lc || lc <= l))
			return false;

		return true;
	}

	private bool IsBelow(SarSources source, decimal close, decimal sar)
	{
		if (close >= sar)
			return false;

		var useHigher = source is SarSources.CurrentAndHigher or SarSources.All;
		var useLower = source is SarSources.CurrentAndLower or SarSources.All;

		if (useHigher && (_higherSar is not decimal h || _higherClose is not decimal hc || hc >= h))
			return false;

		if (useLower && (_lowerSar is not decimal l || _lowerClose is not decimal lc || lc >= l))
			return false;

		return true;
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (IsAbove(LongSource, close, sarValue) && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_bestPrice = close;
			return;
		}

		if (IsBelow(ShortSource, close, sarValue) && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_bestPrice = close;
			return;
		}

		if (Position > 0)
		{
			_bestPrice = Math.Max(_bestPrice, candle.HighPrice);

			if (close < sarValue || (TrailingPercent > 0 && close <= _bestPrice * (1 - TrailingPercent / 100m)))
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			_bestPrice = _bestPrice == 0 ? candle.LowPrice : Math.Min(_bestPrice, candle.LowPrice);

			if (close > sarValue || (TrailingPercent > 0 && close >= _bestPrice * (1 + TrailingPercent / 100m)))
				BuyMarket(-Position);
		}
	}
}
