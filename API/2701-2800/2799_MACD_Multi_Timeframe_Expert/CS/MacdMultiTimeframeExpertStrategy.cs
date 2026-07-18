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
/// Multi-timeframe MACD confirmation strategy that aligns primary and confirmation timeframe trends.
/// </summary>
public class MacdMultiTimeframeExpertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _primaryType;
	private readonly StrategyParam<DataType> _confirmType;

	private MovingAverageConvergenceDivergenceSignal _macdPrimary;
	private MovingAverageConvergenceDivergenceSignal _macdConfirm;

	private int? _relationPrimary;
	private int? _relationConfirm;
	private int _lastTradeDirection;
	private int _candlesSinceEntry;

	private decimal _entryPrice;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Fast EMA period used by MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used by MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period used by MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for the primary execution timeframe.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryType.Value;
		set => _primaryType.Value = value;
	}

	/// <summary>
	/// Candle type for the confirmation timeframe.
	/// </summary>
	public DataType ConfirmCandleType
	{
		get => _confirmType.Value;
		set => _confirmType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdMultiTimeframeExpertStrategy"/> class.
	/// </summary>
	public MacdMultiTimeframeExpertStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Position size in lots", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Points", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2500m)
			.SetNotNegative()
			.SetDisplay("Take Profit Points", "Take-profit distance in points", "Risk");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period", "MACD");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period", "MACD");

		_primaryType = Param(nameof(PrimaryCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Primary", "Primary execution timeframe", "Timeframes");

		_confirmType = Param(nameof(ConfirmCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Confirm", "Confirmation timeframe", "Timeframes");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, PrimaryCandleType);
		yield return (Security, ConfirmCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macdPrimary = null;
		_macdConfirm = null;
		_relationPrimary = null;
		_relationConfirm = null;
		_lastTradeDirection = 0;
		_candlesSinceEntry = 0;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macdPrimary = CreateMacd();
		_macdConfirm = CreateMacd();

		var primarySubscription = SubscribeCandles(PrimaryCandleType);
		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		SubscribeCandles(ConfirmCandleType)
			.Bind(ProcessConfirmCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawOwnTrades(area);
		}
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
		return new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod }
			},
			SignalMa = { Length = SignalPeriod }
		};
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdValue = _macdPrimary.Process(candle);
		if (!TryUpdateRelation(macdValue, out var relation))
			return;

		_relationPrimary = relation;
		_candlesSinceEntry++;

		// Manage protective exits whenever a position is open.
		if (Position != 0)
		{
			ManageOpenPosition(candle);

			// If position was closed by SL/TP, allow new entry below
			if (Position != 0)
				return;
		}

		if (!_relationConfirm.HasValue)
			return;

		if (OrderVolume <= 0)
			return;

		// Cooldown: require at least 6 candles between trades.
		if (_candlesSinceEntry < 6)
			return;

		// Determine aligned direction: both timeframes must agree.
		var alignedDirection = 0;

		if (_relationPrimary == 1 && _relationConfirm == 1)
			alignedDirection = 1;
		else if (_relationPrimary == -1 && _relationConfirm == -1)
			alignedDirection = -1;

		if (alignedDirection == 0)
			return;

		// Avoid repeated entries in the same direction.
		if (_lastTradeDirection == alignedDirection)
			return;

		_lastTradeDirection = alignedDirection;
		_candlesSinceEntry = 0;

		if (alignedDirection > 0)
		{
			BuyMarket(OrderVolume);
			_entryPrice = candle.ClosePrice;
		}
		else
		{
			SellMarket(OrderVolume);
			_entryPrice = candle.ClosePrice;
		}
	}

	private void ProcessConfirmCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdValue = _macdConfirm.Process(candle);
		if (TryUpdateRelation(macdValue, out var relation))
			_relationConfirm = relation;
	}

	private bool TryUpdateRelation(IIndicatorValue macdValue, out int relation)
	{
		relation = 0;

		if (!macdValue.IsFinal)
			return false;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
			return false;

		// Standard MACD: macd > signal = bullish, macd < signal = bearish.
		if (macd > signal)
			relation = 1;
		else if (macd < signal)
			relation = -1;
		else
			relation = 0;

		return true;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		// Derive the point value. Fall back to 1 if the security lacks a price step.
		var point = Security?.PriceStep ?? 0m;
		if (point <= 0)
			point = 1m;

		if (Position > 0)
		{
			if (TakeProfitPoints > 0 && candle.HighPrice >= _entryPrice + TakeProfitPoints * point)
			{
				SellMarket(Position);
				_entryPrice = 0m;
				_lastTradeDirection = 0;
				return;
			}

			if (StopLossPoints > 0 && candle.LowPrice <= _entryPrice - StopLossPoints * point)
			{
				SellMarket(Position);
				_entryPrice = 0m;
				_lastTradeDirection = 0;
			}
		}
		else if (Position < 0)
		{
			var volume = Position.Abs();

			if (TakeProfitPoints > 0 && candle.LowPrice <= _entryPrice - TakeProfitPoints * point)
			{
				BuyMarket(volume);
				_entryPrice = 0m;
				_lastTradeDirection = 0;
				return;
			}

			if (StopLossPoints > 0 && candle.HighPrice >= _entryPrice + StopLossPoints * point)
			{
				BuyMarket(volume);
				_entryPrice = 0m;
				_lastTradeDirection = 0;
			}
		}
		else
		{
			_entryPrice = 0m;
		}
	}
}
