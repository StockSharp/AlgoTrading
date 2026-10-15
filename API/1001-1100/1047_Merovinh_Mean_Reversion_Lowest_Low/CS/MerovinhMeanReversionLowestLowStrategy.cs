using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MerovinhMeanReversionLowestLowStrategy : Strategy
{
	private readonly StrategyParam<int> _bars;
	private readonly StrategyParam<decimal> _breakoutPercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private decimal _prevLow;
	private decimal _prevHigh;
	private int _barsFromSignal;

	public int Bars { get => _bars.Value; set => _bars.Value = value; }
	public decimal BreakoutPercent { get => _breakoutPercent.Value; set => _breakoutPercent.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MerovinhMeanReversionLowestLowStrategy()
	{
		_bars = Param(nameof(Bars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bars", "Lookback for highest/lowest", "General");
		_breakoutPercent = Param(nameof(BreakoutPercent), 0.4m)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Percent", "Minimum percentage change for new high/low", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highest = null;
		_lowest = null;
		_prevLow = 0m;
		_prevHigh = 0m;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_highest = new Highest { Length = Bars };
		_lowest = new Lowest { Length = Bars };
		_prevLow = 0;
		_prevHigh = 0;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestHigh, decimal lowestLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevLow = lowestLow;
			_prevHigh = highestHigh;
			return;
		}

		_barsFromSignal++;
		var lowBreak = _prevLow > 0m && lowestLow < _prevLow * (1m - BreakoutPercent / 100m);
		var highBreak = _prevHigh > 0m && highestHigh > _prevHigh * (1m + BreakoutPercent / 100m);

		if (_barsFromSignal >= SignalCooldownBars && lowBreak && Position == 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}

		if (highBreak && Position > 0)
			SellMarket();

		_prevLow = lowestLow;
		_prevHigh = highestHigh;
	}
}
