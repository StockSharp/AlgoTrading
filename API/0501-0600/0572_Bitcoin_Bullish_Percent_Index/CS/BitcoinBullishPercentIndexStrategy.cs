using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bitcoin Bullish Percent Index strategy.
/// RSI approximates the bullish percent index: RSI crossing above Oversold goes long, RSI crossing below Overbought goes short,
/// and the opposite signal reverses the position.
/// </summary>
public class BitcoinBullishPercentIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevRsi;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level whose downward cross opens a short.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// RSI level whose upward cross opens a long.
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
	/// Constructor.
	/// </summary>
	public BitcoinBullishPercentIndexStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "RSI level whose downward cross opens a short", "Indicators");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "RSI level whose upward cross opens a long", "Indicators");

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
		_prevRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = null;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

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

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prev = _prevRsi;
		_prevRsi = rsi;

		if (prev is not decimal prevRsi)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (prevRsi <= Oversold && rsi > Oversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (prevRsi >= Overbought && rsi < Overbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
