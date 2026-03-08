using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams Alligator strategy with ATR-based stop-loss.
/// Uses three smoothed moving averages (Jaw, Teeth, Lips) for trend detection.
/// </summary>
public class WilliamsAlligatorAtrStrategy : Strategy
{
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private bool _prevLipsAboveJaw;
	private bool _prevLipsBelowJaw;
	private bool _isInitialized;
	private decimal _entryPrice;
	private int _cooldownRemaining;

	public int JawLength { get => _jawLength.Value; set => _jawLength.Value = value; }
	public int TeethLength { get => _teethLength.Value; set => _teethLength.Value = value; }
	public int LipsLength { get => _lipsLength.Value; set => _lipsLength.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public WilliamsAlligatorAtrStrategy()
	{
		_jawLength = Param(nameof(JawLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Length", "Alligator jaw period", "Alligator");

		_teethLength = Param(nameof(TeethLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Length", "Alligator teeth period", "Alligator");

		_lipsLength = Param(nameof(LipsLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Length", "Alligator lips period", "Alligator");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for stop-loss", "ATR");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss", "ATR");

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
		_prevLipsAboveJaw = false;
		_prevLipsBelowJaw = false;
		_isInitialized = false;
		_entryPrice = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var jaw = new SmoothedMovingAverage { Length = JawLength };
		var lips = new SmoothedMovingAverage { Length = LipsLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jaw, lips, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jaw);
			DrawIndicator(area, lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jawVal, decimal lipsVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevLipsAboveJaw = lipsVal > jawVal;
			_prevLipsBelowJaw = lipsVal < jawVal;
			_isInitialized = true;
			return;
		}

		if (!_isInitialized)
		{
			_prevLipsAboveJaw = lipsVal > jawVal;
			_prevLipsBelowJaw = lipsVal < jawVal;
			_isInitialized = true;
			return;
		}

		var lipsAboveJaw = lipsVal > jawVal;
		var lipsBelowJaw = lipsVal < jawVal;

		// Check ATR stop for existing positions
		if (Position > 0 && _entryPrice > 0 && atrVal > 0)
		{
			if (candle.ClosePrice <= _entryPrice - AtrMultiplier * atrVal)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0m;
				_cooldownRemaining = CooldownBars;
				_prevLipsAboveJaw = lipsAboveJaw;
				_prevLipsBelowJaw = lipsBelowJaw;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && atrVal > 0)
		{
			if (candle.ClosePrice >= _entryPrice + AtrMultiplier * atrVal)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
				_cooldownRemaining = CooldownBars;
				_prevLipsAboveJaw = lipsAboveJaw;
				_prevLipsBelowJaw = lipsBelowJaw;
				return;
			}
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevLipsAboveJaw = lipsAboveJaw;
			_prevLipsBelowJaw = lipsBelowJaw;
			return;
		}

		// Long entry: lips crosses above jaw
		if (!_prevLipsAboveJaw && lipsAboveJaw && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}
		// Short entry: lips crosses below jaw
		else if (!_prevLipsBelowJaw && lipsBelowJaw && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}

		_prevLipsAboveJaw = lipsAboveJaw;
		_prevLipsBelowJaw = lipsBelowJaw;
	}
}
