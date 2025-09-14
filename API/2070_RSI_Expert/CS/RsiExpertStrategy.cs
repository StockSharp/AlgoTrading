using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based strategy with overbought and oversold level cross signals.
/// Opens long when RSI crosses above the oversold level and short when it crosses below the overbought level.
/// Optional take profit, stop loss and trailing stop are supported via built-in protection.
/// </summary>
public class RsiExpertStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _levelUp;
	private readonly StrategyParam<decimal> _levelDown;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal _prevRsi;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level for short entries.
	/// </summary>
	public decimal LevelUp
	{
		get => _levelUp.Value;
		set => _levelUp.Value = value;
	}

	/// <summary>
	/// RSI level for long entries.
	/// </summary>
	public decimal LevelDown
	{
		get => _levelDown.Value;
		set => _levelDown.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Trailing stop percentage.
	/// </summary>
	public decimal TrailingStopPercent
	{
		get => _trailingStopPercent.Value;
		set => _trailingStopPercent.Value = value;
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
	/// Initializes a new instance of <see cref="RsiExpertStrategy"/>.
	/// </summary>
	public RsiExpertStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of the RSI indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_levelUp = Param(nameof(LevelUp), 70m)
			.SetDisplay("RSI Overbought", "Upper RSI level triggering a short", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_levelDown = Param(nameof(LevelDown), 30m)
			.SetDisplay("RSI Oversold", "Lower RSI level triggering a long", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
			.SetDisplay("Take Profit %", "Take profit percentage, 0 disables", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 0m)
			.SetDisplay("Stop Loss %", "Stop loss percentage, 0 disables", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 1m);

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 0m)
			.SetDisplay("Trailing Stop %", "Trailing stop percentage, 0 disables", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 1m);

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
		_prevRsi = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		if (TakeProfitPercent > 0m || StopLossPercent > 0m || TrailingStopPercent > 0m)
		{
			StartProtection(
				takeProfit: new Unit(TakeProfitPercent / 100m, UnitTypes.Percent),
				stopLoss: new Unit((TrailingStopPercent > 0m ? TrailingStopPercent : StopLossPercent) / 100m, UnitTypes.Percent),
				isStopTrailing: TrailingStopPercent > 0m);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_rsi.IsFormed)
		{
			_prevRsi = rsiValue;
			return;
		}

		var crossUp = _prevRsi < LevelDown && rsiValue > LevelDown;
		var crossDown = _prevRsi > LevelUp && rsiValue < LevelUp;

		if (crossUp && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && Position >= 0)
		{
			CancelActiveOrders();
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevRsi = rsiValue;
	}
}

