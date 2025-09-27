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

/// <summary>
/// Trend-following strategy converted from the MetaTrader "Easiest RSI" expert advisor.
/// </summary>
public class EasiestRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _trailingBufferPoints;

	private RelativeStrengthIndex _rsi;
	private decimal? _previousRsi;
	private decimal? _olderRsi;
	private decimal _pipSize;
	private decimal _pointSize;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _lastLongEntryPrice;
	private decimal? _lastShortEntryPrice;
	private int _longEntries;
	private int _shortEntries;
	private bool _longOrderPending;
	private bool _shortOrderPending;
	private bool _longExitPending;
	private bool _shortExitPending;

	/// <summary>
	/// Initializes a new instance of <see cref="EasiestRsiStrategy"/>.
	/// </summary>
	public EasiestRsiStrategy()
	{
		_lotSize = Param(nameof(LotSize), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Trade volume used for each market order", "Trading")
					.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss (pips)", "Initial stop-loss distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 50m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_stepPips = Param(nameof(StepPips), 20m)
			.SetDisplay("Add-on Step (pips)", "Minimum favourable distance before adding to an existing position", "Strategy")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of candles used for RSI calculation", "Indicators")
			.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold Level", "RSI threshold that triggers a long signal after a cross", "Indicators")
			.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "RSI threshold that triggers a short signal after a cross", "Indicators")
			.SetCanOptimize(true);

		_maxEntries = Param(nameof(MaxEntries), 3)
			.SetGreaterThanZero()
			.SetDisplay("Maximum Entries", "Maximum number of sequential entries allowed in the same direction", "Strategy")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for signal evaluation", "General");

		_trailingBufferPoints = Param(nameof(TrailingBufferPoints), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Buffer", "Minimum improvement in stop level before updating (price steps)", "Risk")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Trade volume used for each market order.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum favourable distance before adding to an existing position.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Number of candles used for RSI calculation.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold that enables long setups after a cross.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// RSI threshold that enables short setups after a cross.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Maximum number of sequential entries allowed in the same direction.
	/// </summary>
	public int MaxEntries
	{
		get => _maxEntries.Value;
		set => _maxEntries.Value = value;
	}

	/// <summary>
	/// Time frame used for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int TrailingBufferPoints
	{
		get => _trailingBufferPoints.Value;
		set => _trailingBufferPoints.Value = value;
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

		_rsi = null;
		_previousRsi = null;
		_olderRsi = null;
		_pipSize = 0m;
		_pointSize = 0m;
		_longStopPrice = null;
		_shortStopPrice = null;
		_lastLongEntryPrice = null;
		_lastShortEntryPrice = null;
		_longEntries = 0;
		_shortEntries = 0;
		_longOrderPending = false;
		_shortOrderPending = false;
		_longExitPending = false;
		_shortExitPending = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = LotSize;

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_pipSize = CalculatePipSize();
		_pointSize = Security?.Step ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order == null)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			_longOrderPending = false;
			_lastLongEntryPrice = trade.Trade.Price;
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			_shortOrderPending = false;
			_lastShortEntryPrice = trade.Trade.Price;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			_longEntries = CalculateEntryCount(Position);
			_shortEntries = 0;
			_lastShortEntryPrice = null;
			_shortExitPending = false;

			var stopDistance = GetStopLossDistance();
			if (stopDistance > 0m)
			{
				var candidate = PositionPrice - stopDistance;
				_longStopPrice = _longStopPrice is decimal current
				? Math.Min(current, candidate)
				: candidate;
			}
			else
			{
				_longStopPrice = null;
			}
		}
		else if (Position < 0m)
		{
			_shortEntries = CalculateEntryCount(Position);
			_longEntries = 0;
			_lastLongEntryPrice = null;
			_longExitPending = false;

			var stopDistance = GetStopLossDistance();
			if (stopDistance > 0m)
			{
				var candidate = PositionPrice + stopDistance;
				_shortStopPrice = _shortStopPrice is decimal current
				? Math.Max(current, candidate)
				: candidate;
			}
			else
			{
				_shortStopPrice = null;
			}
		}
		else
		{
			_longEntries = 0;
			_shortEntries = 0;
			_longStopPrice = null;
			_shortStopPrice = null;
			_lastLongEntryPrice = null;
			_lastShortEntryPrice = null;
			_longOrderPending = false;
			_shortOrderPending = false;
			_longExitPending = false;
			_shortExitPending = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageStops(candle);

		if (_previousRsi is null)
		{
			_previousRsi = rsiValue;
			return;
		}

		if (_olderRsi is null)
		{
			_olderRsi = _previousRsi;
			_previousRsi = rsiValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_olderRsi = _previousRsi;
			_previousRsi = rsiValue;
			return;
		}

		var prev = _previousRsi.Value;
		var older = _olderRsi.Value;

		var crossedAbove = older < OversoldLevel && prev >= OversoldLevel;
		var crossedBelow = older > OverboughtLevel && prev <= OverboughtLevel;

		if (Position == 0m)
		{
			if (crossedAbove && !_longOrderPending)
			{
				SendLongEntry();
			}
			else if (crossedBelow && !_shortOrderPending)
			{
				SendShortEntry();
			}
		}
		else if (Position > 0m)
		{
			TryScaleLong(candle);
		}
		else if (Position < 0m)
		{
			TryScaleShort(candle);
		}

		_olderRsi = prev;
		_previousRsi = rsiValue;
	}

	private void ManageStops(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			ManageLongStops(candle);
		}
		else if (Position < 0m)
		{
			ManageShortStops(candle);
		}
	}

	private void ManageLongStops(ICandleMessage candle)
	{
		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			TryCloseLong();
			return;
		}

		var trailDistance = GetTrailingDistance();
		if (trailDistance <= 0m)
			return;

		var referencePrice = Math.Max(candle.HighPrice, candle.ClosePrice);
		var desiredStop = referencePrice - trailDistance;
		var buffer = GetTrailingBuffer();

		if (_longStopPrice is decimal currentStop)
		{
			if (desiredStop - currentStop > buffer)
				_longStopPrice = desiredStop;
		}
		else
		{
			_longStopPrice = desiredStop;
		}
	}

	private void ManageShortStops(ICandleMessage candle)
	{
		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			TryCloseShort();
			return;
		}

		var trailDistance = GetTrailingDistance();
		if (trailDistance <= 0m)
			return;

		var referencePrice = Math.Min(candle.LowPrice, candle.ClosePrice);
		var desiredStop = referencePrice + trailDistance;
		var buffer = GetTrailingBuffer();

		if (_shortStopPrice is decimal currentStop)
		{
			if (currentStop - desiredStop > buffer)
				_shortStopPrice = desiredStop;
		}
		else
		{
			_shortStopPrice = desiredStop;
		}
	}

	private void TryScaleLong(ICandleMessage candle)
	{
		if (_longEntries <= 0 || _longEntries >= MaxEntries)
			return;

		if (_longOrderPending || _longExitPending)
			return;

		var stepDistance = GetStepDistance();
		if (stepDistance <= 0m)
			return;

		if (_lastLongEntryPrice is not decimal lastPrice)
			return;

		var referencePrice = Math.Max(candle.HighPrice, candle.ClosePrice);
		if (referencePrice < lastPrice + stepDistance)
			return;

		SendLongEntry();
	}

	private void TryScaleShort(ICandleMessage candle)
	{
		if (_shortEntries <= 0 || _shortEntries >= MaxEntries)
			return;

		if (_shortOrderPending || _shortExitPending)
			return;

		var stepDistance = GetStepDistance();
		if (stepDistance <= 0m)
			return;

		if (_lastShortEntryPrice is not decimal lastPrice)
			return;

		var referencePrice = Math.Min(candle.LowPrice, candle.ClosePrice);
		if (referencePrice > lastPrice - stepDistance)
			return;

		SendShortEntry();
	}

	private void SendLongEntry()
	{
		var volume = LotSize;
		if (volume <= 0m)
			return;

		_longOrderPending = true;
		BuyMarket(volume: volume);
	}

	private void SendShortEntry()
	{
		var volume = LotSize;
		if (volume <= 0m)
			return;

		_shortOrderPending = true;
		SellMarket(volume: volume);
	}

	private void TryCloseLong()
	{
		if (_longExitPending)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		_longExitPending = true;
		SellMarket(volume: volume);
	}

	private void TryCloseShort()
	{
		if (_shortExitPending)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		_shortExitPending = true;
		BuyMarket(volume: volume);
	}

	private int CalculateEntryCount(decimal positionVolume)
	{
		var baseVolume = LotSize;
		if (baseVolume <= 0m)
			return 0;

		var ratio = Math.Abs(positionVolume) / baseVolume;
		if (ratio <= 0m)
			return 0;

		return Math.Max(1, (int)Math.Round(ratio, MidpointRounding.AwayFromZero));
	}

	private decimal GetStopLossDistance() => StopLossPips * _pipSize;

	private decimal GetTrailingDistance() => TrailingStopPips * _pipSize;

	private decimal GetStepDistance() => StepPips * _pipSize;

	private decimal GetTrailingBuffer()
	{
		return _pointSize > 0m ? TrailingBufferPoints * _pointSize : 0m;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.Step ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}