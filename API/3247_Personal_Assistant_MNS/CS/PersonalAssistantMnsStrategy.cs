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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual helper strategy inspired by the MetaTrader personal assistant script.
/// Provides methods that emulate the hotkey actions from the original EA and logs account metrics.
/// </summary>
public class PersonalAssistantMnsStrategy : Strategy
{
	private readonly StrategyParam<int> _magicNumber;
	private readonly StrategyParam<bool> _displayLegend;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private bool _longBreakEvenArmed;
	private bool _shortBreakEvenArmed;
	private decimal? _longManualStopLoss;
	private decimal? _longManualTakeProfit;
	private decimal? _shortManualStopLoss;
	private decimal? _shortManualTakeProfit;

	/// <summary>Magic number used only for logging.</summary>
	public int MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>Whether to print legend lines and status messages.</summary>
	public bool DisplayLegend
	{
		get => _displayLegend.Value;
		set => _displayLegend.Value = value;
	}

	/// <summary>Distance for calculating long or short take-profit levels in pips.</summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>Distance for calculating protective stop-loss levels in pips.</summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>Enable or disable the move-to-break-even trailing rule.</summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>Profit distance (in pips) required before arming the break-even logic.</summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>Offset (in pips) added to the entry price when the break-even stop is moved.</summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>Candle type used for periodic monitoring.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Initializes a new instance of the <see cref="PersonalAssistantMnsStrategy"/> class.</summary>
	public PersonalAssistantMnsStrategy()
	{
		_magicNumber = Param(nameof(MagicNumber), 200808)
		.SetDisplay("Magic Number", "Identifier used only for logging", "General");

		_displayLegend = Param(nameof(DisplayLegend), true)
		.SetDisplay("Display Legend", "Print legend and status updates", "Display");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
		.SetDisplay("Take Profit (pips)", "Distance for take-profit levels", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 500m)
		.SetDisplay("Stop Loss (pips)", "Distance for stop-loss levels", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Break Even", "Enable the move-to-break-even rule", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 20m)
		.SetDisplay("Break Even Trigger", "Profit distance before arming the break-even stop", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 10m)
		.SetDisplay("Break Even Offset", "Offset applied when the stop is moved", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Data type used for monitoring", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		Volume = OrderVolume;
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		if (DisplayLegend)
		{
			LogInfo("Action legend:");
			LogInfo("* PressBuy() opens a long position and arms optional stop/take levels.");
			LogInfo("* PressSell() opens a short position with matching protection.");
			LogInfo("* PressCloseAll() closes every open position.");
			LogInfo("* IncreaseVolume() / DecreaseVolume() adjust the trading volume.");
			LogInfo("* CloseLongPositions() / CloseShortPositions() flatten one side only.");
			LogInfo("* CloseProfitablePositions() exits when floating PnL is positive.");
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_longBreakEvenArmed = false;
			_shortBreakEvenArmed = false;
			_longManualStopLoss = null;
			_longManualTakeProfit = null;
			_shortManualStopLoss = null;
			_shortManualTakeProfit = null;
			return;
		}

		if (Position > 0)
		{
			_shortBreakEvenArmed = false;
			_shortManualStopLoss = null;
			_shortManualTakeProfit = null;
		}
		else if (Position < 0)
		{
			_longBreakEvenArmed = false;
			_longManualStopLoss = null;
			_longManualTakeProfit = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		LogStatus(candle);
		if (!CheckManualExits(candle))
		{
			ApplyTrailingStop(candle);
		}
	}

	private void LogStatus(ICandleMessage candle)
	{
		var ordersBySecurity = Orders.Where(o => o.Security == Security).ToArray();
		var activeOrders = ordersBySecurity.Count(o => o.State == OrderStates.Active);
		var stopOrders = ordersBySecurity.Count(o => o.Type == OrderTypes.Stop || o.Type == OrderTypes.StopLimit);
		var takeOrders = ordersBySecurity.Count(o => o.Type == OrderTypes.TakeProfit || o.Type == OrderTypes.TakeProfitLimit);
		var spread = (Security?.BestAskPrice, Security?.BestBidPrice) is (decimal ask, decimal bid) ? ask - bid : 0m;
		var tickValue = (Security?.StepPrice ?? 0m) * OrderVolume;

		LogInfo(
		$"Magic={MagicNumber}; Symbol={Security?.Id}; Candle={candle.OpenTime}; Position={Position}; PnL={PnL}; " +
		$"Orders={activeOrders}; Stops={stopOrders}; Takes={takeOrders}; Volume={OrderVolume}; Spread={spread}; TickValue={tickValue}"
		);
	}

	private bool CheckManualExits(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longManualTakeProfit is decimal longTp && candle.HighPrice >= longTp)
			{
				SellMarket(Position);
				_longManualTakeProfit = null;
				_longManualStopLoss = null;
				LogInfo($"Long take-profit triggered at {longTp}.");
				return true;
			}

			if (_longManualStopLoss is decimal longSl && candle.LowPrice <= longSl)
			{
				SellMarket(Position);
				_longManualTakeProfit = null;
				_longManualStopLoss = null;
				LogInfo($"Long stop-loss triggered at {longSl}.");
				return true;
			}
		}
		else
		{
			_longManualTakeProfit = null;
			_longManualStopLoss = null;
		}

		if (Position < 0)
		{
			if (_shortManualTakeProfit is decimal shortTp && candle.LowPrice <= shortTp)
			{
				BuyMarket(-Position);
				_shortManualTakeProfit = null;
				_shortManualStopLoss = null;
				LogInfo($"Short take-profit triggered at {shortTp}.");
				return true;
			}

			if (_shortManualStopLoss is decimal shortSl && candle.HighPrice >= shortSl)
			{
				BuyMarket(-Position);
				_shortManualTakeProfit = null;
				_shortManualStopLoss = null;
				LogInfo($"Short stop-loss triggered at {shortSl}.");
				return true;
			}
		}
		else
		{
			_shortManualTakeProfit = null;
			_shortManualStopLoss = null;
		}

		return false;
	}

	private void ApplyTrailingStop(ICandleMessage candle)
	{
		if (!UseTrailingStop || _pipSize <= 0m)
			return;

		var closePrice = candle.ClosePrice;

		if (Position > 0 && PositionPrice is decimal longEntry)
		{
			var triggerDistance = BreakEvenTriggerPips * _pipSize;
			var offsetDistance = BreakEvenOffsetPips * _pipSize;

			if (!_longBreakEvenArmed && closePrice - longEntry >= triggerDistance)
			{
				_longBreakEvenArmed = true;
				LogInfo($"Long break-even armed at price {closePrice}.");
			}

			if (_longBreakEvenArmed)
			{
				var stopPrice = longEntry + offsetDistance;
				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Position);
					_longBreakEvenArmed = false;
					LogInfo($"Long break-even stop executed at {stopPrice}.");
				}
			}
		}
		else
		{
			_longBreakEvenArmed = false;
		}

		if (Position < 0 && PositionPrice is decimal shortEntry)
		{
			var triggerDistance = BreakEvenTriggerPips * _pipSize;
			var offsetDistance = BreakEvenOffsetPips * _pipSize;

			if (!_shortBreakEvenArmed && shortEntry - closePrice >= triggerDistance)
			{
				_shortBreakEvenArmed = true;
				LogInfo($"Short break-even armed at price {closePrice}.");
			}

			if (_shortBreakEvenArmed)
			{
				var stopPrice = shortEntry - offsetDistance;
				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(-Position);
					_shortBreakEvenArmed = false;
					LogInfo($"Short break-even stop executed at {stopPrice}.");
				}
			}
		}
		else
		{
			_shortBreakEvenArmed = false;
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		var decimals = Security?.Decimals;
		if (decimals is 5 or 3)
			return step * 10m;

		return step;
	}

	private decimal? GetReferencePrice(bool isBuy)
	{
		decimal? price = null;

		if (isBuy)
		{
			price = Security?.BestAskPrice ?? Security?.LastTick?.Price ?? Security?.LastPrice;
		}
		else
		{
			price = Security?.BestBidPrice ?? Security?.LastTick?.Price ?? Security?.LastPrice;
		}

		return price;
	}

	/// <summary>Open a long position and cache optional stop-loss and take-profit levels.</summary>
	public void PressBuy()
	{
		var volume = OrderVolume + Math.Abs(Position);
		BuyMarket(volume);

		var referencePrice = GetReferencePrice(isBuy: true);
		if (referencePrice is decimal ask && _pipSize > 0m)
		{
			_longManualTakeProfit = TakeProfitPips > 0m ? ask + 0.25m * TakeProfitPips * _pipSize : null;
			_longManualStopLoss = StopLossPips > 0m ? ask - 4.25m * StopLossPips * _pipSize : null;
			LogInfo($"Long order requested with TP={_longManualTakeProfit ?? "none"}, SL={_longManualStopLoss ?? "none"}.");
		}
		else
		{
			_longManualTakeProfit = null;
			_longManualStopLoss = null;
			LogWarning("Unable to evaluate long stop/take levels because the reference price is missing.");
		}
	}

	/// <summary>Open a short position and cache optional stop-loss and take-profit levels.</summary>
	public void PressSell()
	{
		var volume = OrderVolume + Math.Abs(Position);
		SellMarket(volume);

		var referencePrice = GetReferencePrice(isBuy: false);
		if (referencePrice is decimal bid && _pipSize > 0m)
		{
			_shortManualTakeProfit = TakeProfitPips > 0m ? bid - 0.25m * TakeProfitPips * _pipSize : null;
			_shortManualStopLoss = StopLossPips > 0m ? bid + 3.25m * StopLossPips * _pipSize : null;
			LogInfo($"Short order requested with TP={_shortManualTakeProfit ?? "none"}, SL={_shortManualStopLoss ?? "none"}.");
		}
		else
		{
			_shortManualTakeProfit = null;
			_shortManualStopLoss = null;
			LogWarning("Unable to evaluate short stop/take levels because the reference price is missing.");
		}
	}

	/// <summary>Close all open exposure.</summary>
	public void PressCloseAll()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}
		else
		{
			LogInfo("No position to close.");
		}
	}

	/// <summary>Increase the trading volume by 0.01.</summary>
	public void IncreaseVolume()
	{
		OrderVolume += 0.01m;
		LogInfo($"Volume increased to {OrderVolume}.");
	}

	/// <summary>Decrease the trading volume by 0.01 while respecting the minimum.</summary>
	public void DecreaseVolume()
	{
		if (OrderVolume <= 0.01m)
		{
			LogWarning("Volume is already at the minimum.");
			return;
		}

		OrderVolume -= 0.01m;
		LogInfo($"Volume decreased to {OrderVolume}.");
	}

	/// <summary>Close only the long exposure.</summary>
	public void CloseLongPositions()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else
		{
			LogInfo("No long position to close.");
		}
	}

	/// <summary>Close only the short exposure.</summary>
	public void CloseShortPositions()
	{
		if (Position < 0)
		{
			BuyMarket(-Position);
		}
		else
		{
			LogInfo("No short position to close.");
		}
	}

	/// <summary>Close the position when the floating PnL is positive.</summary>
	public void CloseProfitablePositions()
	{
		if (Position == 0)
		{
			LogInfo("No open position to evaluate for profit.");
			return;
		}

		if (PnL > 0m)
		{
			PressCloseAll();
			LogInfo("Profitable position closed by request.");
		}
		else
		{
			LogInfo("PnL is not positive, position remains open.");
		}
	}
}