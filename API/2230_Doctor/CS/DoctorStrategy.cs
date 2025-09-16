using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Doctor strategy ported from MQL. Combines WMA slope, MA position, RSI and PSAR.
/// </summary>
public class DoctorStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<bool> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _wma40 = new decimal[2];
	private readonly decimal[] _wma400 = new decimal[4];
	private readonly decimal[] _high = new decimal[4];
	private readonly decimal[] _low = new decimal[4];

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Stop-loss distance in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take-profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Enable trailing stop logic.
	/// </summary>
	public bool TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private WeightedMovingAverage _wmaSlope = null!;
	private WeightedMovingAverage _wmaTrend = null!;
	private RelativeStrengthIndex _rsi14 = null!;
	private RelativeStrengthIndex _rsi5 = null!;
	private ParabolicSar _psar = null!;

	/// <summary>
	/// Initialize <see cref="DoctorStrategy"/>.
	/// </summary>
	public DoctorStrategy()
	{
		_stopLossTicks = Param(nameof(StopLossTicks), 70)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop-loss distance in ticks", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 40)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take-profit distance in ticks", "Risk");

		_trailingStop = Param(nameof(TrailingStop), true)
		.SetDisplay("Trailing Stop", "Use trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		Array.Clear(_wma40);
		Array.Clear(_wma400);
		Array.Clear(_high);
		Array.Clear(_low);
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_wmaSlope = new WeightedMovingAverage { Length = 40 };
		_wmaTrend = new WeightedMovingAverage { Length = 400 };
		_rsi14 = new RelativeStrengthIndex { Length = 14 };
		_rsi5 = new RelativeStrengthIndex { Length = 5 };
		_psar = new ParabolicSar();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_wmaSlope, _wmaTrend, _rsi14, _rsi5, _psar, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _wmaSlope);
			DrawIndicator(area, _wmaTrend);
			DrawIndicator(area, _psar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wma40, decimal wma400, decimal rsi14, decimal rsi5, decimal psar)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Shift history arrays
		_wma40[1] = _wma40[0];
		_wma40[0] = wma40;

		for (var i = 3; i > 0; i--)
		{
			_wma400[i] = _wma400[i - 1];
			_high[i] = _high[i - 1];
			_low[i] = _low[i - 1];
		}

		_wma400[0] = wma400;
		_high[0] = candle.HighPrice;
		_low[0] = candle.LowPrice;

		if (_wma40[1] == 0m || _wma400[3] == 0m)
		return; // Not enough data yet

		// Determine slope direction
		var slope = _wma40[0] > _wma40[1] ? 2 : 1;

		// Check long-term MA relative to recent bars
		var maBelow = _wma400[1] < _low[1] && _wma400[2] < _low[2] && _wma400[3] < _low[3];
		var maAbove = _wma400[1] > _high[1] && _wma400[2] > _high[2] && _wma400[3] > _high[3];
		var maLinear = maAbove ? 2 : maBelow ? 1 : 0;

		// RSI relations
		var rsiState = rsi14 < 50m && rsi5 > rsi14 ? 1 : rsi14 > 50m && rsi5 < rsi14 ? 2 : 0;

		// Parabolic SAR position
		var psarState = psar <= candle.LowPrice ? 1 : psar >= candle.HighPrice ? 2 : 0;

		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossTicks * step;
		var takeDistance = TakeProfitTicks * step;

		// Close positions on opposite signals
		if (Position > 0 && slope == 1 && (maLinear == 1 || rsiState == 1 || psarState == 2))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && slope == 2 && (maLinear == 2 || rsiState == 2 || psarState == 1))
		{
			BuyMarket(-Position);
		}

		// Trailing and protective exits
		if (Position > 0)
		{
			if (TrailingStop && candle.ClosePrice - _entryPrice > stopDistance / 2m)
			_stopPrice = Math.Max(_stopPrice, candle.ClosePrice - stopDistance);

			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (TrailingStop && _entryPrice - candle.ClosePrice > stopDistance / 2m)
			_stopPrice = Math.Min(_stopPrice, candle.ClosePrice + stopDistance);

			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			BuyMarket(-Position);
		}

		// Entry conditions
		var volume = Volume + Math.Abs(Position);

		if (slope == 2 && maLinear == 2 && rsiState == 2 && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - stopDistance;
			_takePrice = _entryPrice + takeDistance;
			BuyMarket(volume);
		}
		else if (slope == 1 && maLinear == 1 && rsiState == 1 && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + stopDistance;
			_takePrice = _entryPrice - takeDistance;
			SellMarket(volume);
		}
	}
}
