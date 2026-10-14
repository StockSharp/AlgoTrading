using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTCUSD Adjustable SLTP strategy.
/// A bullish SMA(FastSmaLength)/SMA(SlowSmaLength) crossover arms a long setup at close * (1 - RetracementPercentage); the long opens
/// when the close crosses back above that level. A bearish crossover cancels the setup, closes a long while the close is below
/// EMA(EmaFilterLength) and in that case also opens a short. Take profit, stop loss and break-even trigger are distances in price
/// steps from the entry close; once the trigger is reached the stop moves to the entry price.
/// </summary>
public class BtcusdAdjustableSltpStrategy : Strategy
{
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _emaFilterLength;
	private readonly StrategyParam<decimal> _takeProfitDistance;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _retracementPercentage;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _prevClose;
	private decimal? _retracementLevel;
	private decimal _entryPrice;
	private bool _breakEven;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastSmaLength
	{
		get => _fastSmaLength.Value;
		set => _fastSmaLength.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowSmaLength
	{
		get => _slowSmaLength.Value;
		set => _slowSmaLength.Value = value;
	}

	/// <summary>
	/// EMA filter period.
	/// </summary>
	public int EmaFilterLength
	{
		get => _emaFilterLength.Value;
		set => _emaFilterLength.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	/// <summary>
	/// Profit in price steps that moves the stop to the entry price.
	/// </summary>
	public decimal BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	/// <summary>
	/// Retracement below the crossover close as a fraction (0.01 = 1%).
	/// </summary>
	public decimal RetracementPercentage
	{
		get => _retracementPercentage.Value;
		set => _retracementPercentage.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BtcusdAdjustableSltpStrategy()
	{
		_fastSmaLength = Param(nameof(FastSmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length", "Fast SMA period", "Indicators");

		_slowSmaLength = Param(nameof(SlowSmaLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Length", "Slow SMA period", "Indicators");

		_emaFilterLength = Param(nameof(EmaFilterLength), 150)
			.SetGreaterThanZero()
			.SetDisplay("EMA Filter Length", "EMA filter period", "Indicators");

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 1000m)
			.SetNotNegative()
			.SetDisplay("Take Profit Distance", "Take profit distance in price steps", "Risk");

		_stopLossDistance = Param(nameof(StopLossDistance), 250m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Distance", "Stop loss distance in price steps", "Risk");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 500m)
			.SetNotNegative()
			.SetDisplay("Break Even Trigger", "Profit in price steps that moves the stop to the entry price", "Risk");

		_retracementPercentage = Param(nameof(RetracementPercentage), 0.01m)
			.SetNotNegative()
			.SetDisplay("Retracement Percentage", "Retracement below the crossover close as a fraction", "Entry");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fast = new SimpleMovingAverage { Length = FastSmaLength };
		var slow = new SimpleMovingAverage { Length = SlowSmaLength };
		var ema = new ExponentialMovingAverage { Length = EmaFilterLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_prevClose = null;
		_retracementLevel = null;
		_entryPrice = 0;
		_breakEven = false;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		var prevClose = _prevClose;

		_prevFast = fast;
		_prevSlow = slow;
		_prevClose = close;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CheckStops(candle))
			return;

		if (prevFast is not decimal pf || prevSlow is not decimal ps || prevClose is not decimal pc)
			return;

		var bullCross = pf <= ps && fast > slow;
		var bearCross = pf >= ps && fast < slow;

		if (bullCross)
		{
			_retracementLevel = close * (1m - RetracementPercentage);
		}
		else if (bearCross)
		{
			_retracementLevel = null;

			if (close < ema)
			{
				if (Position >= 0)
					Enter(false, close);

				return;
			}
		}

		if (_retracementLevel is decimal level && pc <= level && close > level && Position <= 0)
		{
			_retracementLevel = null;
			Enter(true, close);
		}
	}

	private void Enter(bool isLong, decimal price)
	{
		var volume = Volume + Math.Abs(Position);

		if (isLong)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_entryPrice = price;
		_breakEven = false;
	}

	private bool CheckStops(ICandleMessage candle)
	{
		if (Position == 0 || _entryPrice <= 0)
			return false;

		var step = Security?.PriceStep ?? 1m;
		var take = TakeProfitDistance * step;
		var stopDistance = StopLossDistance * step;
		var trigger = BreakEvenTrigger * step;

		if (Position > 0)
		{
			decimal? stop = _breakEven ? _entryPrice : StopLossDistance > 0 ? _entryPrice - stopDistance : null;

			if ((stop is decimal s && candle.LowPrice <= s) || (TakeProfitDistance > 0 && candle.HighPrice >= _entryPrice + take))
			{
				SellMarket(Position);
				_entryPrice = 0;
				return true;
			}

			if (BreakEvenTrigger > 0 && candle.HighPrice - _entryPrice >= trigger)
				_breakEven = true;
		}
		else
		{
			decimal? stop = _breakEven ? _entryPrice : StopLossDistance > 0 ? _entryPrice + stopDistance : null;

			if ((stop is decimal s && candle.HighPrice >= s) || (TakeProfitDistance > 0 && candle.LowPrice <= _entryPrice - take))
			{
				BuyMarket(-Position);
				_entryPrice = 0;
				return true;
			}

			if (BreakEvenTrigger > 0 && _entryPrice - candle.LowPrice >= trigger)
				_breakEven = true;
		}

		return false;
	}
}
