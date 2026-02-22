using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class NyFirstCandleBreakAndRetestStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _rewardRiskRatio;
	private readonly StrategyParam<int> _emaLength;

	private decimal? _firstHigh;
	private decimal? _firstLow;
	private bool _sessionActive;
	private bool _breakAbove;
	private bool _breakBelow;
	private int _barsAfterBreak;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal RewardRiskRatio { get => _rewardRiskRatio.Value; set => _rewardRiskRatio.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	public NyFirstCandleBreakAndRetestStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero();
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.2m).SetGreaterThanZero();
		_rewardRiskRatio = Param(nameof(RewardRiskRatio), 1.5m).SetGreaterThanZero();
		_emaLength = Param(nameof(EmaLength), 13).SetGreaterThanZero();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_firstHigh = null;
		_firstLow = null;
		_sessionActive = false;
		_breakAbove = false;
		_breakBelow = false;
		_barsAfterBreak = 0;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, ema);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atr <= 0)
			return;

		// Use hour-based session detection (simplified)
		var hour = candle.OpenTime.TimeOfDay.TotalHours;
		var inSession = hour >= 9.5 && hour <= 16.0;

		if (!inSession)
		{
			_sessionActive = false;
			_firstHigh = null;
			_firstLow = null;
			_breakAbove = false;
			_breakBelow = false;
			_barsAfterBreak = 0;
			if (Position != 0)
			{
				if (Position > 0) SellMarket(Math.Abs(Position));
				else BuyMarket(Math.Abs(Position));
			}
			return;
		}

		if (!_sessionActive)
		{
			_sessionActive = true;
			_firstHigh = candle.HighPrice;
			_firstLow = candle.LowPrice;
			_breakAbove = false;
			_breakBelow = false;
			_barsAfterBreak = 0;
			return;
		}

		if (_firstHigh == null || _firstLow == null)
			return;

		if (_breakAbove || _breakBelow)
			_barsAfterBreak++;

		if (!_breakAbove && candle.HighPrice >= _firstHigh.Value + atr * 0.15m)
		{
			_breakAbove = true;
			_barsAfterBreak = 0;
		}

		if (!_breakBelow && candle.LowPrice <= _firstLow.Value - atr * 0.15m)
		{
			_breakBelow = true;
			_barsAfterBreak = 0;
		}

		// Manage existing positions first
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
			{
				SellMarket(Math.Abs(Position));
				return;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}

		// Entry logic
		if (_breakAbove && Position <= 0 && _barsAfterBreak >= 2 && _barsAfterBreak <= 25)
		{
			var retest = candle.LowPrice <= _firstHigh.Value + atr * 0.25m;
			if (retest && candle.ClosePrice > ema)
			{
				if (Position < 0) BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice - atr * AtrMultiplier;
				_targetPrice = candle.ClosePrice + (candle.ClosePrice - _stopPrice) * RewardRiskRatio;
			}
		}
		else if (_breakBelow && Position >= 0 && _barsAfterBreak >= 2 && _barsAfterBreak <= 25)
		{
			var retest = candle.HighPrice >= _firstLow.Value - atr * 0.25m;
			if (retest && candle.ClosePrice < ema)
			{
				if (Position > 0) SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice + atr * AtrMultiplier;
				_targetPrice = candle.ClosePrice - (_stopPrice - candle.ClosePrice) * RewardRiskRatio;
			}
		}

		if (_barsAfterBreak > 25)
		{
			_breakAbove = false;
			_breakBelow = false;
		}
	}
}
