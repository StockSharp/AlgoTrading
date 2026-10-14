using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe Bollinger Bands strategy.
/// Bollinger Bands run on the trading timeframe and on MtfCandleType. A long opens when the close is below the
/// higher-timeframe lower band and a short when it is above the higher-timeframe upper band; with UseMaFilter the close
/// must also be above (long) or below (short) the EMA. A long exits on a close above the trading-timeframe upper band,
/// a short on a close below its lower band, and SLPercent sets a percent stop-loss.
/// </summary>
public class MtfBbStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _mtfCandleType;

	private decimal? _mtfUpper;
	private decimal? _mtfLower;

	/// <summary>
	/// Bollinger Bands period on both timeframes.
	/// </summary>
	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier on both timeframes.
	/// </summary>
	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Enable the EMA trend filter.
	/// </summary>
	public bool UseMaFilter
	{
		get => _useMaFilter.Value;
		set => _useMaFilter.Value = value;
	}

	/// <summary>
	/// EMA filter period.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage. 0 disables it.
	/// </summary>
	public decimal SLPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
	}

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType MtfCandleType
	{
		get => _mtfCandleType.Value;
		set => _mtfCandleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MtfBbStrategy()
	{
		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

		_useMaFilter = Param(nameof(UseMaFilter), false)
			.SetDisplay("Use MA Filter", "Require the close on the trade side of the EMA", "Filter");

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "EMA filter period", "Filter");

		_slPercent = Param(nameof(SLPercent), 2m)
			.SetNotNegative()
			.SetDisplay("SL %", "Stop-loss percentage, 0 disables", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Trading timeframe", "General");

		_mtfCandleType = Param(nameof(MtfCandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("MTF Candle type", "Higher timeframe for the entry bands", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, MtfCandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_mtfUpper = null;
		_mtfLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_mtfUpper = null;
		_mtfLower = null;

		var bollinger = new BollingerBands { Length = BBLength, Width = BBMultiplier };
		var mtfBollinger = new BollingerBands { Length = BBLength, Width = BBMultiplier };
		var ma = new ExponentialMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ma, ProcessCandle)
			.Start();

		SubscribeCandles(MtfCandleType)
			.BindEx(mtfBollinger, ProcessMtfCandle)
			.Start();

		if (SLPercent > 0)
			StartProtection(new Unit(), new Unit(SLPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMtfCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		_mtfUpper = upper;
		_mtfLower = lower;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || (UseMaFilter && !maValue.IsFormed))
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		if (_mtfUpper is not decimal mtfUpper || _mtfLower is not decimal mtfLower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var ma = UseMaFilter ? maValue.ToDecimal() : 0m;

		var longSignal = close < mtfLower && (!UseMaFilter || close > ma);
		var shortSignal = close > mtfUpper && (!UseMaFilter || close < ma);

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close > upper)
			SellMarket(Position);
		else if (Position < 0 && close < lower)
			BuyMarket(-Position);
	}
}
