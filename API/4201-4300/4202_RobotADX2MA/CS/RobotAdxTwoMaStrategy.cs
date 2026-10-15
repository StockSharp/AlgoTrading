using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RobotADX2MA: Dual EMA crossover with RSI confirmation and ATR stops.
/// </summary>
public class RobotAdxTwoMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public RobotAdxTwoMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");
		_fastEmaLength = Param(nameof(FastEmaLength), 5)
			.SetDisplay("Fast EMA Length", "Fast EMA period.", "Indicators");
		_slowEmaLength = Param(nameof(SlowEmaLength), 12)
			.SetDisplay("Slow EMA Length", "Slow EMA period.", "Indicators");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0; _prevSlow = 0; _entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFast = 0; _prevSlow = 0; _entryPrice = 0;
		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, rsi, atr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, fastEma); DrawIndicator(area, slowEma); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal rsiVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevFast == 0 || _prevSlow == 0 || atrVal <= 0) { _prevFast = fastVal; _prevSlow = slowVal; return; }
		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if ((fastVal < slowVal && _prevFast >= _prevSlow) || close <= _entryPrice - atrVal * 2m) { SellMarket(); _entryPrice = 0; }
		}
		else if (Position < 0)
		{
			if ((fastVal > slowVal && _prevFast <= _prevSlow) || close >= _entryPrice + atrVal * 2m) { BuyMarket(); _entryPrice = 0; }
		}

		if (Position == 0)
		{
			if (fastVal > slowVal && _prevFast <= _prevSlow && rsiVal > 50) { _entryPrice = close; BuyMarket(); }
			else if (fastVal < slowVal && _prevFast >= _prevSlow && rsiVal < 50) { _entryPrice = close; SellMarket(); }
		}
		_prevFast = fastVal; _prevSlow = slowVal;
	}
}
