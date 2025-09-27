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
/// Parabolic SAR breakout strategy based on the MetaTrader expert "Parabolic SAR EA".
/// Opens a long position when the SAR value drops below the completed candle low and sells when the SAR rises above the high.
/// Protective stop-loss and take-profit levels replicate the MetaTrader pip handling with fractional quotes.
/// </summary>
public class ParabolicSarEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal _pipSize = 0.0001m;

	/// <summary>
	/// Desired trading volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader-style pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader-style pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Acceleration step for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
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
	/// Initializes a new instance of the <see cref="ParabolicSarEaStrategy"/> class.
	/// </summary>
	public ParabolicSarEaStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order volume measured in lots", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop-Loss (pips)", "Stop-loss distance in MetaTrader pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take-Profit (pips)", "Take-profit distance in MetaTrader pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration step of the Parabolic SAR", "Indicator")
			.SetRange(0.01m, 0.1m)
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetDisplay("SAR Maximum", "Maximum acceleration factor for the Parabolic SAR", "Indicator")
			.SetRange(0.1m, 0.5m)
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for Parabolic SAR signals", "Trading");

		Volume = _tradeVolume.Value;
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

		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaximum
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		// Trade only on finished candles to mirror the MetaTrader new-bar logic.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the subscription is active and the strategy is allowed to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		CheckProtectiveLevels(candle);

		var low = candle.LowPrice;
		var high = candle.HighPrice;

		if (sarValue < low)
		{
			TryEnterLong(candle, sarValue);
		}
		else if (sarValue > high)
		{
			TryEnterShort(candle, sarValue);
		}
	}

	private void TryEnterLong(ICandleMessage candle, decimal sarValue)
	{
		var desiredVolume = Volume;

		if (desiredVolume <= 0m)
			return;

		var position = Position;

		if (position > 0m)
			return;

		if (position < 0m)
			desiredVolume += Math.Abs(position);

		if (desiredVolume <= 0m)
			return;

		BuyMarket(desiredVolume);

		var entryPrice = candle.ClosePrice;
		var stop = StopLossPips > 0 ? entryPrice - StopLossPips * _pipSize : (decimal?)null;
		var take = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * _pipSize : (decimal?)null;

		_longStop = stop;
		_longTake = take;
		ResetShortTargets();

		LogInfo($"Long entry: close={entryPrice}, sar={sarValue}, stop={_longStop}, take={_longTake}");
	}

	private void TryEnterShort(ICandleMessage candle, decimal sarValue)
	{
		var desiredVolume = Volume;

		if (desiredVolume <= 0m)
			return;

		var position = Position;

		if (position < 0m)
			return;

		if (position > 0m)
			desiredVolume += Math.Abs(position);

		if (desiredVolume <= 0m)
			return;

		SellMarket(desiredVolume);

		var entryPrice = candle.ClosePrice;
		var stop = StopLossPips > 0 ? entryPrice + StopLossPips * _pipSize : (decimal?)null;
		var take = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * _pipSize : (decimal?)null;

		_shortStop = stop;
		_shortTake = take;
		ResetLongTargets();

		LogInfo($"Short entry: close={entryPrice}, sar={sarValue}, stop={_shortStop}, take={_shortTake}");
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		var position = Position;

		if (position > 0m)
		{
			var volume = Math.Abs(position);

			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(volume);
				LogInfo($"Long stop-loss triggered at {stop}.");
				ResetLongTargets();
				return;
			}

			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket(volume);
				LogInfo($"Long take-profit triggered at {take}.");
				ResetLongTargets();
			}
		}
		else if (position < 0m)
		{
			var volume = Math.Abs(position);

			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				LogInfo($"Short stop-loss triggered at {stop}.");
				ResetShortTargets();
				return;
			}

			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(volume);
				LogInfo($"Short take-profit triggered at {take}.");
				ResetShortTargets();
			}
		}
	}

	private void ResetLongTargets()
	{
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortTargets()
	{
		_shortStop = null;
		_shortTake = null;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;

		if (step <= 0m)
			step = 0.0001m;

		var decimals = Security?.Decimals ?? GetDecimals(step);
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;

		return step * multiplier;
	}

	private static int GetDecimals(decimal value)
	{
		value = Math.Abs(value);

		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}

