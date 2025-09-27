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
/// Directional strategy that replicates the MetaTrader "nextbar" expert advisor.
/// </summary>
public class NextbarStrategy : Strategy
{
	/// <summary>
	/// Trading direction modes supported by the original expert advisor.
	/// </summary>
	public enum NextbarDirection
	{
		/// <summary>
		/// Follow the detected momentum (buy after a rise, sell after a drop).
		/// </summary>
		Follow = 1,

		/// <summary>
		/// Trade against the momentum (buy after a drop, sell after a rise).
		/// </summary>
		Reverse = 2,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _barsToCheck;
	private readonly StrategyParam<int> _barsToHold;
	private readonly StrategyParam<int> _minMovePoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<NextbarDirection> _direction;

	private readonly List<decimal> _closeHistory = new();

	private int _barsSinceEntry;
	private decimal? _entryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="NextbarStrategy"/> class.
	/// </summary>
	public NextbarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for price comparisons", "General");

		_barsToCheck = Param(nameof(BarsToCheck), 8)
			.SetGreaterThanZero()
			.SetDisplay("Bars To Check", "Lookback distance for the momentum filter", "Trading Rules")
			.SetCanOptimize(true);

		_barsToHold = Param(nameof(BarsToHold), 10)
			.SetGreaterThanZero()
			.SetDisplay("Bars To Hold", "Maximum number of completed candles to keep a trade open", "Trading Rules")
			.SetCanOptimize(true);

		_minMovePoints = Param(nameof(MinMovePoints), 77)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Move (points)", "Required point distance between the recent closes", "Trading Rules")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 115)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Target distance counted in MetaTrader points", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 115)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Protective distance expressed in MetaTrader points", "Risk")
			.SetCanOptimize(true);

		_direction = Param(nameof(Direction), NextbarDirection.Follow)
			.SetDisplay("Direction", "Choose between trend-following or contrarian behaviour", "Trading Rules");
	}

	/// <summary>
	/// Candle type to subscribe for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of completed candles separating the two close prices compared for the trigger.
	/// </summary>
	public int BarsToCheck
	{
		get => _barsToCheck.Value;
		set => _barsToCheck.Value = value;
	}

	/// <summary>
	/// Maximum number of finished candles to keep a position opened.
	/// </summary>
	public int BarsToHold
	{
		get => _barsToHold.Value;
		set => _barsToHold.Value = value;
	}

	/// <summary>
	/// Required price displacement in MetaTrader points.
	/// </summary>
	public int MinMovePoints
	{
		get => _minMovePoints.Value;
		set => _minMovePoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Defines whether the strategy follows or fades the detected momentum swing.
	/// </summary>
	public NextbarDirection Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
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

		_closeHistory.Clear();
		_barsSinceEntry = 0;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest close so we can compare it with older values.
		_closeHistory.Add(candle.ClosePrice);

		var maxHistory = Math.Max(BarsToCheck + BarsToHold + 5, BarsToCheck + 2);
		if (_closeHistory.Count > maxHistory)
			_closeHistory.RemoveRange(0, _closeHistory.Count - maxHistory);

		var hadPosition = Position != 0;

		if (hadPosition)
		{
			_barsSinceEntry++;
			var closed = CheckExit(candle);

			if (closed)
				return;
		}

		if (!hadPosition)
			TryEnter(candle);
	}

	private void TryEnter(ICandleMessage candle)
	{
		var lookbackClose = GetCloseOffset(BarsToCheck);
		if (!lookbackClose.HasValue)
			return;

		var priceStep = GetPointSize();
		if (priceStep <= 0m)
		{
			LogWarning("PriceStep is not configured. Entry rules cannot be evaluated.");
			return;
		}

		var minDistance = MinMovePoints * priceStep;
		if (minDistance <= 0m)
			return;

		var currentClose = candle.ClosePrice;
		var pastClose = lookbackClose.Value;

		var bullishMove = currentClose - pastClose >= minDistance;
		var bearishMove = pastClose - currentClose >= minDistance;

		if (!bullishMove && !bearishMove)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Volume;
		if (volume <= 0m)
		{
			LogWarning("Volume must be positive to place orders.");
			return;
		}

		var direction = Direction;

		if ((direction == NextbarDirection.Follow && bullishMove) ||
			(direction == NextbarDirection.Reverse && bearishMove))
		{
			BuyMarket(volume);
			_entryPrice = currentClose;
			_barsSinceEntry = 0;
		}
		else if ((direction == NextbarDirection.Follow && bearishMove) ||
			(direction == NextbarDirection.Reverse && bullishMove))
		{
			SellMarket(volume);
			_entryPrice = currentClose;
			_barsSinceEntry = 0;
		}
	}

	private bool CheckExit(ICandleMessage candle)
	{
		if (Position == 0)
			return false;

		var entryPrice = _entryPrice;
		if (!entryPrice.HasValue)
		{
			// Without a stored entry price we cannot emulate the original thresholds.
			return false;
		}

		var priceStep = GetPointSize();
		if (priceStep <= 0m)
		{
			LogWarning("PriceStep is not configured. Exit rules cannot be applied.");
			return false;
		}

		var takeProfitDistance = TakeProfitPoints * priceStep;
		var stopLossDistance = StopLossPoints * priceStep;

		var currentClose = candle.ClosePrice;
		var entry = entryPrice.Value;
		var shouldClose = false;

		if (Position > 0)
		{
			if (currentClose - entry >= takeProfitDistance)
				shouldClose = true;
			else if (entry - currentClose >= stopLossDistance)
				shouldClose = true;
		}
		else if (Position < 0)
		{
			if (currentClose - entry >= stopLossDistance)
				shouldClose = true;
			else if (entry - currentClose >= takeProfitDistance)
				shouldClose = true;
		}

		if (!shouldClose && _barsSinceEntry >= BarsToHold)
			shouldClose = true;

		if (!shouldClose)
			return false;

		ClosePosition();
		_entryPrice = null;
		_barsSinceEntry = 0;
		return true;
	}

	private decimal? GetCloseOffset(int offset)
	{
		var index = _closeHistory.Count - 1 - offset;
		if (index < 0 || index >= _closeHistory.Count)
			return null;

		return _closeHistory[index];
	}

	private decimal GetPointSize()
	{
		return Security?.PriceStep ?? 0m;
	}
}

