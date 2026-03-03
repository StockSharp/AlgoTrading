using System;
using System.Collections.Generic;

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

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private ExponentialMovingAverage _emaFilter;
	private decimal _prevMacd;
	private bool _initialized;
	private int _cooldown;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevMacd = default;
		_initialized = false;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_emaFast = new ExponentialMovingAverage { Length = FastLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowLength };
		_emaFilter = new ExponentialMovingAverage { Length = EmaLength };

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

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMacd = macdLine;
			return;
		}

		var crossUp = _prevMacd <= 0 && macdLine > 0;
		var crossDown = _prevMacd >= 0 && macdLine < 0;

		// Entry with EMA filter
		if (crossUp && candle.ClosePrice >= ema && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldown = 5;
		}
		else if (crossDown && candle.ClosePrice <= ema && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldown = 5;
		}

		_prevMacd = macdLine;
	}
}
