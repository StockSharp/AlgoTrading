using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD-based strategy with RSI relative strength filter.
/// </summary>
public class MacdOfRelativeStrenghtStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFast;
	private EMA _emaSlow;
	private RelativeStrengthIndex _rsi;
	private decimal _prevMacd;
	private bool _initialized;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdOfRelativeStrenghtStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetDisplay("Fast", "Fast EMA", "MACD");
		_slowLength = Param(nameof(SlowLength), 26).SetDisplay("Slow", "Slow EMA", "MACD");
		_rsiLength = Param(nameof(RsiLength), 14).SetDisplay("RSI", "RSI period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevMacd = 0; _initialized = false;

		_emaFast = new EMA { Length = FastLength };
		_emaSlow = new EMA { Length = SlowLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_emaFast, _emaSlow, _rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_rsi.IsFormed)
			return;

		var macd = fast - slow;

		if (!_initialized) { _prevMacd = macd; _initialized = true; return; }

		var crossUp = _prevMacd <= 0 && macd > 0;
		var crossDown = _prevMacd >= 0 && macd < 0;

		// RSI filter: buy when not overbought, sell when not oversold
		if (crossUp && rsi < 70 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && rsi > 30 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevMacd = macd;
	}
}
