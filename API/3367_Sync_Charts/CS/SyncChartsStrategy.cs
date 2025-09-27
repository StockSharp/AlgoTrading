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
/// Synchronizes two candle streams of the same security and trades when both agree on trend direction.
/// </summary>
public class SyncChartsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _masterCandleType;
	private readonly StrategyParam<DataType> _followerCandleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<TimeSpan> _syncTolerance;

	private decimal _previousMasterSma;
	private decimal _previousFollowerSma;
	private bool _masterInitialized;
	private bool _followerInitialized;
	private bool? _masterTrendUp;
	private bool? _followerTrendUp;
	private DateTimeOffset? _masterLastTime;
	private DateTimeOffset? _followerLastTime;
	private decimal _masterLastClose;
	private decimal _followerLastClose;
	private int _lastTradeDirection;

	/// <summary>
	/// The candle type representing the master chart that leads synchronization.
	/// </summary>
	public DataType MasterCandleType
	{
		get => _masterCandleType.Value;
		set => _masterCandleType.Value = value;
	}

	/// <summary>
	/// The candle type representing the follower chart that is synchronized with the master.
	/// </summary>
	public DataType FollowerCandleType
	{
		get => _followerCandleType.Value;
		set => _followerCandleType.Value = value;
	}

	/// <summary>
	/// The SMA period used to determine the directional bias on both charts.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Maximum allowed time difference between the latest candles of the charts before flattening.
	/// </summary>
	public TimeSpan SyncTolerance
	{
		get => _syncTolerance.Value;
		set => _syncTolerance.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SyncChartsStrategy"/> class.
	/// </summary>
	public SyncChartsStrategy()
	{
		_masterCandleType = Param(nameof(MasterCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Master Candle", "Primary candle series used as master", "General");

		_followerCandleType = Param(nameof(FollowerCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Follower Candle", "Secondary candle series following the master", "General");

		_smaLength = Param(nameof(SmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period of simple moving averages", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 10);

		_syncTolerance = Param(nameof(SyncTolerance), TimeSpan.FromSeconds(15))
			.SetGreaterThan(TimeSpan.Zero)
			.SetDisplay("Sync Tolerance", "Maximum difference between master and follower candles", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(5));
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, MasterCandleType);
		yield return (Security, FollowerCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousMasterSma = 0m;
		_previousFollowerSma = 0m;
		_masterInitialized = false;
		_followerInitialized = false;
		_masterTrendUp = null;
		_followerTrendUp = null;
		_masterLastTime = null;
		_followerLastTime = null;
		_masterLastClose = 0m;
		_followerLastClose = 0m;
		_lastTradeDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var masterSma = new SMA { Length = SmaLength };
		var followerSma = new SMA { Length = SmaLength };

		var masterSubscription = SubscribeCandles(MasterCandleType);
		masterSubscription
			.Bind(masterSma, OnMasterCandle)
			.Start();

		var followerSubscription = SubscribeCandles(FollowerCandleType);
		followerSubscription
			.Bind(followerSma, OnFollowerCandle)
			.Start();

		var masterArea = CreateChartArea("Master");
		if (masterArea != null)
		{
			DrawCandles(masterArea, masterSubscription);
			DrawIndicator(masterArea, masterSma);
			DrawOwnTrades(masterArea);
		}

		var followerArea = CreateChartArea("Follower");
		if (followerArea != null)
		{
			DrawCandles(followerArea, followerSubscription);
			DrawIndicator(followerArea, followerSma);
		}
	}

	private void OnMasterCandle(ICandleMessage candle, decimal masterSmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_masterLastTime = candle.OpenTime;
		_masterLastClose = candle.ClosePrice;

		if (!_masterInitialized)
		{
			_previousMasterSma = masterSmaValue;
			_masterInitialized = true;
			LogInfo($"Master chart initialized at {candle.OpenTime:O} with SMA {masterSmaValue:0.#####}.");
			return;
		}

		_masterTrendUp = masterSmaValue >= _previousMasterSma;
		_previousMasterSma = masterSmaValue;

		LogInfo($"Master candle {candle.OpenTime:O} close {candle.ClosePrice:0.#####}, SMA {masterSmaValue:0.#####}.");

		TrySyncTrade();
	}

	private void OnFollowerCandle(ICandleMessage candle, decimal followerSmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_followerLastTime = candle.OpenTime;
		_followerLastClose = candle.ClosePrice;

		if (!_followerInitialized)
		{
			_previousFollowerSma = followerSmaValue;
			_followerInitialized = true;
			LogInfo($"Follower chart initialized at {candle.OpenTime:O} with SMA {followerSmaValue:0.#####}.");
			return;
		}

		_followerTrendUp = followerSmaValue >= _previousFollowerSma;
		_previousFollowerSma = followerSmaValue;

		LogInfo($"Follower candle {candle.OpenTime:O} close {candle.ClosePrice:0.#####}, SMA {followerSmaValue:0.#####}.");

		TrySyncTrade();
	}

	private void TrySyncTrade()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_masterTrendUp.HasValue || !_followerTrendUp.HasValue)
			return;

		if (_masterLastTime is null || _followerLastTime is null)
			return;

		var delta = (_masterLastTime.Value - _followerLastTime.Value).Duration();
		if (delta > SyncTolerance)
		{
			if (Position != 0)
			{
				LogInfo($"Charts out of sync by {delta.TotalSeconds:0.##} seconds. Flattening position.");
				ClosePosition();
				_lastTradeDirection = 0;
			}
			return;
		}

		if (_masterTrendUp.Value && _followerTrendUp.Value)
		{
			EnterLong();
			return;
		}

		if (!_masterTrendUp.Value && !_followerTrendUp.Value)
		{
			EnterShort();
			return;
		}

		if (_lastTradeDirection != 0)
		{
			LogInfo("Trend disagreement detected. Closing existing position.");
			ClosePosition();
			_lastTradeDirection = 0;
		}
	}

	private void EnterLong()
	{
		if (_lastTradeDirection == 1)
			return;

		var volume = Volume + Math.Abs(Position);
		if (volume <= 0)
			return;

		BuyMarket(volume);
		_lastTradeDirection = 1;
		LogInfo($"Entering long because both charts point up. Master close {_masterLastClose:0.#####}, follower close {_followerLastClose:0.#####}.");
	}

	private void EnterShort()
	{
		if (_lastTradeDirection == -1)
			return;

		var volume = Volume + Math.Abs(Position);
		if (volume <= 0)
			return;

		SellMarket(volume);
		_lastTradeDirection = -1;
		LogInfo($"Entering short because both charts point down. Master close {_masterLastClose:0.#####}, follower close {_followerLastClose:0.#####}.");
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
			LogInfo("Closing long position to restore synchronization.");
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
			LogInfo("Closing short position to restore synchronization.");
		}
	}
}

