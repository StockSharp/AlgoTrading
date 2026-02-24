using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "XP Trade Manager Grid" MetaTrader expert.
/// Uses RSI for entry direction with grid-style position management.
/// </summary>
public class XpTradeManagerGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;

	private RelativeStrengthIndex _rsi;
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

	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	public XpTradeManagerGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal generation", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_rsiUpper = Param(nameof(RsiUpper), 65m)
			.SetDisplay("RSI Upper", "RSI threshold for sell", "Signals");

		_rsiLower = Param(nameof(RsiLower), 35m)
			.SetDisplay("RSI Lower", "RSI threshold for buy", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = null;
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
		{
			_prevRsi = rsiValue;
			return;
		}

		if (_prevRsi is null)
		{
			_prevRsi = rsiValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// RSI crosses below oversold -> buy
		var crossDown = _prevRsi.Value > RsiLower && rsiValue <= RsiLower;
		// RSI crosses above overbought -> sell
		var crossUp = _prevRsi.Value < RsiUpper && rsiValue >= RsiUpper;

		if (crossDown)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossUp)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevRsi = rsiValue;
	}
}
