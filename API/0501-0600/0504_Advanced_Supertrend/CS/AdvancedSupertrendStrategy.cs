using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Advanced Supertrend strategy with EMA trend filter and ATR-based stops.
/// </summary>
public class AdvancedSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrStopLength;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private bool _prevUpTrend;
	private bool _hasPrev;
	private decimal _entryPrice;
	private decimal _atrAtEntry;
	private int _cooldownRemaining;

	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int AtrStopLength { get => _atrStopLength.Value; set => _atrStopLength.Value = value; }
	public decimal SlMultiplier { get => _slMultiplier.Value; set => _slMultiplier.Value = value; }
	public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdvancedSupertrendStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for SuperTrend", "SuperTrend");

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetDisplay("Multiplier", "SuperTrend multiplier", "SuperTrend");

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for trend filter", "Filters");

		_atrStopLength = Param(nameof(AtrStopLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Length", "ATR period for stops", "Risk");

		_slMultiplier = Param(nameof(SlMultiplier), 2m)
			.SetDisplay("SL Multiplier", "Stop loss ATR multiplier", "Risk");

		_tpMultiplier = Param(nameof(TpMultiplier), 4m)
			.SetDisplay("TP Multiplier", "Take profit ATR multiplier", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevUpTrend = false;
		_hasPrev = false;
		_entryPrice = 0;
		_atrAtEntry = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var st = new SuperTrend { Length = AtrLength, Multiplier = Multiplier };
		var ema = new ExponentialMovingAverage { Length = MaLength };
		var atr = new AverageTrueRange { Length = AtrStopLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(st, ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stValue.IsEmpty || emaValue.IsEmpty || atrValue.IsEmpty)
			return;

		var stv = (SuperTrendIndicatorValue)stValue;
		var upTrend = stv.IsUpTrend;
		var emaVal = emaValue.ToDecimal();
		var atrVal = atrValue.ToDecimal();

		if (!_hasPrev)
		{
			_prevUpTrend = upTrend;
			_hasPrev = true;
			return;
		}

		// Check stop/TP
		if (Position > 0 && _entryPrice > 0 && _atrAtEntry > 0)
		{
			var sl = _entryPrice - _atrAtEntry * SlMultiplier;
			var tp = _entryPrice + _atrAtEntry * TpMultiplier;
			if (candle.ClosePrice <= sl || candle.ClosePrice >= tp)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0;
				_cooldownRemaining = CooldownBars;
				_prevUpTrend = upTrend;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && _atrAtEntry > 0)
		{
			var sl = _entryPrice + _atrAtEntry * SlMultiplier;
			var tp = _entryPrice - _atrAtEntry * TpMultiplier;
			if (candle.ClosePrice >= sl || candle.ClosePrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0;
				_cooldownRemaining = CooldownBars;
				_prevUpTrend = upTrend;
				return;
			}
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevUpTrend = upTrend;
			return;
		}

		var bullishFlip = upTrend && !_prevUpTrend;
		var bearishFlip = !upTrend && _prevUpTrend;

		if (bullishFlip && candle.ClosePrice > emaVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_atrAtEntry = atrVal;
			_cooldownRemaining = CooldownBars;
		}
		else if (bearishFlip && candle.ClosePrice < emaVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_atrAtEntry = atrVal;
			_cooldownRemaining = CooldownBars;
		}

		_prevUpTrend = upTrend;
	}
}
