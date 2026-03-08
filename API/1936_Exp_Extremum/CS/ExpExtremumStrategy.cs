using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Extremum reversal strategy using highest and lowest comparisons.
/// </summary>
public class ExpExtremumStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private readonly Lowest _minHigh = new();
	private readonly Highest _maxLow = new();
	private bool _upPrev1;
	private bool _dnPrev1;
	private bool _upPrev2;
	private bool _dnPrev2;
	private int _barsSinceTrade;

	/// <summary>
	/// Indicator period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpExtremumStrategy"/> class.
	/// </summary>
	public ExpExtremumStrategy()
	{
		_length = Param(nameof(Length), 40)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Indicator period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame of the Extremum indicator", "General");

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Signals");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Entry", "Permission to buy", "Signals");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Entry", "Permission to sell", "Signals");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Long", "Permission to exit long positions", "Signals");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Short", "Permission to exit short positions", "Signals");
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

		_minHigh.Length = Length;
		_maxLow.Length = Length;
		_minHigh.Reset();
		_maxLow.Reset();
		_upPrev1 = false;
		_dnPrev1 = false;
		_upPrev2 = false;
		_dnPrev2 = false;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_minHigh.Length = Length;
		_maxLow.Length = Length;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
			DrawCandles(area, subscription);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var minHighValue = _minHigh.Process(new DecimalIndicatorValue(_minHigh, candle.HighPrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var maxLowValue = _maxLow.Process(new DecimalIndicatorValue(_maxLow, candle.LowPrice, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_minHigh.IsFormed || !_maxLow.IsFormed)
			return;

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		var pressure = (candle.HighPrice - minHighValue) + (candle.LowPrice - maxLowValue);
		var up = pressure > 0m;
		var dn = pressure < 0m;
		var bullishReversal = _dnPrev2 && _upPrev1 && up && candle.ClosePrice > candle.OpenPrice;
		var bearishReversal = _upPrev2 && _dnPrev1 && dn && candle.ClosePrice < candle.OpenPrice;

		if (BuyPosClose && bearishReversal && Position > 0)
		{
			SellMarket(Position);
			_barsSinceTrade = 0;
		}

		if (SellPosClose && bullishReversal && Position < 0)
		{
			BuyMarket(-Position);
			_barsSinceTrade = 0;
		}

		if (_barsSinceTrade >= CooldownBars)
		{
			if (BuyPosOpen && bullishReversal && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}

			if (SellPosOpen && bearishReversal && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
		}

		_upPrev2 = _upPrev1;
		_dnPrev2 = _dnPrev1;
		_upPrev1 = up;
		_dnPrev1 = dn;
	}
}
