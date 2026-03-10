namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bill Williams Alligator strategy: trades on lips/jaw crossover and exits on lips/teeth crossover.
/// </summary>
public class BarsAlligatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;

	private readonly SmoothedMovingAverage _jaw = new() { Length = 13 };
	private readonly SmoothedMovingAverage _teeth = new() { Length = 8 };
	private readonly SmoothedMovingAverage _lips = new() { Length = 5 };

	private decimal _previousJaw;
	private decimal _previousTeeth;
	private decimal _previousLips;
	private bool _hasPrevious;
	private decimal? _entryPrice;
	private int _cooldownLeft;

	public BarsAlligatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_cooldownBars = Param(nameof(CooldownBars), 6).SetNotNegative().SetDisplay("Cooldown Bars", "Bars between completed trades", "Trading");
		_stopLossPips = Param(nameof(StopLossPips), 150).SetNotNegative().SetDisplay("Stop Loss", "Stop distance in pips", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 150).SetNotNegative().SetDisplay("Take Profit", "Take-profit distance in pips", "Risk");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousJaw = 0m;
		_previousTeeth = 0m;
		_previousLips = 0m;
		_hasPrevious = false;
		_entryPrice = null;
		_cooldownLeft = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		OnReseted();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownLeft > 0)
			_cooldownLeft--;

		var price = (candle.HighPrice + candle.LowPrice) / 2m;
		var jawValue = _jaw.Process(new DecimalIndicatorValue(_jaw, price, candle.OpenTime) { IsFinal = true });
		var teethValue = _teeth.Process(new DecimalIndicatorValue(_teeth, price, candle.OpenTime) { IsFinal = true });
		var lipsValue = _lips.Process(new DecimalIndicatorValue(_lips, price, candle.OpenTime) { IsFinal = true });

		if (!_jaw.IsFormed || !_teeth.IsFormed || !_lips.IsFormed || jawValue.IsEmpty || teethValue.IsEmpty || lipsValue.IsEmpty)
			return;

		var jaw = jawValue.ToDecimal();
		var teeth = teethValue.ToDecimal();
		var lips = lipsValue.ToDecimal();

		if (Position != 0 && _entryPrice is null)
			_entryPrice = candle.ClosePrice;

		if (TryExitByRisk(candle))
		{
			UpdatePrevious(jaw, teeth, lips);
			return;
		}

		if (!_hasPrevious)
		{
			UpdatePrevious(jaw, teeth, lips);
			return;
		}

		var closeLong = lips < teeth && _previousLips >= _previousTeeth && Position > 0 && _entryPrice is decimal longEntry && candle.ClosePrice >= longEntry;
		var closeShort = lips > teeth && _previousLips <= _previousTeeth && Position < 0 && _entryPrice is decimal shortEntry && candle.ClosePrice <= shortEntry;

		if (closeLong)
		{
			SellMarket(Position);
			_entryPrice = null;
			_cooldownLeft = CooldownBars;
			UpdatePrevious(jaw, teeth, lips);
			return;
		}

		if (closeShort)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = null;
			_cooldownLeft = CooldownBars;
			UpdatePrevious(jaw, teeth, lips);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading() || _cooldownLeft > 0)
		{
			UpdatePrevious(jaw, teeth, lips);
			return;
		}

		var buySignal = lips > jaw && _previousLips <= _previousJaw && lips > teeth && teeth > jaw;
		var sellSignal = lips < jaw && _previousLips >= _previousJaw && lips < teeth && teeth < jaw;

		if (buySignal && Position <= 0)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
				_cooldownLeft = CooldownBars;
			}
			else
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_cooldownLeft = CooldownBars;
			}
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				_entryPrice = null;
				_cooldownLeft = CooldownBars;
			}
			else
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_cooldownLeft = CooldownBars;
			}
		}

		UpdatePrevious(jaw, teeth, lips);
	}

	private bool TryExitByRisk(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entryPrice || Position == 0)
			return false;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0)
			step = 1m;

		var stopDistance = StopLossPips * step;
		var takeDistance = TakeProfitPips * step;

		if (Position > 0)
		{
			if ((stopDistance > 0 && candle.LowPrice <= entryPrice - stopDistance) ||
				(takeDistance > 0 && candle.HighPrice >= entryPrice + takeDistance))
			{
				SellMarket(Position);
				_entryPrice = null;
				_cooldownLeft = CooldownBars;
				return true;
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);

			if ((stopDistance > 0 && candle.HighPrice >= entryPrice + stopDistance) ||
				(takeDistance > 0 && candle.LowPrice <= entryPrice - takeDistance))
			{
				BuyMarket(volume);
				_entryPrice = null;
				_cooldownLeft = CooldownBars;
				return true;
			}
		}

		return false;
	}

	private void UpdatePrevious(decimal jaw, decimal teeth, decimal lips)
	{
		_previousJaw = jaw;
		_previousTeeth = teeth;
		_previousLips = lips;
		_hasPrevious = true;
	}
}
