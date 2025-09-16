using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Auto SL-TP Setter strategy.
/// Automatically places stop loss and take profit if missing.
/// </summary>
public class AutoSlTpSetterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _setStopLoss;
	private readonly StrategyParam<bool> _setTakeProfit;
	private readonly StrategyParam<int> _stopLossMethod;
	private readonly StrategyParam<decimal> _fixedStopLoss;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<int> _takeProfitMethod;
	private readonly StrategyParam<decimal> _fixedTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitAtr;
	private readonly StrategyParam<int> _atrPeriod;

	private AverageTrueRange _atr;
	private bool _protectionStarted;

	/// <summary>Type of candles for ATR calculation.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Enable automatic stop loss placement.</summary>
	public bool SetStopLoss { get => _setStopLoss.Value; set => _setStopLoss.Value = value; }

	/// <summary>Enable automatic take profit placement.</summary>
	public bool SetTakeProfit { get => _setTakeProfit.Value; set => _setTakeProfit.Value = value; }

	/// <summary>Stop loss calculation method: 1 = fixed pips, 2 = ATR multiple.</summary>
	public int StopLossMethod { get => _stopLossMethod.Value; set => _stopLossMethod.Value = value; }

	/// <summary>Fixed stop loss in pips.</summary>
	public decimal FixedStopLoss { get => _fixedStopLoss.Value; set => _fixedStopLoss.Value = value; }

	/// <summary>ATR multiplier for stop loss.</summary>
	public decimal StopLossAtr { get => _stopLossAtr.Value; set => _stopLossAtr.Value = value; }

	/// <summary>Take profit calculation method: 1 = fixed pips, 2 = ATR multiple.</summary>
	public int TakeProfitMethod { get => _takeProfitMethod.Value; set => _takeProfitMethod.Value = value; }

	/// <summary>Fixed take profit in pips.</summary>
	public decimal FixedTakeProfit { get => _fixedTakeProfit.Value; set => _fixedTakeProfit.Value = value; }

	/// <summary>ATR multiplier for take profit.</summary>
	public decimal TakeProfitAtr { get => _takeProfitAtr.Value; set => _takeProfitAtr.Value = value; }

	/// <summary>ATR period.</summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public AutoSlTpSetterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "ATR time frame", "General");

		_setStopLoss = Param(nameof(SetStopLoss), true)
			.SetDisplay("Set Stop Loss", "Automatically set stop loss", "General");

		_setTakeProfit = Param(nameof(SetTakeProfit), true)
			.SetDisplay("Set Take Profit", "Automatically set take profit", "General");

		_stopLossMethod = Param(nameof(StopLossMethod), 1)
			.SetDisplay("Stop Loss Method", "1=Fixed pips  2=ATR multiple", "Stop Loss")
			.SetCanOptimize(true);

		_fixedStopLoss = Param(nameof(FixedStopLoss), 5m)
			.SetDisplay("Fixed SL (pips)", "Fixed stop loss in pips", "Stop Loss")
			.SetCanOptimize(true);

		_stopLossAtr = Param(nameof(StopLossAtr), 0.7m)
			.SetDisplay("SL ATR Multiplier", "ATR multiplier for stop loss", "Stop Loss")
			.SetCanOptimize(true);

		_takeProfitMethod = Param(nameof(TakeProfitMethod), 1)
			.SetDisplay("Take Profit Method", "1=Fixed pips  2=ATR multiple", "Take Profit")
			.SetCanOptimize(true);

		_fixedTakeProfit = Param(nameof(FixedTakeProfit), 10m)
			.SetDisplay("Fixed TP (pips)", "Fixed take profit in pips", "Take Profit")
			.SetCanOptimize(true);

		_takeProfitAtr = Param(nameof(TakeProfitAtr), 1.8m)
			.SetDisplay("TP ATR Multiplier", "ATR multiplier for take profit", "Take Profit")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 30)
			.SetDisplay("ATR Period", "ATR calculation period", "ATR");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if ((SetStopLoss && StopLossMethod == 2) || (SetTakeProfit && TakeProfitMethod == 2))
		{
			_atr = new AverageTrueRange { Length = AtrPeriod };

			SubscribeCandles(CandleType)
				.Bind(_atr, OnAtr)
				.Start();
		}
		else
		{
			StartProtection(
				takeProfit: SetTakeProfit ? new Unit(FixedTakeProfit, UnitTypes.Step) : default,
				stopLoss: SetStopLoss ? new Unit(FixedStopLoss, UnitTypes.Step) : default);
			_protectionStarted = true;
		}
	}

	private void OnAtr(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished || _protectionStarted)
			return;

		var step = Security?.PriceStep ?? 1m;

		var stop = StopLossMethod == 2 ? atrValue * StopLossAtr / step : FixedStopLoss;
		var take = TakeProfitMethod == 2 ? atrValue * TakeProfitAtr / step : FixedTakeProfit;

		StartProtection(
			takeProfit: SetTakeProfit ? new Unit(take, UnitTypes.Step) : default,
			stopLoss: SetStopLoss ? new Unit(stop, UnitTypes.Step) : default);
		_protectionStarted = true;
	}
}
