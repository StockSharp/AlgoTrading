
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Step Stochastic cross strategy.
/// Opens long when slow line is above 50 and fast line crosses below slow.
/// Opens short when slow line is below 50 and fast line crosses above slow.
/// </summary>
public class StepStochasticCrossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _kFast;
	private readonly StrategyParam<decimal> _kSlow;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	public decimal KFast { get => _kFast.Value; set => _kFast.Value = value; }
	public decimal KSlow { get => _kSlow.Value; set => _kSlow.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool AllowBuyOpen { get => _allowBuyOpen.Value; set => _allowBuyOpen.Value = value; }
	public bool AllowSellOpen { get => _allowSellOpen.Value; set => _allowSellOpen.Value = value; }
	public bool AllowBuyClose { get => _allowBuyClose.Value; set => _allowBuyClose.Value = value; }
	public bool AllowSellClose { get => _allowSellClose.Value; set => _allowSellClose.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isPrevSet;

	public StepStochasticCrossStrategy()
	{
		_kFast = Param(nameof(KFast), 1m)
			.SetDisplay("Fast Multiplier", "Multiplier for fast channel", "Parameters");

		_kSlow = Param(nameof(KSlow), 1m)
			.SetDisplay("Slow Multiplier", "Multiplier for slow channel", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for indicator", "Parameters");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
			.SetDisplay("Allow Buy Open", "Permission to open long positions", "Trading");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
			.SetDisplay("Allow Sell Open", "Permission to open short positions", "Trading");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
			.SetDisplay("Allow Buy Close", "Permission to close long positions", "Trading");

		_allowSellClose = Param(nameof(AllowSellClose), true)
			.SetDisplay("Allow Sell Close", "Permission to close short positions", "Trading");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Protection");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Take profit in price units", "Protection");
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
		_prevFast = default;
		_prevSlow = default;
		_isPrevSet = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stepSto = new StepStochasticIndicator
		{
			KFast = KFast,
			KSlow = KSlow,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stepSto, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Absolute) : default,
			stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Absolute) : default
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = (StepStochasticValue)indicatorValue;

		if (value.Fast is not decimal fast || value.Slow is not decimal slow)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isPrevSet = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isPrevSet = true;
			return;
		}

		if (!_isPrevSet)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isPrevSet = true;
			return;
		}

		if (slow > 50m)
		{
			if (AllowSellClose && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (AllowBuyOpen && _prevFast > _prevSlow && fast <= slow && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (slow < 50m)
		{
			if (AllowBuyClose && Position > 0)
				SellMarket(Position);

			if (AllowSellOpen && _prevFast < _prevSlow && fast >= slow && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}

/// <summary>
/// Step Stochastic indicator producing fast and slow lines.
/// </summary>
public class StepStochasticIndicator : BaseIndicator<decimal>
{
	public decimal KFast { get; set; } = 1m;
	public decimal KSlow { get; set; } = 1m;

	private readonly AverageTrueRange _atr = new() { Length = 10 };

	private bool _initialized;
	private decimal _atrMax, _atrMin;
	private decimal _sMinMin, _sMaxMin, _sMinMax, _sMaxMax, _sMinMid, _sMaxMid;
	private int _trendMin, _trendMax, _trendMid;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new StepStochasticValue(this, input, null, null);

		var atrVal = _atr.Process(input).ToDecimal();

		if (!_atr.IsFormed)
			return new StepStochasticValue(this, input, null, null);

		if (!_initialized)
		{
			_atrMax = atrVal;
			_atrMin = atrVal;

			var close = candle.ClosePrice;

			_sMinMin = close;
			_sMaxMin = close;
			_sMinMax = close;
			_sMaxMax = close;
			_sMinMid = close;
			_sMaxMid = close;

			_trendMax = +1;
			_trendMin = -1;
			_trendMid = +1;

			_initialized = true;

			return new StepStochasticValue(this, input, null, null);
		}

		var atrMax0 = Math.Max(atrVal, _atrMax);
		var atrMin0 = Math.Min(atrVal, _atrMin);

		var stepSizeMin = KFast * atrMin0;
		var stepSizeMax = KFast * atrMax0;
		var stepSizeMid = KFast * 0.5m * KSlow * (atrMax0 + atrMin0);

		var closePrice = candle.ClosePrice;

		var sMaxMin0 = closePrice + 2m * stepSizeMin;
		var sMinMin0 = closePrice - 2m * stepSizeMin;

		var sMaxMax0 = closePrice + 2m * stepSizeMax;
		var sMinMax0 = closePrice - 2m * stepSizeMax;

		var sMaxMid0 = closePrice + 2m * stepSizeMid;
		var sMinMid0 = closePrice - 2m * stepSizeMid;

		var trendMin0 = _trendMin;
		var trendMax0 = _trendMax;
		var trendMid0 = _trendMid;

		if (closePrice > _sMaxMin)
			trendMin0 = +1;
		if (closePrice < _sMinMin)
			trendMin0 = -1;

		if (closePrice > _sMaxMax)
			trendMax0 = +1;
		if (closePrice < _sMinMax)
			trendMax0 = -1;

		if (closePrice > _sMaxMid)
			trendMid0 = +1;
		if (closePrice < _sMinMid)
			trendMid0 = -1;

		if (trendMin0 > 0 && sMinMin0 < _sMinMin)
			sMinMin0 = _sMinMin;
		if (trendMin0 < 0 && sMaxMin0 > _sMaxMin)
			sMaxMin0 = _sMaxMin;

		if (trendMax0 > 0 && sMinMax0 < _sMinMax)
			sMinMax0 = _sMinMax;
		if (trendMax0 < 0 && sMaxMax0 > _sMaxMax)
			sMaxMax0 = _sMaxMax;

		if (trendMid0 > 0 && sMinMid0 < _sMinMid)
			sMinMid0 = _sMinMid;
		if (trendMid0 < 0 && sMaxMid0 > _sMaxMid)
			sMaxMid0 = _sMaxMid;

		var lineMin = trendMin0 > 0 ? sMinMin0 + stepSizeMin : sMaxMin0 - stepSizeMin;
		var lineMax = trendMax0 > 0 ? sMinMax0 + stepSizeMax : sMaxMax0 - stepSizeMax;
		var lineMid = trendMid0 > 0 ? sMinMid0 + stepSizeMid : sMaxMid0 - stepSizeMid;

		var bsMin = lineMax - stepSizeMax;
		var bsMax = lineMax + stepSizeMax;

		var sto1 = (lineMin - bsMin) / (bsMax - bsMin);
		var sto2 = (lineMid - bsMin) / (bsMax - bsMin);

		var fast = sto1 * 100m;
		var slow = sto2 * 100m;

		_atrMax = atrMax0;
		_atrMin = atrMin0;
		_sMinMin = sMinMin0;
		_sMaxMin = sMaxMin0;
		_sMinMax = sMinMax0;
		_sMaxMax = sMaxMax0;
		_sMinMid = sMinMid0;
		_sMaxMid = sMaxMid0;
		_trendMin = trendMin0;
		_trendMax = trendMax0;
		_trendMid = trendMid0;

		return new StepStochasticValue(this, input, fast, slow);
	}
}

/// <summary>
/// Indicator value for <see cref="StepStochasticIndicator"/>.
/// </summary>
public class StepStochasticValue : ComplexIndicatorValue
{
	public StepStochasticValue(IIndicator indicator, IIndicatorValue input, decimal? fast, decimal? slow)
		: base(indicator, input)
	{
		Fast = fast;
		Slow = slow;
	}

	public decimal? Fast { get; }
	public decimal? Slow { get; }
}
