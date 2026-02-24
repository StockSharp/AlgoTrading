using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ThreeBreaky strategy: combines ATR expansion breakout, Ichimoku cloud flip,
/// and large body exhaustion with Parabolic SAR exit.
/// </summary>
public class ThreeBreakyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _bodyMultiplier;

	private readonly Queue<decimal> _bodyHistory = new();
	private decimal _prevClose;
	private decimal _prevSar;
	private decimal _prePrevSar;
	private decimal _prevSpanA;
	private decimal _prevSpanB;
	private decimal _prevRange;
	private bool _hasPrev;
	private bool _hasPrePrev;
	private int _candleCount;

	/// <summary>
	/// Constructor.
	/// </summary>
	public ThreeBreakyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_atrLength = Param(nameof(AtrLength), 72)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for expansion detection", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Range must exceed ATR * this for System 1", "Signals");

		_bodyMultiplier = Param(nameof(BodyMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Body Multiplier", "Body must exceed max body * this for System 3", "Signals");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public decimal BodyMultiplier
	{
		get => _bodyMultiplier.Value;
		set => _bodyMultiplier.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_hasPrePrev = false;
		_candleCount = 0;
		_bodyHistory.Clear();

		var atr = new AverageTrueRange { Length = AtrLength };
		var sar = new ParabolicSar { Acceleration = 0.02m, AccelerationMax = 0.2m };
		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = 9 },
			Kijun = { Length = 26 },
			SenkouB = { Length = 52 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, sar, ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue sarValue, IIndicatorValue ichValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atrVal = atrValue.ToDecimal();
		var sarVal = sarValue.ToDecimal();

		var ich = (IchimokuValue)ichValue;
		if (ich.SenkouA is not decimal spanA || ich.SenkouB is not decimal spanB)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var isBullish = candle.ClosePrice > candle.OpenPrice;

		// Store body history for System 3
		_bodyHistory.Enqueue(body);
		while (_bodyHistory.Count > 20)
			_bodyHistory.Dequeue();

		_candleCount++;

		if (_hasPrev)
		{
			var longSignal = false;
			var shortSignal = false;

			// System 1: ATR expansion breakout
			if (range > atrVal * AtrMultiplier && atrVal > 0)
			{
				if (isBullish)
					longSignal = true;
				else
					shortSignal = true;
			}

			// System 2: Ichimoku cloud flip
			if (_hasPrePrev)
			{
				var prevAboveCloud = _prevClose > _prevSpanA && _prevClose > _prevSpanB;
				var prevBelowCloud = _prevClose < _prevSpanA && _prevClose < _prevSpanB;
				var nowAboveCloud = candle.ClosePrice > spanA && candle.ClosePrice > spanB;
				var nowBelowCloud = candle.ClosePrice < spanA && candle.ClosePrice < spanB;

				if (!prevAboveCloud && nowAboveCloud)
					longSignal = true;
				if (!prevBelowCloud && nowBelowCloud)
					shortSignal = true;
			}

			// System 3: Large body exhaustion
			var maxBody = GetMaxBody();
			if (maxBody > 0 && body > maxBody * BodyMultiplier)
			{
				if (isBullish)
					longSignal = true;
				else
					shortSignal = true;
			}

			// SAR exit
			if (_hasPrePrev && Position > 0 && _prevClose > _prevSar && candle.ClosePrice < sarVal)
			{
				SellMarket();
			}
			else if (_hasPrePrev && Position < 0 && _prevClose < _prevSar && candle.ClosePrice > sarVal)
			{
				BuyMarket();
			}

			// Entry signals
			if (longSignal && Position <= 0)
			{
				BuyMarket();
			}
			else if (shortSignal && Position >= 0)
			{
				SellMarket();
			}
		}

		_hasPrePrev = _hasPrev;
		_prePrevSar = _prevSar;
		_prevClose = candle.ClosePrice;
		_prevSar = sarVal;
		_prevSpanA = spanA;
		_prevSpanB = spanB;
		_prevRange = range;
		_hasPrev = true;
	}

	private decimal GetMaxBody()
	{
		var max = 0m;
		foreach (var value in _bodyHistory)
		{
			if (value > max)
				max = value;
		}
		return max;
	}
}
