using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with break-even stop.
/// </summary>
public class OmegaGalskyStrategy : Strategy
{
	private readonly StrategyParam<int> _ema8Period;
	private readonly StrategyParam<int> _ema21Period;
	private readonly StrategyParam<int> _ema89Period;
	private readonly StrategyParam<decimal> _slPercentage;
	private readonly StrategyParam<decimal> _tpPercentage;
	private readonly StrategyParam<decimal> _fixedRiskReward;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema8;
	private ExponentialMovingAverage _ema21;
	private ExponentialMovingAverage _ema89;

	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;
	private bool _stopMovedToBreakEven;
	private bool _wasEma8BelowEma21;
	private bool _isInitialized;

	/// <summary>
	/// EMA8 period.
	/// </summary>
	public int Ema8Period
	{
		get => _ema8Period.Value;
		set => _ema8Period.Value = value;
	}

	/// <summary>
	/// EMA21 period.
	/// </summary>
	public int Ema21Period
	{
		get => _ema21Period.Value;
		set => _ema21Period.Value = value;
	}

	/// <summary>
	/// EMA89 period.
	/// </summary>
	public int Ema89Period
	{
		get => _ema89Period.Value;
		set => _ema89Period.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal SlPercentage
	{
		get => _slPercentage.Value;
		set => _slPercentage.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TpPercentage
	{
		get => _tpPercentage.Value;
		set => _tpPercentage.Value = value;
	}

	/// <summary>
	/// Risk reward multiplier for moving stop to break-even.
	/// </summary>
	public decimal FixedRiskReward
	{
		get => _fixedRiskReward.Value;
		set => _fixedRiskReward.Value = value;
	}

	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OmegaGalskyStrategy()
	{
		_ema8Period = Param(nameof(Ema8Period), 8)
			.SetGreaterThanZero()
			.SetDisplay("EMA 8 Period", "Period for fast EMA", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(4, 20, 1);

		_ema21Period = Param(nameof(Ema21Period), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA 21 Period", "Period for slow EMA", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 1);

		_ema89Period = Param(nameof(Ema89Period), 89)
			.SetGreaterThanZero()
			.SetDisplay("EMA 89 Period", "Trend EMA period", "EMA");

		_slPercentage = Param(nameof(SlPercentage), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.0005m, 0.005m, 0.0005m);

		_tpPercentage = Param(nameof(TpPercentage), 0.0025m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.01m, 0.001m);

		_fixedRiskReward = Param(nameof(FixedRiskReward), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Multiplier for moving stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

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
		_stopLoss = 0;
		_takeProfit = 0;
		_stopMovedToBreakEven = false;
		_wasEma8BelowEma21 = false;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema8 = new ExponentialMovingAverage { Length = Ema8Period };
		_ema21 = new ExponentialMovingAverage { Length = Ema21Period };
		_ema89 = new ExponentialMovingAverage { Length = Ema89Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema8, _ema21, _ema89, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema8);
			DrawIndicator(area, _ema21);
			DrawIndicator(area, _ema89);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema8Value, decimal ema21Value, decimal ema89Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized && _ema8.IsFormed && _ema21.IsFormed && _ema89.IsFormed)
		{
			_wasEma8BelowEma21 = ema8Value < ema21Value;
			_isInitialized = true;
			return;
		}

		if (!_isInitialized)
			return;

		var isEma8BelowEma21 = ema8Value < ema21Value;

		if (_wasEma8BelowEma21 && !isEma8BelowEma21 && candle.ClosePrice > ema89Value && candle.ClosePrice > candle.OpenPrice && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopLoss = _entryPrice * (1 - SlPercentage);
			_takeProfit = _entryPrice * (1 + TpPercentage);
			_stopMovedToBreakEven = false;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!_wasEma8BelowEma21 && isEma8BelowEma21 && candle.ClosePrice < ema89Value && candle.ClosePrice < candle.OpenPrice && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopLoss = _entryPrice * (1 + SlPercentage);
			_takeProfit = _entryPrice * (1 - TpPercentage);
			_stopMovedToBreakEven = false;
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0)
		{
			if (!_stopMovedToBreakEven && candle.HighPrice >= _entryPrice + (_entryPrice * SlPercentage * FixedRiskReward))
			{
				_stopLoss = _entryPrice;
				_stopMovedToBreakEven = true;
			}

			if (candle.LowPrice <= _stopLoss)
				SellMarket(Math.Abs(Position));
			else if (candle.HighPrice >= _takeProfit)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (!_stopMovedToBreakEven && candle.LowPrice <= _entryPrice - (_entryPrice * SlPercentage * FixedRiskReward))
			{
				_stopLoss = _entryPrice;
				_stopMovedToBreakEven = true;
			}

			if (candle.HighPrice >= _stopLoss)
				BuyMarket(Math.Abs(Position));
			else if (candle.LowPrice <= _takeProfit)
				BuyMarket(Math.Abs(Position));
		}

		_wasEma8BelowEma21 = isEma8BelowEma21;
	}
}
