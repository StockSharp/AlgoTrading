using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-layer Acceleration Deceleration strategy.
/// Builds up to five long entries on rising AC momentum above the Alligator teeth.
/// </summary>
public class MultiLayerAccelerationDecelerationStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _tradeStart;
	private readonly StrategyParam<DateTimeOffset> _tradeStop;

	private readonly SmoothedMovingAverage _teeth = new() { Length = 8 };
	private readonly SimpleMovingAverage _aoFast = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlow = new() { Length = 34 };
	private readonly SimpleMovingAverage _acMa = new() { Length = 5 };

	private readonly decimal?[] _teethBuffer = new decimal?[6];
	private readonly decimal?[] _highs = new decimal?[5];
	private readonly decimal?[] _lows = new decimal?[5];
	private readonly decimal[] _acBuffer = new decimal[5];

	private int _bufferCount;
	private int _acCount;
	private int _trend;
	private int _prevTrend;
	private int _signalsQty;
	private int _entryCount;
	private bool _prevAcSignal;
	private decimal _prevHigh;
	private decimal? _upFractalLevel;
	private decimal? _downFractalLevel;
	private decimal? _acActivation;

	/// <summary>
	/// EMA period for filter.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Candle series type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Start of trading period.
	/// </summary>
	public DateTimeOffset TradeStart { get => _tradeStart.Value; set => _tradeStart.Value = value; }

	/// <summary>
	/// End of trading period.
	/// </summary>
	public DateTimeOffset TradeStop { get => _tradeStop.Value; set => _tradeStop.Value = value; }

	/// <summary>
	/// Initializes <see cref="MultiLayerAccelerationDecelerationStrategy"/>.
	/// </summary>
	public MultiLayerAccelerationDecelerationStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 100)
			.SetDisplay("EMA Length", "EMA period", "Settings")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_tradeStart = Param(nameof(TradeStart), new DateTimeOffset(new DateTime(2023, 1, 1), TimeSpan.Zero))
			.SetDisplay("Trade Start", "Start date", "Period");

		_tradeStop = Param(nameof(TradeStop), new DateTimeOffset(new DateTime(2025, 1, 1), TimeSpan.Zero))
			.SetDisplay("Trade Stop", "End date", "Period");
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
		Array.Clear(_acBuffer);
		_bufferCount = 0;
		_acCount = 0;
		_trend = 0;
		_prevTrend = 0;
		_signalsQty = 0;
		_entryCount = 0;
		_prevAcSignal = false;
		_prevHigh = 0m;
		_upFractalLevel = null;
		_downFractalLevel = null;
		_acActivation = null;
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
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal filterEmaValue)
	{
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

		for (var i = 4; i > 0; i--)
			_acBuffer[i] = _acBuffer[i - 1];
		_acBuffer[0] = ac;
		if (_acCount < 5)
			_acCount++;

		for (var i = 0; i < 4; i++)
		{
			_highs[i] = _highs[i + 1];
			_lows[i] = _lows[i + 1];
		}
		_highs[4] = candle.HighPrice;
		_lows[4] = candle.LowPrice;

		if (candle.State != CandleStates.Finished)
		{
			_prevHigh = candle.HighPrice;
			return;
		}

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
			_upFractalLevel = up;
		if (downFractal is decimal down && down < teeth)
			_downFractalLevel = down;

		if (_upFractalLevel is decimal uf && candle.HighPrice > uf)
			_trend = 1;
		if (_downFractalLevel is decimal df && candle.LowPrice < df)
			_trend = -1;

		var ac1 = _acCount > 1 ? _acBuffer[1] : 0m;
		var ac2 = _acCount > 2 ? _acBuffer[2] : 0m;
		var ac3 = _acCount > 3 ? _acBuffer[3] : 0m;
		var ac4 = _acCount > 4 ? _acBuffer[4] : 0m;
		var diff = ac - ac1;

		var signal1 = ac > 0 && ac > ac1 && ac1 > ac2 && ac2 < ac3;
		var signal2 = ac < 0 && ac > ac1 && ac1 > ac2 && ac2 > ac3 && ac3 < ac4;
		var acSignal = (signal1 || signal2) && _trend == 1 && candle.ClosePrice > filterEmaValue;

		var prevActivation = _acActivation;
		if (acSignal)
			_acActivation = candle.HighPrice;

		if ((prevActivation is decimal pa && candle.HighPrice > pa) || diff < 0)
			_acActivation = null;

		if (acSignal && !_prevAcSignal)
			_signalsQty++;

		if (diff < 0)
		{
			CancelActiveOrders();
			if (_signalsQty > 0 && _acActivation is null)
				_signalsQty--;
		}

		if (_prevTrend == 1 && _trend == -1)
		{
			CancelActiveOrders();
			if (Position > 0)
				SellMarket(Position);
			_signalsQty = 0;
			_entryCount = 0;
		}

		if (_acActivation is decimal act &&
			_signalsQty > _entryCount && _entryCount < 5 &&
			IsFormedAndOnlineAndAllowTrading() &&
			candle.ServerTime >= TradeStart && candle.ServerTime <= TradeStop)
		{
			while (_entryCount < Math.Min(_signalsQty, 5))
			{
				BuyStop(Volume, act);
				_entryCount++;
			}
		}

		_prevAcSignal = acSignal;
		_prevTrend = _trend;
		_prevHigh = candle.HighPrice;
	}
}
