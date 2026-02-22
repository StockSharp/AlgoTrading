using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD histogram scalping with EMA filter.
/// </summary>
public class MacdAggressiveScalpSimpleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFast;
	private EMA _emaSlow;
	private EMA _emaFilter;
	private decimal _prevMacd;
	private bool _initialized;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdAggressiveScalpSimpleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetGreaterThanZero()
			.SetDisplay("Fast", "MACD fast", "MACD");
		_slowLength = Param(nameof(SlowLength), 26).SetGreaterThanZero()
			.SetDisplay("Slow", "MACD slow", "MACD");
		_emaLength = Param(nameof(EmaLength), 50).SetGreaterThanZero()
			.SetDisplay("EMA", "EMA trend filter", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevMacd = 0;
		_initialized = false;

		_emaFast = new EMA { Length = FastLength };
		_emaSlow = new EMA { Length = SlowLength };
		_emaFilter = new EMA { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _emaFilter, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawIndicator(area, _emaFilter);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_emaFilter.IsFormed)
			return;

		var macdLine = fast - slow;

		if (!_initialized)
		{
			_prevMacd = macdLine;
			_initialized = true;
			return;
		}

		var crossUp = _prevMacd <= 0 && macdLine > 0;
		var crossDown = _prevMacd >= 0 && macdLine < 0;

		if (crossUp && candle.ClosePrice >= ema && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && candle.ClosePrice <= ema && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		// Exit on histogram momentum reversal
		if (Position > 0 && macdLine < _prevMacd && macdLine < 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && macdLine > _prevMacd && macdLine > 0)
			BuyMarket(Math.Abs(Position));

		_prevMacd = macdLine;
	}
}
