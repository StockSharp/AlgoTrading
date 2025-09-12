namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// J-Lines Ribbon 4-Cycle Engine strategy.
/// Classifies market state into CHOP, LONG, and SHORT cycles using EMA ribbon and ADX.
/// Executes entries on cycle changes and EMA rebounds.
/// </summary>
public class JLinesRibbon4CycleEngineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<decimal> _adxFloor;

	private ExponentialMovingAverage _ema72;
	private ExponentialMovingAverage _ema89;
	private ExponentialMovingAverage _ema126;
	private ExponentialMovingAverage _ema267;
	private ExponentialMovingAverage _ema360;
	private ExponentialMovingAverage _ema445;
	private AverageDirectionalIndex _adx;

	private enum CycleState { Chop, Long, Short }
	private CycleState _cycle = CycleState.Chop;
	private bool _newCycle;

	private decimal _prevEma72;
	private decimal _prevEma89;
	private decimal _prevEma126;
	private decimal _prevClose;

	private bool _dipped72;
	private bool _dipped126;
	private bool _topped72;
	private bool _topped126;

	private decimal _lastHigh;
	private decimal _lastLow;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// DMI length.
	/// </summary>
	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	/// <summary>
	/// ADX floor to detect chop.
	/// </summary>
	public decimal AdxFloor
	{
		get => _adxFloor.Value;
		set => _adxFloor.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="JLinesRibbon4CycleEngineStrategy"/>.
	/// </summary>
	public JLinesRibbon4CycleEngineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_dmiLength = Param(nameof(DmiLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("DI Length", "Directional Index length", "Indicators");

		_adxFloor = Param(nameof(AdxFloor), 12m)
			.SetDisplay("ADX Floor", "Chop threshold", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema72 = new() { Length = 72 };
		_ema89 = new() { Length = 89 };
		_ema126 = new() { Length = 126 };
		_ema267 = new() { Length = 267 };
		_ema360 = new() { Length = 360 };
		_ema445 = new() { Length = 445 };
		_adx = new AverageDirectionalIndex { Length = DmiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema72, _ema89, _ema126, _ema267, _ema360, _ema445, _adx, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal ema72, decimal ema89, decimal ema126, decimal ema267,
		decimal ema360, decimal ema445, decimal adx)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema72.IsFormed || !_ema89.IsFormed || !_ema126.IsFormed || !_adx.IsFormed)
			return;

		var inChop = adx <= AdxFloor;
		var trendLong = !inChop && ema72 > ema89;
		var trendShort = !inChop && ema72 < ema89;

		if (inChop && _cycle != CycleState.Chop)
		{
			_cycle = CycleState.Chop;
			_newCycle = true;
		}
		else if (trendLong && _cycle != CycleState.Long)
		{
			_cycle = CycleState.Long;
			_newCycle = true;
		}
		else if (trendShort && _cycle != CycleState.Short)
		{
			_cycle = CycleState.Short;
			_newCycle = true;
		}
		else
		{
			_newCycle = false;
		}

		if (_newCycle && _cycle == CycleState.Chop && Position != 0)
			ClosePosition();

		if (_newCycle)
		{
			if (_cycle == CycleState.Long && Position <= 0)
				BuyMarket();
			else if (_cycle == CycleState.Short && Position >= 0)
				SellMarket();
		}

		var flipLongEvt = _prevEma72 <= _prevEma89 && ema72 > ema89;
		var flipShortEvt = _prevEma72 >= _prevEma89 && ema72 < ema89;
		var xAbove72 = _prevClose <= _prevEma72 && candle.ClosePrice > ema72;
		var xBelow72 = _prevClose >= _prevEma72 && candle.ClosePrice < ema72;
		var xAbove126 = _prevClose <= _prevEma126 && candle.ClosePrice > ema126;
		var xBelow126 = _prevClose >= _prevEma126 && candle.ClosePrice < ema126;

		if (_cycle == CycleState.Long)
		{
			if (flipLongEvt)
				BuyMarket();

			_dipped72 = candle.ClosePrice < ema72 || (_dipped72 && candle.ClosePrice < ema72);
			if (_dipped72 && xAbove72)
			{
				BuyMarket();
				_dipped72 = false;
			}

			_dipped126 = (candle.ClosePrice < ema126 && candle.ClosePrice >= ema267) || (_dipped126 && candle.ClosePrice < ema126);
			if (_dipped126 && xAbove126)
			{
				BuyMarket();
				_dipped126 = false;
			}

			if (Position == 0 && ema72 > ema89)
				BuyMarket();

			if (xBelow72 && Position > 0)
				ClosePosition();

			if (Position > 0 && candle.LowPrice <= _lastLow)
				ClosePosition();
		}
		else if (_cycle == CycleState.Short)
		{
			if (Position == 0 && ema72 < ema89)
				SellMarket();

			if (flipShortEvt)
				SellMarket();

			_topped72 = candle.ClosePrice > ema72 || (_topped72 && candle.ClosePrice > ema72);
			if (_topped72 && xBelow72)
			{
				SellMarket();
				_topped72 = false;
			}

			_topped126 = (candle.ClosePrice > ema126 && candle.ClosePrice <= ema267) || (_topped126 && candle.ClosePrice > ema126);
			if (_topped126 && xBelow126)
			{
				SellMarket();
				_topped126 = false;
			}

			if (xAbove72 && Position < 0)
				ClosePosition();

			if (Position < 0 && candle.HighPrice >= _lastHigh)
				ClosePosition();
		}

		_lastHigh = candle.HighPrice;
		_lastLow = candle.LowPrice;
		_prevEma72 = ema72;
		_prevEma89 = ema89;
		_prevEma126 = ema126;
		_prevClose = candle.ClosePrice;
	}
}
