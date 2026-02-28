using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy requiring strict moving average alignment
/// and rising volatility (ATR) before entering trades.
/// Exits when alignment is lost.
/// </summary>
public class TrueSort1001Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _midLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevFast;
	private decimal _prevMid;
	private decimal _prevSlow;
	private decimal _prevAtr;
	private decimal _entryPrice;

	public TrueSort1001Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastLength = Param(nameof(FastLength), 10)
			.SetDisplay("Fast SMA", "Fast SMA period.", "Indicators");

		_midLength = Param(nameof(MidLength), 50)
			.SetDisplay("Mid SMA", "Medium SMA period.", "Indicators");

		_slowLength = Param(nameof(SlowLength), 200)
			.SetDisplay("Slow SMA", "Slow SMA period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for volatility filter.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int MidLength
	{
		get => _midLength.Value;
		set => _midLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevMid = 0;
		_prevSlow = 0;
		_prevAtr = 0;
		_entryPrice = 0;

		var fast = new SimpleMovingAverage { Length = FastLength };
		var mid = new SimpleMovingAverage { Length = MidLength };
		var slow = new SimpleMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, mid, slow, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, mid);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal midVal, decimal slowVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevMid == 0 || _prevSlow == 0)
		{
			_prevFast = fastVal;
			_prevMid = midVal;
			_prevSlow = slowVal;
			_prevAtr = atrVal;
			return;
		}

		var close = candle.ClosePrice;
		var bullishAligned = fastVal > midVal && midVal > slowVal;
		var bearishAligned = fastVal < midVal && midVal < slowVal;
		var atrRising = atrVal > _prevAtr;

		// Exit on alignment lost
		if (Position > 0 && !bullishAligned)
		{
			SellMarket();
		}
		else if (Position < 0 && !bearishAligned)
		{
			BuyMarket();
		}

		// Entry on alignment + rising ATR
		if (Position == 0)
		{
			if (bullishAligned && atrRising && close > fastVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (bearishAligned && atrRising && close < fastVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevFast = fastVal;
		_prevMid = midVal;
		_prevSlow = slowVal;
		_prevAtr = atrVal;
	}
}
