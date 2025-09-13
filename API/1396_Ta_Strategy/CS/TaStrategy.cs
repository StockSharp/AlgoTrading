using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ta Strategy from TradingView.
/// Uses MACD crossover with pivot support/resistance, RSI and ADX filters.
/// Two profit targets are used with partial exit.
/// </summary>
public class TaStrategy : Strategy
{
	private readonly StrategyParam<int> _leftBars;
	private readonly StrategyParam<int> _rightBars;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _sellQty;
	private readonly StrategyParam<decimal> _longTp1;
	private readonly StrategyParam<decimal> _longTp2;
	private readonly StrategyParam<decimal> _longSl;
	private readonly StrategyParam<decimal> _shortTp1;
	private readonly StrategyParam<decimal> _shortTp2;
	private readonly StrategyParam<decimal> _shortSl;
	private readonly StrategyParam<bool> _inverse;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _target1;
	private decimal _target2;
	private decimal _stop;
	private decimal _tradeVolume;
	private bool _tp1Done;

	public int LeftBars { get => _leftBars.Value; set => _leftBars.Value = value; }
	public int RightBars { get => _rightBars.Value; set => _rightBars.Value = value; }
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public decimal SellQtyPercent { get => _sellQty.Value; set => _sellQty.Value = value; }
	public decimal LongTp1Percent { get => _longTp1.Value; set => _longTp1.Value = value; }
	public decimal LongTp2Percent { get => _longTp2.Value; set => _longTp2.Value = value; }
	public decimal LongSlPercent { get => _longSl.Value; set => _longSl.Value = value; }
	public decimal ShortTp1Percent { get => _shortTp1.Value; set => _shortTp1.Value = value; }
	public decimal ShortTp2Percent { get => _shortTp2.Value; set => _shortTp2.Value = value; }
	public decimal ShortSlPercent { get => _shortSl.Value; set => _shortSl.Value = value; }
	public bool Inverse { get => _inverse.Value; set => _inverse.Value = value; }
	public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TaStrategy()
	{
		_leftBars = Param(nameof(LeftBars), 6)
		.SetGreaterThanZero()
		.SetDisplay("Left Bars", "Bars left for pivot", "Support/Resistance");
		_rightBars = Param(nameof(RightBars), 6)
		.SetGreaterThanZero()
		.SetDisplay("Right Bars", "Bars right for pivot", "Support/Resistance");
		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length", "MACD");
		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length", "MACD");
		_macdSignal = Param(nameof(MacdSignal), 7)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length", "MACD");
		_adxLength = Param(nameof(AdxLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Length", "ADX period", "ADX");
		_sellQty = Param(nameof(SellQtyPercent), 50m)
		.SetDisplay("Sell Qty %", "Percent to exit at TP1", "Exits");
		_longTp1 = Param(nameof(LongTp1Percent), 6m)
		.SetDisplay("Long TP1 %", "First long target percent", "Exits");
		_longTp2 = Param(nameof(LongTp2Percent), 12m)
		.SetDisplay("Long TP2 %", "Second long target percent", "Exits");
		_longSl = Param(nameof(LongSlPercent), 3m)
		.SetDisplay("Long SL %", "Long stop percent", "Exits");
		_shortTp1 = Param(nameof(ShortTp1Percent), 6m)
		.SetDisplay("Short TP1 %", "First short target percent", "Exits");
		_shortTp2 = Param(nameof(ShortTp2Percent), 12m)
		.SetDisplay("Short TP2 %", "Second short target percent", "Exits");
		_shortSl = Param(nameof(ShortSlPercent), 3m)
		.SetDisplay("Short SL %", "Short stop percent", "Exits");
		_inverse = Param(nameof(Inverse), false)
		.SetDisplay("Inverse", "Inverse signals", "General");
		_direction = Param(nameof(Direction), (Sides?)null)
			.SetDisplay("Direction", "Allowed direction", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Working timeframe", "General");
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
		_entryPrice = 0m;
		_target1 = 0m;
		_target2 = 0m;
		_stop = 0m;
		_tradeVolume = 0m;
		_tp1Done = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = MacdSignal }
		};
		var dmi = new DirectionalIndex { Length = AdxLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var highest = new Highest { Length = LeftBars };
		var lowest = new Lowest { Length = LeftBars };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(macd, dmi, adx, rsi, highest, lowest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, rsi);
			DrawIndicator(area, adx);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle,
	IIndicatorValue macdValue,
	IIndicatorValue dmiValue,
	IIndicatorValue adxValue,
	IIndicatorValue rsiValue,
	IIndicatorValue highValue,
	IIndicatorValue lowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macd ||
		dmiValue is not DirectionalIndexValue dmi ||
		adxValue is not AverageDirectionalIndexValue adxData ||
		!rsiValue.IsFinal ||
		!highValue.IsFinal ||
		!lowValue.IsFinal)
		return;

		if (macd.Macd is not decimal macdLine ||
		macd.Signal is not decimal signalLine ||
		dmi.Plus is not decimal plus ||
		dmi.Minus is not decimal minus ||
		adxData.MovingAverage is not decimal adx ||
		rsiValue.GetValue<decimal>() is not decimal rsi ||
		highValue.GetValue<decimal>() is not decimal res ||
		lowValue.GetValue<decimal>() is not decimal sup)
		return;

		var longCond = macdLine > signalLine && candle.ClosePrice > res && rsi > 50m && plus > minus && adx > 20m;
		var shortCond = macdLine < signalLine && candle.ClosePrice < sup && rsi < 50m && plus < minus && adx > 20m;

		if (Inverse)
		{
			(longCond, shortCond) = (shortCond, longCond);
		}

		var allowLong = Direction != Sides.Sell;
		var allowShort = Direction != Sides.Buy;

		if (longCond && allowLong && Position <= 0)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (shortCond && allowShort && Position >= 0)
		{
			EnterShort(candle.ClosePrice);
		}

		ManagePosition(candle);
	}

	private void EnterLong(decimal price)
	{
		var vol = Volume + Math.Abs(Position);
		BuyMarket(vol);
		_entryPrice = price;
		_stop = price * (1 - LongSlPercent / 100m);
		_target1 = price * (1 + LongTp1Percent / 100m);
		_target2 = price * (1 + LongTp2Percent / 100m);
		_tradeVolume = vol;
		_tp1Done = false;
	}

	private void EnterShort(decimal price)
	{
		var vol = Volume + Math.Abs(Position);
		SellMarket(vol);
		_entryPrice = price;
		_stop = price * (1 + ShortSlPercent / 100m);
		_target1 = price * (1 - ShortTp1Percent / 100m);
		_target2 = price * (1 - ShortTp2Percent / 100m);
		_tradeVolume = vol;
		_tp1Done = false;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (candle.LowPrice <= _stop)
			{
				SellMarket(Math.Abs(Position));
				ResetTrade();
			}
			else
			{
				if (!_tp1Done && candle.HighPrice >= _target1)
				{
					var qty = _tradeVolume * SellQtyPercent / 100m;
					SellMarket(qty);
					_tp1Done = true;
					_stop = _entryPrice;
				}
				if (candle.HighPrice >= _target2)
				{
					SellMarket(Math.Abs(Position));
					ResetTrade();
				}
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetTrade();
			}
			else
			{
				if (!_tp1Done && candle.LowPrice <= _target1)
				{
					var qty = _tradeVolume * SellQtyPercent / 100m;
					BuyMarket(qty);
					_tp1Done = true;
					_stop = _entryPrice;
				}
				if (candle.LowPrice <= _target2)
				{
					BuyMarket(Math.Abs(Position));
					ResetTrade();
				}
			}
		}
	}

	private void ResetTrade()
	{
		_entryPrice = 0m;
		_target1 = 0m;
		_target2 = 0m;
		_stop = 0m;
		_tradeVolume = 0m;
		_tp1Done = false;
	}
}