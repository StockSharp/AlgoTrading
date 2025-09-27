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
/// Gap DM strategy that trades against overnight gaps.
/// Buys when the new session opens below the previous close by a minimum gap.
/// Sells when the market opens above the previous close by a minimum gap.
/// Optional stop-loss and take-profit can be applied to every entry.
/// </summary>
public class GapDMStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _minGapPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _minGapSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _maxExposure;
	private decimal _entryPrice;
	private decimal? _activeStopPrice;
	private decimal? _activeTakePrice;
	private decimal _previousClose;
	private bool _hasPreviousClose;

	/// <summary>
	/// Trading volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Minimum opening gap measured in pips.
	/// </summary>
	public decimal MinGapPips
	{
		get => _minGapPips.Value;
		set => _minGapPips.Value = value;
	}

	/// <summary>
	/// Maximum aggregated number of lots allowed per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Candle type used to detect the gap between sessions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GapDMStrategy"/> class.
	/// </summary>
	public GapDMStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Order Volume", "Trading volume in lots", "General");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Stop Loss (pips)", "Protective stop measured in pips. Set 0 to disable.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Take Profit (pips)", "Profit target measured in pips. Set 0 to disable.", "Risk");

		_minGapPips = Param(nameof(MinGapPips), 1m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Minimum Gap (pips)", "Minimum distance between previous close and current open.", "Signals");

		_maxPositions = Param(nameof(MaxPositions), 15)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of lots allowed in one direction.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for gap detection.", "General");
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

		// Reset cached state when the strategy is restarted.
		_pipSize = 0m;
		_minGapSize = 0m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_maxExposure = 0m;
		_entryPrice = 0m;
		_activeStopPrice = null;
		_activeTakePrice = null;
		_previousClose = 0m;
		_hasPreviousClose = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (OrderVolume <= 0m)
			throw new InvalidOperationException("Order volume must be positive.");

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var decimals = GetDecimalPlaces(step);
		_pipSize = (decimals == 3 || decimals == 5) ? step * 10m : step;

		_minGapSize = MinGapPips * _pipSize;
		_stopLossOffset = StopLossPips * _pipSize;
		_takeProfitOffset = TakeProfitPips * _pipSize;
		_maxExposure = Math.Max(0, MaxPositions) * OrderVolume;

		Volume = OrderVolume;

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

		// Manage existing position before evaluating new signals.
		if (Position > 0m)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0m)
		{
			ManageShortPosition(candle);
		}
		else
		{
			ResetProtection();
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousClose(candle);
			return;
		}

		if (_hasPreviousClose)
		{
			var gapDown = _previousClose - candle.OpenPrice >= _minGapSize;
			var gapUp = candle.OpenPrice - _previousClose >= _minGapSize;

			if (gapDown)
			{
				var gapSize = _previousClose - candle.OpenPrice;
				TryEnterLong(candle.OpenPrice, gapSize);
			}
			else if (gapUp)
			{
				var gapSize = candle.OpenPrice - _previousClose;
				TryEnterShort(candle.OpenPrice, gapSize);
			}
		}

		// Re-evaluate stops after potential entries to catch intrabar triggers.
		if (Position > 0m)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0m)
		{
			ManageShortPosition(candle);
		}

		UpdatePreviousClose(candle);
	}

	private void TryEnterLong(decimal entryPrice, decimal gapSize)
	{
		var volume = CalculateBuyVolume();
		if (volume <= 0m)
			return;

		CancelActiveOrders();
		BuyMarket(volume);

		_entryPrice = entryPrice;
		_activeStopPrice = _stopLossOffset > 0m ? entryPrice - _stopLossOffset : null;
		_activeTakePrice = _takeProfitOffset > 0m ? entryPrice + _takeProfitOffset : null;

		if (_pipSize > 0m)
		{
			var gapInPips = gapSize / _pipSize;
			LogInfo($"Gap down of {gapInPips:F2} pips detected. Entering long with volume {volume}.");
		}
		else
		{
			LogInfo($"Gap down detected. Entering long with volume {volume}.");
		}
	}

	private void TryEnterShort(decimal entryPrice, decimal gapSize)
	{
		var volume = CalculateSellVolume();
		if (volume <= 0m)
			return;

		CancelActiveOrders();
		SellMarket(volume);

		_entryPrice = entryPrice;
		_activeStopPrice = _stopLossOffset > 0m ? entryPrice + _stopLossOffset : null;
		_activeTakePrice = _takeProfitOffset > 0m ? entryPrice - _takeProfitOffset : null;

		if (_pipSize > 0m)
		{
			var gapInPips = gapSize / _pipSize;
			LogInfo($"Gap up of {gapInPips:F2} pips detected. Entering short with volume {volume}.");
		}
		else
		{
			LogInfo($"Gap up detected. Entering short with volume {volume}.");
		}
	}

	private decimal CalculateBuyVolume()
	{
		if (_maxExposure <= 0m)
			return 0m;

		if (Position >= _maxExposure)
			return 0m;

		var allowed = _maxExposure - Position;
		var additional = OrderVolume;
		if (additional > allowed)
			additional = allowed;

		if (additional <= 0m)
			return 0m;

		var volume = additional;
		if (Position < 0m)
			volume += Math.Abs(Position);

		return volume;
	}

	private decimal CalculateSellVolume()
	{
		if (_maxExposure <= 0m)
			return 0m;

		if (-Position >= _maxExposure)
			return 0m;

		var allowed = _maxExposure + Position;
		if (allowed <= 0m)
			return 0m;

		var additional = OrderVolume;
		if (additional > allowed)
			additional = allowed;

		if (additional <= 0m)
			return 0m;

		var volume = additional;
		if (Position > 0m)
			volume += Math.Abs(Position);

		return volume;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (_activeStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(volume);
			LogInfo($"Long stop-loss triggered at {stop}.");
			ResetProtection();
			return;
		}

		if (_activeTakePrice is decimal take && candle.HighPrice >= take)
		{
			SellMarket(volume);
			LogInfo($"Long take-profit reached at {take}.");
			ResetProtection();
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (_activeStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(volume);
			LogInfo($"Short stop-loss triggered at {stop}.");
			ResetProtection();
			return;
		}

		if (_activeTakePrice is decimal take && candle.LowPrice <= take)
		{
			BuyMarket(volume);
			LogInfo($"Short take-profit reached at {take}.");
			ResetProtection();
		}
	}

	private void ResetProtection()
	{
		_activeStopPrice = null;
		_activeTakePrice = null;
		_entryPrice = 0m;
	}

	private void UpdatePreviousClose(ICandleMessage candle)
	{
		_previousClose = candle.ClosePrice;
		_hasPreviousClose = true;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		var exponent = (bits[3] >> 16) & 0xFF;
		return exponent;
	}
}

