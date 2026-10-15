using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// London session breakout strategy using Highest/Lowest channels.
/// </summary>
public class LondonBreakOutClassicStrategy : Strategy
{
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private Highest _highest;
	private Lowest _lowest;
	private decimal _prevHigh;
	private decimal _prevLow;
	private int _barsSinceSignal;

	public int ChannelLength { get => _channelLength.Value; set => _channelLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public LondonBreakOutClassicStrategy()
	{
		_channelLength = Param(nameof(ChannelLength), 20)
			.SetDisplay("Channel Length", "Period for breakout channel", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetDisplay("Cooldown Bars", "Min bars between signals", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = null;
		_lowest = null;
		_prevHigh = 0;
		_prevLow = 0;
		_barsSinceSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = ChannelLength };
		_lowest = new Lowest { Length = ChannelLength };
		_prevHigh = 0;
		_prevLow = 0;
		_barsSinceSignal = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highVal, decimal lowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevHigh = highVal;
			_prevLow = lowVal;
			return;
		}

		if (_barsSinceSignal < CooldownBars)
		{
			_prevHigh = highVal;
			_prevLow = lowVal;
			return;
		}

		// Breakout above channel high
		if (candle.ClosePrice > _prevHigh && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
		// Breakout below channel low
		else if (candle.ClosePrice < _prevLow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}

		_prevHigh = highVal;
		_prevLow = lowVal;
	}
}
