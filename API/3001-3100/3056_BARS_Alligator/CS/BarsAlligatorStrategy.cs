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
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_cooldownBars = Param(nameof(CooldownBars), 6).SetNotNegative().SetDisplay("Cooldown Bars", "Bars between completed trades", "Trading");
		_stopLossPercent = Param(nameof(StopLossPercent), 3m).SetDisplay("Stop Loss %", "Stop distance as percentage of entry price", "Risk");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m).SetDisplay("Take Profit %", "Take-profit distance as percentage of entry price", "Risk");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

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
		subscription
			.Bind(_jaw, _teeth, _lips, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal jaw, decimal teeth, decimal lips)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownLeft > 0)
			_cooldownLeft--;

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

		// Exit conditions: lips crosses teeth against position
		var closeLong = lips < teeth && _previousLips >= _previousTeeth && Position > 0;
		var closeShort = lips > teeth && _previousLips <= _previousTeeth && Position < 0;

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

		// Entry: lips crosses jaw with proper Alligator ordering
		var buySignal = lips > jaw && _previousLips <= _previousJaw && lips > teeth;
		var sellSignal = lips < jaw && _previousLips >= _previousJaw && lips < teeth;

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
		if (_entryPrice is not decimal entryPrice || Position == 0 || entryPrice == 0)
			return false;

		var stopDistance = entryPrice * StopLossPercent / 100m;
		var takeDistance = entryPrice * TakeProfitPercent / 100m;

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
