using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ProfessionalOrbStrategy : Strategy
{
	private readonly StrategyParam<int> _orbMinutes;
	private readonly StrategyParam<decimal> _minOrbRange;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<decimal> _profitTargetPoints;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _orbHigh;
	private decimal _orbLow;
	private decimal _orbRange;
	private bool _orbFormed;
	private int _tradesToday;
	private PositionSide _positionSide;
	private decimal _entryPrice;
	private decimal _profitTargetLevel;
	private decimal _breakoutCandleHigh;
	private decimal _breakoutCandleLow;
	private int _prevMinutes;
	private decimal _prevClose;
	private DateTime _currentDate;

	private enum PositionSide
	{
		None,
		Long,
		Short
	}

	public int OrbMinutes
	{
		get => _orbMinutes.Value;
		set => _orbMinutes.Value = value;
	}

	public decimal MinOrbRange
	{
		get => _minOrbRange.Value;
		set => _minOrbRange.Value = value;
	}

	public decimal StopLossAtr
	{
		get => _stopLossAtr.Value;
		set => _stopLossAtr.Value = value;
	}

	public decimal ProfitTargetPoints
	{
		get => _profitTargetPoints.Value;
		set => _profitTargetPoints.Value = value;
	}

	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ProfessionalOrbStrategy()
	{
		_orbMinutes = Param(nameof(OrbMinutes), 15)
			.SetDisplay("ORB Minutes", "Duration of opening range in minutes", "ORB")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_minOrbRange = Param(nameof(MinOrbRange), 40m)
			.SetDisplay("Min ORB Range", "Minimum opening range in points", "ORB")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_stopLossAtr = Param(nameof(StopLossAtr), 1.5m)
			.SetDisplay("Stop Loss ATR", "ATR multiplier for stop loss", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_profitTargetPoints = Param(nameof(ProfitTargetPoints), 50m)
			.SetDisplay("Profit Target", "Profit target in points", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_maxTrades = Param(nameof(MaxTrades), 2)
			.SetDisplay("Max Trades", "Maximum trades per day", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetDaily();
		_prevClose = 0m;
		_prevMinutes = -1;
		_currentDate = DateTime.MinValue;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime;
		var date = time.Date;
		var currentMinutes = time.Hour * 60 + time.Minute;
		var orbStart = 9 * 60 + 15;
		var orbEnd = orbStart + OrbMinutes;

		if (date != _currentDate)
		{
			ResetDaily();
			_currentDate = date;
		}

		var isOrbTime = currentMinutes >= orbStart && currentMinutes < orbEnd;
		var isFirst = isOrbTime && (_prevMinutes < orbStart || _prevMinutes == -1);

		if (isFirst)
		{
			_orbHigh = candle.HighPrice;
			_orbLow = candle.LowPrice;
		}
		else if (isOrbTime)
		{
			if (candle.HighPrice > _orbHigh)
				_orbHigh = candle.HighPrice;
			if (candle.LowPrice < _orbLow)
				_orbLow = candle.LowPrice;
		}

		var orbEnded = currentMinutes >= orbEnd && _prevMinutes < orbEnd;
		if (orbEnded && !_orbFormed)
		{
			_orbFormed = true;
			_orbRange = _orbHigh - _orbLow;
		}

		var validRange = _orbFormed && _orbRange >= MinOrbRange;
		var afterOrb = _orbFormed && currentMinutes > orbEnd;
		var canTrade = validRange && _tradesToday < MaxTrades && _positionSide == PositionSide.None;

		var bullBreak = afterOrb && _prevClose <= _orbHigh && candle.ClosePrice > _orbHigh;
		var bearBreak = afterOrb && _prevClose >= _orbLow && candle.ClosePrice < _orbLow;

		if (canTrade && bullBreak)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_positionSide = PositionSide.Long;
			_tradesToday++;
			_entryPrice = candle.ClosePrice;
			_profitTargetLevel = _entryPrice + ProfitTargetPoints;
			_breakoutCandleHigh = candle.HighPrice;
			_breakoutCandleLow = candle.LowPrice;
		}
		else if (canTrade && bearBreak)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_positionSide = PositionSide.Short;
			_tradesToday++;
			_entryPrice = candle.ClosePrice;
			_profitTargetLevel = _entryPrice - ProfitTargetPoints;
			_breakoutCandleHigh = candle.HighPrice;
			_breakoutCandleLow = candle.LowPrice;
		}

		if (_positionSide == PositionSide.Long && (candle.LowPrice <= _breakoutCandleLow || candle.ClosePrice <= _breakoutCandleLow))
		{
			SellMarket(Position);
			ResetPositionState();
		}
		else if (_positionSide == PositionSide.Short && (candle.HighPrice >= _breakoutCandleHigh || candle.ClosePrice >= _breakoutCandleHigh))
		{
			BuyMarket(-Position);
			ResetPositionState();
		}
		else
		{
			var longStop = _positionSide == PositionSide.Long && candle.ClosePrice < (_orbHigh - atrValue * StopLossAtr);
			var shortStop = _positionSide == PositionSide.Short && candle.ClosePrice > (_orbLow + atrValue * StopLossAtr);
			var longProfit = _positionSide == PositionSide.Long && candle.ClosePrice >= _profitTargetLevel;
			var shortProfit = _positionSide == PositionSide.Short && candle.ClosePrice <= _profitTargetLevel;
			var endDay = time.Hour >= 15 && time.Minute >= 15;

			if (longStop || shortStop || endDay)
			{
				if (Position > 0)
					SellMarket(Position);
				else if (Position < 0)
					BuyMarket(-Position);
				ResetPositionState();
			}
			else if (longProfit || shortProfit)
			{
				if (Position > 0)
					SellMarket(Position);
				else if (Position < 0)
					BuyMarket(-Position);
				ResetPositionState();
			}
		}

		_prevMinutes = currentMinutes;
		_prevClose = candle.ClosePrice;
	}

	private void ResetPositionState()
	{
		_positionSide = PositionSide.None;
		_entryPrice = 0m;
		_profitTargetLevel = 0m;
		_breakoutCandleHigh = 0m;
		_breakoutCandleLow = 0m;
	}

	private void ResetDaily()
	{
		_orbHigh = 0m;
		_orbLow = 0m;
		_orbRange = 0m;
		_orbFormed = false;
		_tradesToday = 0;
		ResetPositionState();
		_prevMinutes = -1;
	}
}
