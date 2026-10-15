using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSIOMA strategy using RSI with overbought/oversold levels.
/// Buys when RSI crosses above oversold, sells when crosses below overbought.
/// </summary>
public class ExpRsiomaV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;

	private decimal? _prevRsi;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	public ExpRsiomaV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators");

		_overbought = Param(nameof(Overbought), 65m)
			.SetDisplay("Overbought", "Overbought RSI level", "Levels");

		_oversold = Param(nameof(Oversold), 35m)
			.SetDisplay("Oversold", "Oversold RSI level", "Levels");
	}

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
		}

		var indArea = CreateChartArea();
		if (indArea != null)
		{
			DrawIndicator(indArea, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsiValue;
			return;
		}

		if (_prevRsi == null)
		{
			_prevRsi = rsiValue;
			return;
		}

		// RSI crosses above oversold → buy signal
		if (_prevRsi.Value <= Oversold && rsiValue > Oversold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// RSI crosses below overbought → sell signal
		else if (_prevRsi.Value >= Overbought && rsiValue < Overbought && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevRsi = rsiValue;
	}
}
