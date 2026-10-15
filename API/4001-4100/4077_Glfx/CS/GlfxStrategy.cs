using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// GLFX strategy: RSI + SMA confirmation for entry.
/// Requires consecutive confirmations before trading.
/// </summary>
public class GlfxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _signalsRepeat;

	private decimal _prevRsi;
	private decimal _prevMa;
	private int _buyCount;
	private int _sellCount;
	private decimal _entryPrice;

	public GlfxStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI period.", "Indicators");

		_rsiUpper = Param(nameof(RsiUpper), 65m)
			.SetDisplay("RSI Upper", "Overbought level.", "Indicators");

		_rsiLower = Param(nameof(RsiLower), 35m)
			.SetDisplay("RSI Lower", "Oversold level.", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 60)
			.SetDisplay("MA Period", "SMA period.", "Indicators");

		_signalsRepeat = Param(nameof(SignalsRepeat), 2)
			.SetDisplay("Signals Repeat", "Consecutive confirmations needed.", "Signals");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int SignalsRepeat
	{
		get => _signalsRepeat.Value;
		set => _signalsRepeat.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = 0;
		_prevMa = 0;
		_buyCount = 0;
		_sellCount = 0;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal maVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevRsi == 0 || _prevMa == 0)
		{
			_prevRsi = rsiVal;
			_prevMa = maVal;
			return;
		}

		var close = candle.ClosePrice;

		// RSI signal: rising and below upper = bullish, falling and above lower = bearish
		var rsiSignal = 0;
		if (rsiVal > _prevRsi && rsiVal < RsiUpper)
			rsiSignal = 1;
		else if (rsiVal < _prevRsi && rsiVal > RsiLower)
			rsiSignal = -1;

		// MA signal: price above rising MA = bullish, below falling MA = bearish
		var maSignal = 0;
		if (maVal > _prevMa && close > maVal)
			maSignal = 1;
		else if (maVal < _prevMa && close < maVal)
			maSignal = -1;

		// Both signals must agree
		if (rsiSignal > 0 && maSignal > 0)
		{
			_buyCount++;
			_sellCount = 0;
		}
		else if (rsiSignal < 0 && maSignal < 0)
		{
			_sellCount++;
			_buyCount = 0;
		}
		else
		{
			_buyCount = 0;
			_sellCount = 0;
		}

		// Exit on opposite signal
		if (Position > 0 && _sellCount >= SignalsRepeat)
		{
			SellMarket();
			_entryPrice = 0;
			_buyCount = 0;
			_sellCount = 0;
		}
		else if (Position < 0 && _buyCount >= SignalsRepeat)
		{
			BuyMarket();
			_entryPrice = 0;
			_buyCount = 0;
			_sellCount = 0;
		}

		// Entry after required confirmations
		if (Position == 0)
		{
			if (_buyCount >= SignalsRepeat)
			{
				_entryPrice = close;
				BuyMarket();
				_buyCount = 0;
				_sellCount = 0;
			}
			else if (_sellCount >= SignalsRepeat)
			{
				_entryPrice = close;
				SellMarket();
				_buyCount = 0;
				_sellCount = 0;
			}
		}

		_prevRsi = rsiVal;
		_prevMa = maVal;
	}
}
