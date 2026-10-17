namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Trades large, direction-classified off-exchange prints from an explicitly selected source.</summary>
public class DarkPoolPrintsStrategy : Strategy
{
	private readonly StrategyParam<Security> _darkPoolSecurity;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _volumeLookback;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly Queue<decimal> _reportVolumes = new();
	private readonly HashSet<(long? id, string text)> _reportIds = new();
	private DateTime _reportTime;
	private decimal? _average;
	private int _cooldown;
	private Order _pendingOrder;

	/// <summary>Dedicated off-exchange report instrument; it must differ from the execution instrument.</summary>
	public Security DarkPoolSecurity { get => _darkPoolSecurity.Value; set => _darkPoolSecurity.Value = value; }
	/// <summary>Trend and exit SMA length.</summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	/// <summary>Number of preceding reported trades in the volume baseline.</summary>
	public int VolumeLookback { get => _volumeLookback.Value; set => _volumeLookback.Value = value; }
	/// <summary>Required multiple of the preceding report volumes.</summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
	/// <summary>Execution instrument's candle interval.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	/// <summary>Finished candles between entries; zero allows each qualifying report.</summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	/// <summary>Native percentage loss limit.</summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>Initializes the dark-pool strategy.</summary>
	public DarkPoolPrintsStrategy()
	{
		_darkPoolSecurity = Param<Security>(nameof(DarkPoolSecurity)).SetRequired()
			.SetDisplay("Dark Pool Security", "Dedicated source of off-exchange trade reports", "Data");
		_maPeriod = Param(nameof(MaPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Execution instrument SMA period", "Indicators");
		_volumeLookback = Param(nameof(VolumeLookback), 20).SetGreaterThanZero()
			.SetDisplay("Volume Lookback", "Preceding off-exchange reports for the volume baseline", "Volume");
		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m).SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Required report-volume multiple", "Volume");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Execution instrument candle interval", "Data");
		_cooldownBars = Param(nameof(CooldownBars), 0).SetNotNegative()
			.SetDisplay("Cooldown Bars", "Finished candles between qualifying reports", "Trading");
		_stopLoss = Param(nameof(StopLoss), 2m).SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Percentage loss limit from the actual entry fill", "Risk");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1), (DarkPoolSecurity, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ClearRuntime();
	}

	private void ClearRuntime()
	{
		_reportVolumes.Clear();
		_reportIds.Clear();
		_reportTime = default;
		_average = null;
		_cooldown = 0;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (DarkPoolSecurity is null || Security is null
			|| string.Equals(DarkPoolSecurity.Id, Security.Id, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("DarkPoolSecurity must explicitly identify a separate off-exchange report source.");
		base.OnStarted2(time);
		ClearRuntime();
		StartProtection(new Unit(), new Unit(StopLoss, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Start();
		}
		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var candles = SubscribeCandles(CandleType);
		candles.Bind(sma, ProcessCandle).Start();
		SubscribeTicks(DarkPoolSecurity).Bind(ProcessReport).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private bool IsOrderPending => _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessCandle(ICandleMessage candle, decimal average)
	{
		if (candle.State != CandleStates.Finished)
			return;
		_average = average;
		if (_cooldown > 0)
			_cooldown--;
		if (!IsFormedAndOnlineAndAllowTrading() || IsOrderPending)
			return;
		if (Position > 0m && candle.ClosePrice < average)
			SellMarket(Position);
		else if (Position < 0m && candle.ClosePrice > average)
			BuyMarket(-Position);
	}

	private void ProcessReport(ITickTradeMessage report)
	{
		if (report.IsSystem != false || report.Price <= 0m || report.Volume <= 0m
			|| report.ServerTime < _reportTime || report.Id is null && string.IsNullOrEmpty(report.StringId))
			return;
		if (report.ServerTime > _reportTime)
		{
			_reportTime = report.ServerTime;
			_reportIds.Clear();
		}
		if (!_reportIds.Add((report.Id, report.StringId)))
			return;

		var baseline = _reportVolumes.Count == VolumeLookback ? _reportVolumes.Average() : (decimal?)null;
		_reportVolumes.Enqueue(report.Volume);
		while (_reportVolumes.Count > VolumeLookback)
			_reportVolumes.Dequeue();
		if (baseline is null || report.Volume <= baseline.Value * VolumeMultiplier
			|| _average is not decimal average || _cooldown > 0 || IsOrderPending
			|| !IsFormedAndOnlineAndAllowTrading())
			return;
		if (report.OriginSide == Sides.Buy && report.Price > average && Position <= 0m)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (report.OriginSide == Sides.Sell && report.Price < average && Position >= 0m)
		{
			SellMarket(Volume + Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}