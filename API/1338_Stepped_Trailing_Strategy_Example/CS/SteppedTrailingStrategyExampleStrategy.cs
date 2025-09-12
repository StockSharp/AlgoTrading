using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy with stepped profit targets and optional trailing stop.
/// Enters long on SMA crossover and manages position through three stages.
/// </summary>
public class SteppedTrailingStrategyExampleStrategy : Strategy
{
	// Strategy parameters
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfit1Percent;
	private readonly StrategyParam<decimal> _takeProfit2Percent;
	private readonly StrategyParam<decimal> _takeProfit3Percent;
	private readonly StrategyParam<bool> _activateTrailingOnThirdStep;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _fast;
	private SMA _slow;
	private decimal _entryPrice;
	private int _stage;
	private decimal _trailingHigh;
	private decimal _stopLevel;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Stop-loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// First take profit percent.
	/// </summary>
	public decimal TakeProfit1Percent
	{
		get => _takeProfit1Percent.Value;
		set => _takeProfit1Percent.Value = value;
	}

	/// <summary>
	/// Second take profit percent.
	/// </summary>
	public decimal TakeProfit2Percent
	{
		get => _takeProfit2Percent.Value;
		set => _takeProfit2Percent.Value = value;
	}

	/// <summary>
	/// Third take profit percent.
	/// </summary>
	public decimal TakeProfit3Percent
	{
		get => _takeProfit3Percent.Value;
		set => _takeProfit3Percent.Value = value;
	}

	/// <summary>
	/// Activate trailing after second target.
	/// </summary>
	public bool ActivateTrailingOnThirdStep
	{
		get => _activateTrailingOnThirdStep.Value;
		set => _activateTrailingOnThirdStep.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public SteppedTrailingStrategyExampleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA length", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowLength = Param(nameof(SlowLength), 28)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA length", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_takeProfit1Percent = Param(nameof(TakeProfit1Percent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit 1 %", "First take profit percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_takeProfit2Percent = Param(nameof(TakeProfit2Percent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit 2 %", "Second take profit percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 1m);

		_takeProfit3Percent = Param(nameof(TakeProfit3Percent), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit 3 %", "Third take profit percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 30m, 1m);

		_activateTrailingOnThirdStep = Param(nameof(ActivateTrailingOnThirdStep), false)
			.SetDisplay("Activate Trailing", "Enable trailing on third step", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_entryPrice = 0;
		_stage = 0;
		_trailingHigh = 0;
		_stopLevel = 0;
		_prevFast = 0;
		_prevSlow = 0;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_fast = new SMA { Length = FastLength };
		_slow = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fast, _slow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized && _fast.IsFormed && _slow.IsFormed)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_isInitialized = true;
			return;
		}

		if (!_isInitialized)
			return;

		if (Position == 0)
		{
			_stage = 0;

			if (_prevFast <= _prevSlow && fastValue > slowValue)
			{
				_entryPrice = candle.ClosePrice;
				_stage = 1;
				_stopLevel = _entryPrice * (1m - StopLossPercent / 100m);
				BuyMarket();
			}
		}
		else if (Position > 0)
		{
			var tp1Price = _entryPrice * (1m + TakeProfit1Percent / 100m);
			var tp2Price = _entryPrice * (1m + TakeProfit2Percent / 100m);
			var tp3Price = _entryPrice * (1m + TakeProfit3Percent / 100m);

			if (_stage == 1 && candle.HighPrice >= tp1Price)
			{
				_stage = 2;
				_stopLevel = _entryPrice;
			}

			if (_stage == 2 && candle.HighPrice >= tp2Price)
			{
				_stage = 3;
				if (ActivateTrailingOnThirdStep)
					_trailingHigh = candle.HighPrice;
				else
					_stopLevel = tp1Price;
			}

			if (_stage == 3)
			{
				if (ActivateTrailingOnThirdStep)
				{
					var offset = tp3Price - tp2Price;

					if (candle.HighPrice > _trailingHigh)
						_trailingHigh = candle.HighPrice;

					_stopLevel = _trailingHigh - offset;

					if (candle.LowPrice <= _stopLevel)
					{
						SellMarket(Position);
						return;
					}
				}
				else
				{
					if (candle.LowPrice <= _stopLevel || candle.HighPrice >= tp3Price)
					{
						SellMarket(Position);
						return;
					}
				}
			}
			else
			{
				if (candle.LowPrice <= _stopLevel)
				{
					SellMarket(Position);
					return;
				}

				if (candle.HighPrice >= tp3Price)
				{
					SellMarket(Position);
					return;
				}
			}
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
