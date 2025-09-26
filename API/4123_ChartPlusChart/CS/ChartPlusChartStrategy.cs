using System;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cross-chart data bridge that mirrors the MetaTrader "ChartPlusChart" expert by broadcasting shared portfolio metrics.
/// </summary>
public class ChartPlusChartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _secondaryCandleType;
	private readonly StrategyParam<bool> _useSecondaryStream;

	private ChartSnapshot _primarySnapshot;
	private ChartSnapshot _secondarySnapshot;
	private bool _hasPrimaryClose;
	private bool _hasSecondaryClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChartPlusChartStrategy"/> class.
	/// </summary>
	public ChartPlusChartStrategy()
	{
		_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Primary Candle Type", "Timeframe tracked by the first chart stream", "Data")
			.SetCanOptimize(true);

		_secondaryCandleType = Param(nameof(SecondaryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Secondary Candle Type", "Timeframe tracked by the optional second chart stream", "Data")
			.SetCanOptimize(true);

		_useSecondaryStream = Param(nameof(UseSecondaryStream), true)
			.SetDisplay("Use Secondary Stream", "Enable tracking of the second chart stream", "Data");
	}

	/// <summary>
	/// Gets or sets the candle type processed by the primary stream.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type processed by the secondary stream.
	/// </summary>
	public DataType SecondaryCandleType
	{
		get => _secondaryCandleType.Value;
		set => _secondaryCandleType.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the secondary stream is active.
	/// </summary>
	public bool UseSecondaryStream
	{
		get => _useSecondaryStream.Value;
		set => _useSecondaryStream.Value = value;
	}

	/// <summary>
	/// Gets the latest snapshot published by the primary stream.
	/// </summary>
	public ChartSnapshot PrimarySnapshot => _primarySnapshot;

	/// <summary>
	/// Gets the latest snapshot published by the secondary stream.
	/// </summary>
	public ChartSnapshot SecondarySnapshot => _secondarySnapshot;

	/// <summary>
	/// Fires whenever one of the chart streams refreshes its snapshot.
	/// </summary>
	public event Action<ChartStream, ChartSnapshot> SnapshotUpdated;

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_hasPrimaryClose = false;
		_hasSecondaryClose = false;

		var primarySubscription = SubscribeCandles(PrimaryCandleType);
		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		if (UseSecondaryStream)
		{
			var secondarySubscription = SubscribeCandles(SecondaryCandleType);
			secondarySubscription
				.Bind(ProcessSecondaryCandle)
				.Start();
		}

		RefreshSnapshots();
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal closePrice)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSnapshot(ChartStream.Primary, closePrice);
	}

	private void ProcessSecondaryCandle(ICandleMessage candle, decimal closePrice)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSnapshot(ChartStream.Secondary, closePrice);
	}

	private void UpdateSnapshot(ChartStream stream, decimal closePrice)
	{
		var activeOrders = ActiveOrders.Count;
		var balance = Portfolio?.CurrentValue ?? 0m;
		var profit = Portfolio?.CurrentProfit ?? 0m;

		var snapshot = new ChartSnapshot
		{
			LastClose = closePrice,
			ActiveOrders = activeOrders,
			AccountBalance = balance,
			TotalProfit = profit
		};

		if (stream == ChartStream.Primary)
		{
			_primarySnapshot = snapshot;
			_hasPrimaryClose = true;
		}
		else
		{
			_secondarySnapshot = snapshot;
			_hasSecondaryClose = true;
		}

		SnapshotUpdated?.Invoke(stream, snapshot);
	}

	private void RefreshSnapshots()
	{
		var activeOrders = ActiveOrders.Count;
		var balance = Portfolio?.CurrentValue ?? 0m;
		var profit = Portfolio?.CurrentProfit ?? 0m;

		if (_hasPrimaryClose)
		{
			_primarySnapshot = _primarySnapshot with
			{
				ActiveOrders = activeOrders,
				AccountBalance = balance,
				TotalProfit = profit
			};

			SnapshotUpdated?.Invoke(ChartStream.Primary, _primarySnapshot);
		}

		if (UseSecondaryStream && _hasSecondaryClose)
		{
			_secondarySnapshot = _secondarySnapshot with
			{
				ActiveOrders = activeOrders,
				AccountBalance = balance,
				TotalProfit = profit
			};

			SnapshotUpdated?.Invoke(ChartStream.Secondary, _secondarySnapshot);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderRegistered(Order order)
	{
		base.OnOrderRegistered(order);

		RefreshSnapshots();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		RefreshSnapshots();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		RefreshSnapshots();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		RefreshSnapshots();
	}

	/// <summary>
	/// Retrieves the most recent snapshot of the requested stream.
	/// </summary>
	/// <param name="stream">Stream selector.</param>
	/// <returns>The last known snapshot, or the default struct when no data has been received yet.</returns>
	public ChartSnapshot GetSnapshot(ChartStream stream)
	{
		return stream switch
		{
			ChartStream.Primary => _primarySnapshot,
			ChartStream.Secondary => _secondarySnapshot,
			_ => default
		};
	}

	/// <summary>
	/// Identifies which chart stream raised a snapshot update.
	/// </summary>
	public enum ChartStream
	{
		/// <summary>
		/// Snapshot coming from the first chart stream.
		/// </summary>
		Primary,

		/// <summary>
		/// Snapshot coming from the optional second chart stream.
		/// </summary>
		Secondary
	}

	/// <summary>
	/// Data container mirroring the values shared by the original expert advisor.
	/// </summary>
	public record struct ChartSnapshot
	{
		/// <summary>
		/// Gets the last finished candle close for the associated stream.
		/// </summary>
		public decimal LastClose { get; init; }

		/// <summary>
		/// Gets the number of currently active orders.
		/// </summary>
		public int ActiveOrders { get; init; }

		/// <summary>
		/// Gets the latest account balance reported by the portfolio.
		/// </summary>
		public decimal AccountBalance { get; init; }

		/// <summary>
		/// Gets the total profit as exposed by the connected portfolio.
		/// </summary>
		public decimal TotalProfit { get; init; }
	}
}
