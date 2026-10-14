namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Opens one long momentum trade per New York day, sizes it by candle body,
/// and exits on a red candle or non-increasing candle volume.
/// </summary>
public class PremarketGapMomoTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minGainPct;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<bool> _useSession;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<DataType> _candleType;
	private decimal _previousClose;
	private decimal _previousVolume;
	private bool _isInitialized;
	private DateTime? _lastEntryDate;
	private Order _pendingOrder;

	/// <summary>Initializes gain 5%, minimum candle volume 15000 and the 04:00–09:30 New York entry session.</summary>
	public PremarketGapMomoTraderStrategy()
	{
		_minGainPct = Param(nameof(MinGainPct), 5m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("Minimum Gain Percent", "Inclusive close gain over the preceding finished candle", "Entry");
		_minVolume = Param(nameof(MinVolume), 15000m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("Minimum Candle Volume", "Strict lower bound for candle volume", "Entry");
		_useSession = Param("UseSession", true)
			.SetDisplay("Use Session", "Restrict entries to the New York premarket session", "Session");
		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(4))
			.SetDisplay("Session Start", "Inclusive New York entry time", "Session");
		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromMinutes(570))
			.SetDisplay("Session End", "Exclusive New York entry time", "Session");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Finished candles supplying price, body and volume", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <summary>Inclusive minimum close-to-previous-close gain in percent.</summary>
	public decimal MinGainPct { get => _minGainPct.Value; set => _minGainPct.Value = value; }

	/// <summary>Strict minimum finished-candle volume.</summary>
	public decimal MinVolume { get => _minVolume.Value; set => _minVolume.Value = value; }

	/// <summary>Whether entry times are restricted; the published parameter key is UseSession.</summary>
	public bool IsUseSession { get => _useSession.Value; set => _useSession.Value = value; }

	/// <summary>Inclusive New York entry time, before SessionEnd.</summary>
	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }

	/// <summary>Exclusive New York entry time within the same day.</summary>
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }

	/// <summary>Finished candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	private void ResetState()
	{
		_previousClose = _previousVolume = 0m;
		_isInitialized = false;
		_lastEntryDate = null;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (MinGainPct < 0m || MinVolume < 0m || SessionStart < TimeSpan.Zero || SessionEnd > TimeSpan.FromDays(1)
			|| SessionStart >= SessionEnd)
			throw new ArgumentException("Gain and volume thresholds cannot be negative; session times must be ordered within one New York day.");
		ResetState();
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void Submit(Sides side, decimal volume)
		=> RegisterOrder(new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = volume,
			Comment = side == Sides.Buy ? "Premarket body momentum" : "Premarket momentum exit",
		});

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var previousClose = _previousClose;
		var previousVolume = _previousVolume;
		_previousClose = candle.ClosePrice;
		_previousVolume = candle.TotalVolume;
		if (!_isInitialized)
		{
			_isInitialized = true;
			return;
		}
		if (!IsFormedAndOnlineAndAllowTrading()
			|| _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m)
		{
			if (candle.ClosePrice < candle.OpenPrice || candle.TotalVolume <= previousVolume)
				Submit(Sides.Sell, Position);
			return;
		}
		if (Position != 0m || previousClose <= 0m || candle.ClosePrice <= candle.OpenPrice
			|| candle.TotalVolume <= MinVolume || candle.HighPrice <= candle.LowPrice
			|| 100m * (candle.ClosePrice - previousClose) < MinGainPct * previousClose)
			return;
		var bodyPercent = 100m * (candle.ClosePrice - candle.OpenPrice) / (candle.HighPrice - candle.LowPrice);
		var fraction = bodyPercent >= 90m ? 1m : bodyPercent >= 85m ? 0.5m : bodyPercent >= 75m ? 0.25m : 0m;
		if (fraction == 0m)
			return;
		var local = TimeZoneInfo.ConvertTimeFromUtc(CurrentTime, TimeHelper.Est);
		if (_lastEntryDate == local.Date || IsUseSession && (local.TimeOfDay < SessionStart || local.TimeOfDay >= SessionEnd))
			return;
		var volume = Volume * fraction;
		if (Security.VolumeStep is decimal step && step > 0m)
			volume = volume.Floor(step);
		if (volume <= 0m || Security.MinVolume is decimal min && volume < min
			|| Security.MaxVolume is decimal max && max > 0m && volume > max)
			return;
		_lastEntryDate = local.Date;
		Submit(Sides.Buy, volume);
	}
}
