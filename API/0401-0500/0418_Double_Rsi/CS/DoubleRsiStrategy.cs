using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double RSI strategy.
/// An RSI on the trading timeframe and another on MTFTimeframe. A long opens when the trading RSI crosses up out of
/// the oversold zone while the higher-timeframe RSI is rising (bullish); a short opens when it crosses down out of the
/// overbought zone while the higher-timeframe RSI is falling (bearish). The opposite RSI exit closes the position and an
/// optional percent take-profit locks in gains.
/// </summary>
public class DoubleRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _mtfTimeframe;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<bool> _useTp;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal? _prevRsi;
	private decimal? _mtfRsi;
	private decimal? _prevMtfRsi;

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI period on both timeframes.
	/// </summary>
	public int RSILength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Higher timeframe for the confirming RSI.
	/// </summary>
	public DataType MTFTimeframe
	{
		get => _mtfTimeframe.Value;
		set => _mtfTimeframe.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Enable the take-profit.
	/// </summary>
	public bool UseTP
	{
		get => _useTp.Value;
		set => _useTp.Value = value;
	}

	/// <summary>
	/// Take-profit percentage from the entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DoubleRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Trading timeframe", "General");

		_rsiLength = Param(nameof(RSILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period on both timeframes", "RSI");

		_mtfTimeframe = Param(nameof(MTFTimeframe), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("MTF Timeframe", "Higher timeframe for the confirming RSI", "RSI");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "RSI oversold level", "RSI");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "RSI overbought level", "RSI");

		_useTp = Param(nameof(UseTP), false)
			.SetDisplay("Use Take Profit", "Enable the percent take-profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take-profit percentage from the entry price", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, MTFTimeframe)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = null;
		_mtfRsi = null;
		_prevMtfRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = null;
		_mtfRsi = null;
		_prevMtfRsi = null;

		var rsi = new RelativeStrengthIndex { Length = RSILength };
		var mtfRsi = new RelativeStrengthIndex { Length = RSILength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ProcessCandle)
			.Start();

		SubscribeCandles(MTFTimeframe)
			.BindEx(mtfRsi, ProcessMtfCandle)
			.Start();

		if (UseTP)
			StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
			{
				DrawIndicator(rsiArea, rsi);
				DrawIndicator(rsiArea, mtfRsi);
			}
		}
	}

	private void ProcessMtfCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed)
			return;

		_prevMtfRsi = _mtfRsi;
		_mtfRsi = rsiValue.ToDecimal();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed)
			return;

		var rsi = rsiValue.ToDecimal();
		var prevRsi = _prevRsi;
		_prevRsi = rsi;

		if (prevRsi is not decimal lastRsi || _mtfRsi is not decimal mtfRsi || _prevMtfRsi is not decimal prevMtfRsi)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var exitsOversold = lastRsi < Oversold && rsi >= Oversold;
		var exitsOverbought = lastRsi > Overbought && rsi <= Overbought;

		var longSignal = exitsOversold && mtfRsi > prevMtfRsi;
		var shortSignal = exitsOverbought && mtfRsi < prevMtfRsi;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && exitsOverbought)
			SellMarket(Position);
		else if (Position < 0 && exitsOversold)
			BuyMarket(-Position);
	}
}
