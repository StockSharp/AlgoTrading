using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified conversion of the FxNode Safe Tunnel EA.
/// Uses Highest/Lowest channel (tunnel) with ATR-based stops.
/// Buys near the lower boundary and sells near the upper boundary.
/// </summary>
public class FxNodeSafeTunnelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _touchPct;

	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal TouchPct { get => _touchPct.Value; set => _touchPct.Value = value; }

	public FxNodeSafeTunnelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Lookback for Highest/Lowest channel", "Indicator");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR lookback for stops", "Indicator");

		_touchPct = Param(nameof(TouchPct), 0.2m)
			.SetDisplay("Touch %", "How close price must be to channel boundary (0-1)", "Indicator");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var channelWidth = high - low;
		if (channelWidth <= 0)
			return;

		var touchZone = channelWidth * TouchPct;
		var close = candle.ClosePrice;

		// Check stop/take for active positions
		if (Position > 0)
		{
			// Exit long: price near upper channel or stop loss
			if (close >= high - touchZone || (_entryPrice > 0 && close < _entryPrice - atrVal * 2))
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			// Exit short: price near lower channel or stop loss
			if (close <= low + touchZone || (_entryPrice > 0 && close > _entryPrice + atrVal * 2))
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		// Entry signals
		if (Position <= 0 && close <= low + touchZone)
		{
			// Price near lower boundary - buy
			if (Position < 0) BuyMarket(); // close short first
			BuyMarket();
			_entryPrice = close;
		}
		else if (Position >= 0 && close >= high - touchZone)
		{
			// Price near upper boundary - sell
			if (Position > 0) SellMarket(); // close long first
			SellMarket();
			_entryPrice = close;
		}
	}
}
