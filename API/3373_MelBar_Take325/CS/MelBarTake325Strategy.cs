namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// MelBar Take325 strategy: SMA reversal with RSI exit filter.
/// Enters on SMA direction change, exits when RSI reaches extreme levels.
/// </summary>
public class MelBarTake325Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiExitLevel;

	private decimal _prevSma;
	private decimal _prevPrevSma;
	private bool _hasPrev2;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiExitLevel { get => _rsiExitLevel.Value; set => _rsiExitLevel.Value = value; }

	public MelBarTake325Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_smaPeriod = Param(nameof(SmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_rsiExitLevel = Param(nameof(RsiExitLevel), 75m)
			.SetDisplay("RSI Exit Level", "RSI level to close long; 100-level closes short", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev2 = false;
		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal rsi)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev2)
		{
			// Exit on RSI extremes
			if (Position > 0 && rsi > RsiExitLevel)
				SellMarket();
			else if (Position < 0 && rsi < (100 - RsiExitLevel))
				BuyMarket();

			// Entry on SMA reversal (peak/trough)
			if (Position <= 0 && _prevPrevSma < _prevSma && _prevSma > sma)
				BuyMarket();
			else if (Position >= 0 && _prevPrevSma > _prevSma && _prevSma < sma)
				SellMarket();
		}

		_prevPrevSma = _prevSma;
		_prevSma = sma;
		_hasPrev2 = true;
	}
}
