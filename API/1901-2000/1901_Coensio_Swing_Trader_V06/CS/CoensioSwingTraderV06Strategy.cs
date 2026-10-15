using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Coensio Swing Trader based on Donchian channel breakouts with optional trailing stop and break-even.
/// Manually tracks highest high and lowest low over a rolling window.
/// </summary>
public class CoensioSwingTraderV06Strategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal _entryPrice;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CoensioSwingTraderV06Strategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Period for Donchian Channel", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_highs.Clear();
		_lows.Clear();
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent)
		);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > ChannelPeriod)
			_highs.RemoveAt(0);
		if (_lows.Count > ChannelPeriod)
			_lows.RemoveAt(0);

		if (_highs.Count < ChannelPeriod)
			return;

		var upper = decimal.MinValue;
		var lower = decimal.MaxValue;
		for (var i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > upper) upper = _highs[i];
			if (_lows[i] < lower) lower = _lows[i];
		}

		var price = candle.ClosePrice;

		if (price > upper && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = price;
		}
		else if (price < lower && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = price;
		}
	}
}
