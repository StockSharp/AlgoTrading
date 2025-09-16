using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractal breakout strategy that averages recent fractal levels and filters entries with ExVol momentum.
/// </summary>
public class ExFractalsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _exPeriod;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private readonly Queue<decimal> _bodyQueue = new();

	private decimal _bodySum;

	private decimal _h1;
	private decimal _h2;
	private decimal _h3;
	private decimal _h4;
	private decimal _h5;

	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal _l4;
	private decimal _l5;

	private DateTimeOffset _t1;
	private DateTimeOffset _t2;
	private DateTimeOffset _t3;
	private DateTimeOffset _t4;
	private DateTimeOffset _t5;

	private decimal? _upFractal1;
	private decimal? _upFractal2;
	private DateTimeOffset? _upTime1;
	private DateTimeOffset? _upTime2;

	private decimal? _downFractal1;
	private decimal? _downFractal2;
	private DateTimeOffset? _downTime1;
	private DateTimeOffset? _downTime2;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period for the ExVol average body indicator.
	/// </summary>
	public int ExPeriod
	{
		get => _exPeriod.Value;
		set => _exPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum price improvement before the trailing stop is moved.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ExFractalsStrategy"/>.
	/// </summary>
	public ExFractalsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_exPeriod = Param(nameof(ExPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("ExVol Period", "Average body lookback", "Indicators");

		_stopLossPips = Param(nameof(StopLossPips), 40m)
			.SetDisplay("Stop Loss", "Stop-loss in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetDisplay("Take Profit", "Take-profit in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
			.SetDisplay("Trailing Stop", "Trailing distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step", "Extra movement before trailing", "Risk");

		Volume = 1m;
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

		_bodyQueue.Clear();
		_bodySum = 0m;

		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;

		_t1 = _t2 = _t3 = _t4 = _t5 = default;

		_upFractal1 = _upFractal2 = null;
		_downFractal1 = _downFractal2 = null;
		_upTime1 = _upTime2 = null;
		_downTime1 = _downTime2 = null;

		_longEntryPrice = _shortEntryPrice = null;
		_longStop = _longTake = null;
		_shortStop = _shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		{
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Shift candle history buffers so the third slot represents the confirmed fractal bar.
		_h1 = _h2;
		_h2 = _h3;
		_h3 = _h4;
		_h4 = _h5;
		_h5 = candle.HighPrice;

		_l1 = _l2;
		_l2 = _l3;
		_l3 = _l4;
		_l4 = _l5;
		_l5 = candle.LowPrice;

		_t1 = _t2;
		_t2 = _t3;
		_t3 = _t4;
		_t4 = _t5;
		_t5 = candle.OpenTime;

		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		// Detect new fractal values when enough candles are collected.
		if (_t3 != default)
		{
			if (_h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
			{
				RegisterUpFractal(_h3, _t3);
			}

			if (_l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
			{
				RegisterDownFractal(_l3, _t3);
			}
		}

		var step = Security?.PriceStep ?? 0.0001m;
		if (step <= 0m)
		{
			step = 0.0001m;
		}

		var body = (candle.ClosePrice - candle.OpenPrice) / step;
		_bodyQueue.Enqueue(body);
		_bodySum += body;
		if (_bodyQueue.Count > ExPeriod)
		{
			_bodySum -= _bodyQueue.Dequeue();
		}

		decimal? exVol = _bodyQueue.Count >= ExPeriod ? _bodySum / ExPeriod : null;
		var upperLevel = GetUpperLevel();
		var lowerLevel = GetLowerLevel();
		var price = candle.ClosePrice;

		if (exVol is decimal exVolValue && upperLevel is decimal up && price > up && exVolValue < 0m && Position <= 0)
		{
			var volume = Volume + Math.Max(0m, -Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				InitializeLongTargets(price, step);
			}
		}
		else if (exVol is decimal exVolValue2 && lowerLevel is decimal down && price < down && exVolValue2 > 0m && Position >= 0)
		{
			var volume = Volume + Math.Max(0m, Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				InitializeShortTargets(price, step);
			}
		}

		ManagePosition(price, step);
	}

	private void RegisterUpFractal(decimal price, DateTimeOffset time)
	{
		if (_upFractal1 is null)
		{
			_upFractal1 = price;
			_upTime1 = time;
			return;
		}

		if (_upTime1 == time)
		{
			_upFractal1 = price;
			return;
		}

		if (_upFractal2 is null)
		{
			_upFractal2 = price;
			_upTime2 = time;
			return;
		}

		if (_upTime2 == time)
		{
			_upFractal2 = price;
			return;
		}

		_upFractal1 = _upFractal2;
		_upTime1 = _upTime2;
		_upFractal2 = price;
		_upTime2 = time;
	}

	private void RegisterDownFractal(decimal price, DateTimeOffset time)
	{
		if (_downFractal1 is null)
		{
			_downFractal1 = price;
			_downTime1 = time;
			return;
		}

		if (_downTime1 == time)
		{
			_downFractal1 = price;
			return;
		}

		if (_downFractal2 is null)
		{
			_downFractal2 = price;
			_downTime2 = time;
			return;
		}

		if (_downTime2 == time)
		{
			_downFractal2 = price;
			return;
		}

		_downFractal1 = _downFractal2;
		_downTime1 = _downTime2;
		_downFractal2 = price;
		_downTime2 = time;
	}

	private decimal? GetUpperLevel()
	{
		if (_upFractal1 is decimal first && _upFractal2 is decimal second && _upTime1 != _upTime2)
		{
			return (first + second) / 2m;
		}

		return null;
	}

	private decimal? GetLowerLevel()
	{
		if (_downFractal1 is decimal first && _downFractal2 is decimal second && _downTime1 != _downTime2)
		{
			return (first + second) / 2m;
		}

		return null;
	}

	private void InitializeLongTargets(decimal price, decimal step)
	{
		_longEntryPrice = price;
		_longStop = StopLossPips > 0m ? price - StopLossPips * step : null;
		_longTake = TakeProfitPips > 0m ? price + TakeProfitPips * step : null;

		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
	}

	private void InitializeShortTargets(decimal price, decimal step)
	{
		_shortEntryPrice = price;
		_shortStop = StopLossPips > 0m ? price + StopLossPips * step : null;
		_shortTake = TakeProfitPips > 0m ? price - TakeProfitPips * step : null;

		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
	}

	private void ManagePosition(decimal price, decimal step)
	{
		if (Position > 0)
		{
			ApplyLongTrailing(price, step);

			if (_longTake is decimal take && price >= take)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			if (_longStop is decimal stop && price <= stop)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}
		}
		else if (Position < 0)
		{
			ApplyShortTrailing(price, step);

			if (_shortTake is decimal take && price <= take)
			{
				BuyMarket(-Position);
				ResetShortState();
				return;
			}

			if (_shortStop is decimal stop && price >= stop)
			{
				BuyMarket(-Position);
				ResetShortState();
				return;
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}
	}

	private void ApplyLongTrailing(decimal price, decimal step)
	{
		if (_longEntryPrice is not decimal entry || TrailingStopPips <= 0m)
		{
			return;
		}

		var trailingDistance = TrailingStopPips * step;
		var trailingStep = TrailingStepPips * step;

		if (price - entry <= trailingDistance + trailingStep)
		{
			return;
		}

		var threshold = price - (trailingDistance + trailingStep);
		if (_longStop is null || _longStop < threshold)
		{
			_longStop = price - trailingDistance;
		}
	}

	private void ApplyShortTrailing(decimal price, decimal step)
	{
		if (_shortEntryPrice is not decimal entry || TrailingStopPips <= 0m)
		{
			return;
		}

		var trailingDistance = TrailingStopPips * step;
		var trailingStep = TrailingStepPips * step;

		if (entry - price <= trailingDistance + trailingStep)
		{
			return;
		}

		var threshold = price + trailingDistance + trailingStep;
		if (_shortStop is null || _shortStop > threshold)
		{
			_shortStop = price + trailingDistance;
		}
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
	}
}
