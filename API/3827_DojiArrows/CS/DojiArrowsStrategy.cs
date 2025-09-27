namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;
using StockSharp.Algo.Candles;

/// <summary>
/// Strategy that replicates the Doji Arrows breakout expert.
/// The algorithm looks for a doji candle followed by a breakout on the next bar.
/// </summary>
public class DojiArrowsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _dojiBodyThresholdSteps;
	private readonly StrategyParam<decimal> _breakoutBufferSteps;
	private readonly StrategyParam<decimal> _initialStopSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;

	private ICandleMessage _previousCandle;
	private ICandleMessage _twoCandlesAgo;
	private bool _wasLongSignal;
	private bool _wasShortSignal;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;

	/// <summary>
	/// Candle type used for signal calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum allowed body size (in price steps) to treat a candle as doji.
	/// </summary>
	public decimal DojiBodyThresholdSteps
	{
		get => _dojiBodyThresholdSteps.Value;
		set => _dojiBodyThresholdSteps.Value = value;
	}

	/// <summary>
	/// Additional buffer (in price steps) for breakout validation.
	/// </summary>
	public decimal BreakoutBufferSteps
	{
		get => _breakoutBufferSteps.Value;
		set => _breakoutBufferSteps.Value = value;
	}

	/// <summary>
	/// Initial stop distance (in price steps) applied after entry.
	/// </summary>
	public decimal InitialStopSteps
	{
		get => _initialStopSteps.Value;
		set => _initialStopSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance (in price steps) applied after entry.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Trailing stop distance (in price steps) applied once position moves in profit.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="DojiArrowsStrategy"/>.
	/// </summary>
	public DojiArrowsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for signals", "General");

		_dojiBodyThresholdSteps = Param(nameof(DojiBodyThresholdSteps), 1m)
			.SetDisplay("Doji Body Threshold", "Maximum candle body size in steps", "Signals")
			.SetCanOptimize(true);

		_breakoutBufferSteps = Param(nameof(BreakoutBufferSteps), 0m)
			.SetDisplay("Breakout Buffer", "Extra breakout filter in steps", "Signals")
			.SetCanOptimize(true);

		_initialStopSteps = Param(nameof(InitialStopSteps), 20m)
			.SetDisplay("Initial Stop", "Initial protective stop distance in steps", "Risk")
			.SetCanOptimize(true);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 25m)
			.SetDisplay("Take Profit", "Take-profit distance in steps", "Risk")
			.SetCanOptimize(true);

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 10m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in steps", "Risk")
			.SetCanOptimize(true);
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

		_previousCandle = null;
		_twoCandlesAgo = null;
		_wasLongSignal = false;
		_wasShortSignal = false;
		ResetLongProtection();
		ResetShortProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			LogWarning("Price step is not defined; risk controls are disabled.");
		}

		if (_previousCandle != null && _twoCandlesAgo != null)
		{
			var dojiBody = Math.Abs(_twoCandlesAgo.ClosePrice - _twoCandlesAgo.OpenPrice);
			var dojiThreshold = DojiBodyThresholdSteps * priceStep;
			var isDoji = priceStep <= 0m ? dojiBody == 0m : dojiBody <= dojiThreshold;

			var breakoutBuffer = BreakoutBufferSteps * priceStep;
			var longBreakout = _previousCandle.ClosePrice > _twoCandlesAgo.HighPrice + breakoutBuffer;
			var shortBreakout = _previousCandle.ClosePrice < _twoCandlesAgo.LowPrice - breakoutBuffer;

			var longSignal = isDoji && longBreakout;
			var shortSignal = isDoji && shortBreakout;

			if (longSignal && !_wasLongSignal)
			{
				HandleLongSignal(candle);
			}

			if (shortSignal && !_wasShortSignal)
			{
				HandleShortSignal(candle);
			}

			_wasLongSignal = longSignal;
			_wasShortSignal = shortSignal;
		}

		UpdateRiskManagement(candle, priceStep);

		_twoCandlesAgo = _previousCandle;
		_previousCandle = candle;
	}

	private void HandleLongSignal(ICandleMessage candle)
	{
		if (Position < 0)
		{
			ClosePosition();
			ResetShortProtection();
		}

		if (Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);

			_longEntryPrice = candle.ClosePrice;
			ConfigureLongProtection();

			LogInfo($"Long breakout after doji at {candle.ClosePrice} on {candle.OpenTime:O}");
		}
	}

	private void HandleShortSignal(ICandleMessage candle)
	{
		if (Position > 0)
		{
			ClosePosition();
			ResetLongProtection();
		}

		if (Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);

			_shortEntryPrice = candle.ClosePrice;
			ConfigureShortProtection();

			LogInfo($"Short breakout after doji at {candle.ClosePrice} on {candle.OpenTime:O}");
		}
	}

	private void ConfigureLongProtection()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m || !_longEntryPrice.HasValue)
		{
			ResetLongProtection();
			return;
		}

		_longStopPrice = InitialStopSteps > 0m ? _longEntryPrice - InitialStopSteps * priceStep : null;
		_longTakeProfitPrice = TakeProfitSteps > 0m ? _longEntryPrice + TakeProfitSteps * priceStep : null;
	}

	private void ConfigureShortProtection()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m || !_shortEntryPrice.HasValue)
		{
			ResetShortProtection();
			return;
		}

		_shortStopPrice = InitialStopSteps > 0m ? _shortEntryPrice + InitialStopSteps * priceStep : null;
		_shortTakeProfitPrice = TakeProfitSteps > 0m ? _shortEntryPrice - TakeProfitSteps * priceStep : null;
	}

	private void UpdateRiskManagement(ICandleMessage candle, decimal priceStep)
	{
		if (priceStep <= 0m)
		{
			return;
		}

		if (Position > 0 && _longEntryPrice.HasValue)
		{
			if (TrailingStopSteps > 0m)
			{
				var trailingDistance = TrailingStopSteps * priceStep;
				if (candle.ClosePrice - _longEntryPrice.Value > trailingDistance)
				{
					var newStop = candle.ClosePrice - trailingDistance;
					if (!_longStopPrice.HasValue || newStop > _longStopPrice.Value)
					{
						_longStopPrice = newStop;
					}
				}
			}

			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				LogInfo($"Long stop triggered at {_longStopPrice.Value}");
				ClosePosition();
				ResetLongProtection();
			}
			else if (_longTakeProfitPrice.HasValue && candle.HighPrice >= _longTakeProfitPrice.Value)
			{
				LogInfo($"Long take-profit reached at {_longTakeProfitPrice.Value}");
				ClosePosition();
				ResetLongProtection();
			}
		}
		else if (Position < 0 && _shortEntryPrice.HasValue)
		{
			if (TrailingStopSteps > 0m)
			{
				var trailingDistance = TrailingStopSteps * priceStep;
				if (_shortEntryPrice.Value - candle.ClosePrice > trailingDistance)
				{
					var newStop = candle.ClosePrice + trailingDistance;
					if (!_shortStopPrice.HasValue || newStop < _shortStopPrice.Value)
					{
						_shortStopPrice = newStop;
					}
				}
			}

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				LogInfo($"Short stop triggered at {_shortStopPrice.Value}");
				ClosePosition();
				ResetShortProtection();
			}
			else if (_shortTakeProfitPrice.HasValue && candle.LowPrice <= _shortTakeProfitPrice.Value)
			{
				LogInfo($"Short take-profit reached at {_shortTakeProfitPrice.Value}");
				ClosePosition();
				ResetShortProtection();
			}
		}
	}

	private void ResetLongProtection()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShortProtection()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}
}