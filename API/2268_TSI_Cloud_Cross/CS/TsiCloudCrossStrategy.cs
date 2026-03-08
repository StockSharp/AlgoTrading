using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// True Strength Index cross with shifted TSI line.
/// Opens long when TSI crosses above its shifted value and short on opposite cross.
/// </summary>
public class TsiCloudCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _firstLength;
	private readonly StrategyParam<int> _secondLength;
	private readonly StrategyParam<int> _triggerShift;

	private TrueStrengthIndex _tsi;
	private readonly Queue<decimal> _tsiValues = new();
	private decimal _prevTsi;
	private decimal _prevTrigger;
	private bool _isInitialized;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FirstLength { get => _firstLength.Value; set => _firstLength.Value = value; }
	public int SecondLength { get => _secondLength.Value; set => _secondLength.Value = value; }
	public int TriggerShift { get => _triggerShift.Value; set => _triggerShift.Value = value; }

	public TsiCloudCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_firstLength = Param(nameof(FirstLength), 25)
			.SetDisplay("First Length", "First smoothing period for TSI", "TSI")
			.SetGreaterThanZero();
		_secondLength = Param(nameof(SecondLength), 13)
			.SetDisplay("Second Length", "Second smoothing period for TSI", "TSI")
			.SetGreaterThanZero();
		_triggerShift = Param(nameof(TriggerShift), 1)
			.SetDisplay("Trigger Shift", "Bars to shift TSI for trigger", "TSI")
			.SetGreaterThanZero();
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
		_tsi = null;
		_tsiValues.Clear();
		_prevTsi = 0;
		_prevTrigger = 0;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_tsiValues.Clear();
		_isInitialized = false;

		_tsi = new TrueStrengthIndex
		{
			FirstLength = FirstLength,
			SecondLength = SecondLength,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_tsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (value is not ITrueStrengthIndexValue tsiVal || tsiVal.Tsi is not decimal tsiValue)
			return;

		if (!_tsi.IsFormed)
			return;

		_tsiValues.Enqueue(tsiValue);
		if (_tsiValues.Count > TriggerShift + 1)
			_tsiValues.Dequeue();

		if (_tsiValues.Count < TriggerShift + 1)
		{
			_prevTsi = tsiValue;
			_prevTrigger = tsiValue;
			return;
		}

		var trigger = _tsiValues.Peek();

		if (!_isInitialized)
		{
			_prevTsi = tsiValue;
			_prevTrigger = trigger;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevTsi <= _prevTrigger && tsiValue > trigger;
		var crossDown = _prevTsi >= _prevTrigger && tsiValue < trigger;

		_prevTsi = tsiValue;
		_prevTrigger = trigger;

		if (crossUp && Position <= 0)
			BuyMarket();
		else if (crossDown && Position >= 0)
			SellMarket();
	}
}
