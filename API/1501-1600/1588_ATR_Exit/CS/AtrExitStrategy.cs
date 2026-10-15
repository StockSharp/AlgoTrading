using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with ATR based trailing exits.
/// </summary>
public class AtrExitStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLen;
	private readonly StrategyParam<int> _slowLen;
	private readonly StrategyParam<int> _atrLen;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _prevFast;
	private decimal _prevSlow;

	public int FastLength { get => _fastLen.Value; set => _fastLen.Value = value; }
	public int SlowLength { get => _slowLen.Value; set => _slowLen.Value = value; }
	public int AtrLength { get => _atrLen.Value; set => _atrLen.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AtrExitStrategy()
	{
		_fastLen = Param(nameof(FastLength), 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "General");
		_slowLen = Param(nameof(SlowLength), 30).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "General");
		_atrLen = Param(nameof(AtrLength), 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(fast, slow, atr, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (Position == 0)
		{
			if (crossUp)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - 1.5m * atr;
				BuyMarket();
			}
			else if (crossDown)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + 1.5m * atr;
				SellMarket();
			}
		}
		else if (Position > 0)
		{
			// Trailing stop using ATR
			var newStop = candle.ClosePrice - 1.5m * atr;
			if (newStop > _stopPrice)
				_stopPrice = newStop;

			if (candle.ClosePrice < _stopPrice || crossDown)
				SellMarket();
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + 1.5m * atr;
			if (newStop < _stopPrice)
				_stopPrice = newStop;

			if (candle.ClosePrice > _stopPrice || crossUp)
				BuyMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
