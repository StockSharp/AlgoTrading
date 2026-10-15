using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA RSI swing trend filter strategy.
/// The fast EMA crossing above the slow EMA with the close above the trend EMA goes long, the bearish cross with the close below
/// it goes short. With UseRsiFilter longs need RSI below RsiMaxLong and shorts RSI above RsiMinShort. With ExitOnOpposite an
/// opposite EMA cross closes the position even when the entry filters reject a new trade.
/// </summary>
public class EmaRsiSwingTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastPeriod;
	private readonly StrategyParam<int> _emaSlowPeriod;
	private readonly StrategyParam<int> _emaTrendPeriod;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<decimal> _rsiMaxLong;
	private readonly StrategyParam<decimal> _rsiMinShort;
	private readonly StrategyParam<bool> _requireCloseConfirm;
	private readonly StrategyParam<bool> _exitOnOpposite;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EmaFastPeriod
	{
		get => _emaFastPeriod.Value;
		set => _emaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaSlowPeriod
	{
		get => _emaSlowPeriod.Value;
		set => _emaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Trend EMA period.
	/// </summary>
	public int EmaTrendPeriod
	{
		get => _emaTrendPeriod.Value;
		set => _emaTrendPeriod.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Use the RSI filter.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}

	/// <summary>
	/// RSI below which longs are allowed.
	/// </summary>
	public decimal RsiMaxLong
	{
		get => _rsiMaxLong.Value;
		set => _rsiMaxLong.Value = value;
	}

	/// <summary>
	/// RSI above which shorts are allowed.
	/// </summary>
	public decimal RsiMinShort
	{
		get => _rsiMinShort.Value;
		set => _rsiMinShort.Value = value;
	}

	/// <summary>
	/// Act only on closed candles.
	/// </summary>
	public bool RequireCloseConfirm
	{
		get => _requireCloseConfirm.Value;
		set => _requireCloseConfirm.Value = value;
	}

	/// <summary>
	/// Close the position on an opposite EMA cross.
	/// </summary>
	public bool ExitOnOpposite
	{
		get => _exitOnOpposite.Value;
		set => _exitOnOpposite.Value = value;
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
	public EmaRsiSwingTrendFilterStrategy()
	{
		_emaFastPeriod = Param(nameof(EmaFastPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_emaSlowPeriod = Param(nameof(EmaSlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_emaTrendPeriod = Param(nameof(EmaTrendPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA", "Trend EMA period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_useRsiFilter = Param(nameof(UseRsiFilter), true)
			.SetDisplay("Use RSI Filter", "Use the RSI filter", "Filters");

		_rsiMaxLong = Param(nameof(RsiMaxLong), 70m)
			.SetDisplay("RSI Max Long", "RSI below which longs are allowed", "Filters");

		_rsiMinShort = Param(nameof(RsiMinShort), 30m)
			.SetDisplay("RSI Min Short", "RSI above which shorts are allowed", "Filters");

		_requireCloseConfirm = Param(nameof(RequireCloseConfirm), true)
			.SetDisplay("Close Confirm", "Act only on closed candles", "Filters");

		_exitOnOpposite = Param(nameof(ExitOnOpposite), true)
			.SetDisplay("Exit On Opposite", "Close the position on an opposite EMA cross", "Exits");

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
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fast = new ExponentialMovingAverage { Length = EmaFastPeriod };
		var slow = new ExponentialMovingAverage { Length = EmaSlowPeriod };
		var trend = new ExponentialMovingAverage { Length = EmaTrendPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, trend, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawIndicator(area, trend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal trend, decimal rsi)
	{
		// Signals are always evaluated on closed candles, which is what RequireCloseConfirm asks for.
		if (candle.State != CandleStates.Finished)
			return;

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = pf <= ps && fast > slow;
		var crossDown = pf >= ps && fast < slow;
		var close = candle.ClosePrice;

		var longOk = crossUp && close > trend && (!UseRsiFilter || rsi < RsiMaxLong);
		var shortOk = crossDown && close < trend && (!UseRsiFilter || rsi > RsiMinShort);

		if (longOk && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortOk && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (ExitOnOpposite)
		{
			if (Position > 0 && crossDown)
				SellMarket(Position);
			else if (Position < 0 && crossUp)
				BuyMarket(-Position);
		}
	}
}
