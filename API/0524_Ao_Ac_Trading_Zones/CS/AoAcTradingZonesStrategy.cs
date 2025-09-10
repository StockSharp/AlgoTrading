using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AO/AC trading zones strategy.
/// Builds long positions when momentum bars are rising above the Alligator teeth line.
/// </summary>
public class AoAcTradingZonesStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SmoothedMovingAverage _teeth = new() { Length = 8 };
	private readonly SimpleMovingAverage _aoFast = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlow = new() { Length = 34 };
	private readonly SimpleMovingAverage _acMa = new() { Length = 5 };

	private readonly decimal?[] _teethBuffer = new decimal?[6];
	private readonly decimal?[] _highs = new decimal?[5];
	private readonly decimal?[] _lows = new decimal?[5];

	private int _bufferCount;
	private int _greenBars;
	private decimal? _stopLoss;
	private int _trend;
	private int _prevTrend;
	private decimal _prevClose;
	private decimal _prevAo;
	private decimal _prevAc;
	private int _entryCount;
	private decimal? _upFractalActivation;
	private decimal? _downFractalActivation;

	/// <summary>
	/// EMA period for filter.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Candle series type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AoAcTradingZonesStrategy"/> class.
	/// </summary>
	public AoAcTradingZonesStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 100)
			.SetDisplay("EMA Length", "EMA period", "Settings")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		Array.Clear(_teethBuffer);
		Array.Clear(_highs);
		Array.Clear(_lows);
		_bufferCount = 0;
		_greenBars = 0;
		_stopLoss = null;
		_trend = 0;
		_prevTrend = 0;
		_prevClose = 0;
		_prevAo = 0;
		_prevAc = 0;
		_entryCount = 0;
		_upFractalActivation = null;
		_downFractalActivation = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var filterEma = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(filterEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, filterEma);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal filterEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;

		var smmaValue = _teeth.Process(hl2);
		if (!smmaValue.IsFinal)
			return;

		var teethRaw = smmaValue.GetValue<decimal>();
		for (var i = 0; i < 5; i++)
			_teethBuffer[i] = _teethBuffer[i + 1];
		_teethBuffer[5] = teethRaw;
		decimal? teeth = null;
		if (_bufferCount >= 5)
			teeth = _teethBuffer[0];
		else
			_bufferCount++;

		var aoFastValue = _aoFast.Process(hl2);
		var aoSlowValue = _aoSlow.Process(hl2);
		if (!aoFastValue.IsFinal || !aoSlowValue.IsFinal || teeth is null)
			return;

		var ao = aoFastValue.GetValue<decimal>() - aoSlowValue.GetValue<decimal>();
		var acMaValue = _acMa.Process(ao);
		if (!acMaValue.IsFinal)
			return;
		var ac = ao - acMaValue.GetValue<decimal>();

		for (var i = 0; i < 4; i++)
		{
			_highs[i] = _highs[i + 1];
			_lows[i] = _lows[i + 1];
		}
		_highs[4] = candle.HighPrice;
		_lows[4] = candle.LowPrice;

		decimal? upFractal = null;
		decimal? downFractal = null;

		if (_highs[2] is decimal h2 &&
			_highs[0] is decimal h0 && _highs[1] is decimal h1 &&
			_highs[3] is decimal h3 && _highs[4] is decimal h4 &&
			h2 > h0 && h2 > h1 && h2 > h3 && h2 > h4)
			upFractal = h2;

		if (_lows[2] is decimal l2 &&
			_lows[0] is decimal l0 && _lows[1] is decimal l1 &&
			_lows[3] is decimal l3 && _lows[4] is decimal l4 &&
			l2 < l0 && l2 < l1 && l2 < l3 && l2 < l4)
			downFractal = l2;

		if (upFractal is decimal up && up > teeth)
			_upFractalActivation = up;

		if (_upFractalActivation is decimal actUp && candle.HighPrice > actUp)
		{
			_trend = 1;
			_upFractalActivation = null;
			_downFractalActivation = downFractal;
		}

		if (downFractal is decimal down && down < teeth)
			_downFractalActivation = down;

		if (_downFractalActivation is decimal actDown && candle.LowPrice < actDown)
		{
			_trend = -1;
			_downFractalActivation = null;
			_upFractalActivation = upFractal;
		}

		if (_trend == 1)
			_upFractalActivation = null;
		else if (_trend == -1)
			_downFractalActivation = null;

		if (candle.ClosePrice > teeth && ac > _prevAc && ao > _prevAo && candle.ClosePrice > filterEmaValue)
			_greenBars++;
		else
			_greenBars = 0;

		if (_greenBars == 5)
			_stopLoss = candle.LowPrice;

		if (_entryCount == 0)
			_stopLoss = null;

		if (_entryCount < 5 &&
			candle.ClosePrice > _prevClose &&
			_greenBars >= 2 && _greenBars < 7)
		{
			BuyMarket(Volume);
			_entryCount++;
		}

		if ((_prevTrend == 1 && _trend == -1) || (_stopLoss is decimal sl && candle.LowPrice < sl))
		{
			if (Position > 0)
				SellMarket(Position);
			_entryCount = 0;
		}

		_prevTrend = _trend;
		_prevClose = candle.ClosePrice;
		_prevAo = ao;
		_prevAc = ac;
	}
}
