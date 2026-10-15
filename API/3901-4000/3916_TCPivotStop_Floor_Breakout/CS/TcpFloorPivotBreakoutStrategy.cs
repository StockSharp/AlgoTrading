using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TcpFloorPivotBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _cooldownCandles;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevMid;
	private bool _hasPrev;
	private int _cooldownRemaining;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public int CooldownCandles { get => _cooldownCandles.Value; set => _cooldownCandles.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TcpFloorPivotBreakoutStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 48).SetDisplay("Channel Period", "Channel lookback", "Indicators");
		_cooldownCandles = Param(nameof(CooldownCandles), 150).SetDisplay("Cooldown", "Candles between signals", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = default;
		_prevMid = default;
		_hasPrev = default;
		_cooldownRemaining = default;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevClose = 0;
		_prevMid = 0;
		_hasPrev = false;
		_cooldownRemaining = 0;

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		var mid = (highest + lowest) / 2;
		if (!_hasPrev) { _prevClose = close; _prevMid = mid; _hasPrev = true; return; }

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = close;
			_prevMid = mid;
			return;
		}

		if (_prevClose <= _prevMid && close > mid && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownCandles;
		}
		else if (_prevClose >= _prevMid && close < mid && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownCandles;
		}
		_prevClose = close;
		_prevMid = mid;
	}
}
