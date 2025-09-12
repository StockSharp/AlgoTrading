using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opening range breakout strategy using SPY correlation and optional Heikin Ashi.
/// </summary>
public class OrbHeikinAshiSpyCorrelationStrategy : Strategy
{
	private readonly StrategyParam<int> _rvolPeriod;
	private readonly StrategyParam<TimeSpan> _orbStart;
	private readonly StrategyParam<TimeSpan> _orbEnd;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;
	private readonly StrategyParam<TimeSpan> _exitStart;
	private readonly StrategyParam<TimeSpan> _exitEnd;
	private readonly StrategyParam<bool> _useHeikin;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma = null!;
	private decimal? _orbHigh;
	private decimal? _orbLow;
	private bool _inOrbSession;
	private decimal _prevHaOpen;
	private decimal _prevHaClose;

	/// <summary>
	/// Correlation security, e.g., SPY.
	/// </summary>
	public Security CorrelationSecurity { get; set; } = null!;

	public int RelativeVolumePeriod { get => _rvolPeriod.Value; set => _rvolPeriod.Value = value; }
	public TimeSpan OrbSessionStart { get => _orbStart.Value; set => _orbStart.Value = value; }
	public TimeSpan OrbSessionEnd { get => _orbEnd.Value; set => _orbEnd.Value = value; }
	public TimeSpan TradingSessionStart { get => _tradeStart.Value; set => _tradeStart.Value = value; }
	public TimeSpan TradingSessionEnd { get => _tradeEnd.Value; set => _tradeEnd.Value = value; }
	public TimeSpan ExitSessionStart { get => _exitStart.Value; set => _exitStart.Value = value; }
	public TimeSpan ExitSessionEnd { get => _exitEnd.Value; set => _exitEnd.Value = value; }
	public bool UseHeikinAshi { get => _useHeikin.Value; set => _useHeikin.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrbHeikinAshiSpyCorrelationStrategy()
	{
		_rvolPeriod = Param(nameof(RelativeVolumePeriod), 3);
		_orbStart = Param(nameof(OrbSessionStart), new TimeSpan(9, 30, 0));
		_orbEnd = Param(nameof(OrbSessionEnd), new TimeSpan(10, 0, 0));
		_tradeStart = Param(nameof(TradingSessionStart), new TimeSpan(10, 0, 0));
		_tradeEnd = Param(nameof(TradingSessionEnd), new TimeSpan(12, 0, 0));
		_exitStart = Param(nameof(ExitSessionStart), new TimeSpan(15, 50, 0));
		_exitEnd = Param(nameof(ExitSessionEnd), new TimeSpan(15, 55, 0));
		_useHeikin = Param(nameof(UseHeikinAshi), false);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		if (CorrelationSecurity != null)
			yield return (CorrelationSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_orbHigh = null;
		_orbLow = null;
		_inOrbSession = false;
		_prevHaOpen = 0m;
		_prevHaClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (CorrelationSecurity == null)
			throw new InvalidOperationException("CorrelationSecurity is not set.");

		_volumeSma = new SimpleMovingAverage { Length = RelativeVolumePeriod };

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(c => c.Volume, _volumeSma, ProcessMainCandle).Start();

		var corrSub = SubscribeCandles(CandleType, security: CorrelationSecurity);
		corrSub.Bind(ProcessCorrelationCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCorrelationCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime.TimeOfDay;
		var inOrb = time >= OrbSessionStart && time < OrbSessionEnd;

		decimal high;
		decimal low;

		if (UseHeikinAshi)
		{
			var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
			var haOpen = _prevHaOpen == 0m ? (candle.OpenPrice + candle.ClosePrice) / 2m : (_prevHaOpen + _prevHaClose) / 2m;
			high = Math.Max(Math.Max(haOpen, haClose), candle.HighPrice);
			low = Math.Min(Math.Min(haOpen, haClose), candle.LowPrice);
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
		}
		else
		{
			high = candle.HighPrice;
			low = candle.LowPrice;
		}

		if (inOrb)
		{
			_orbHigh = _orbHigh.HasValue ? Math.Max(_orbHigh.Value, high) : high;
			_orbLow = _orbLow.HasValue ? Math.Min(_orbLow.Value, low) : low;
		}

		_inOrbSession = inOrb;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal avgVolume)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime.TimeOfDay;

		if (time >= ExitSessionStart && time < ExitSessionEnd)
		{
			if (Position != 0)
				ClosePosition();
			return;
		}

		var inTrading = time >= TradingSessionStart && time < TradingSessionEnd;

		if (!inTrading || !_orbHigh.HasValue || !_orbLow.HasValue)
			return;

		if (!_volumeSma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var relVol = avgVolume == 0m ? 0m : candle.Volume / avgVolume;

		if (relVol <= 1m)
			return;

		if (candle.ClosePrice > _orbHigh && Position <= 0)
		{
			BuyMarket();
		}
		else if (candle.ClosePrice < _orbLow && Position >= 0)
		{
			SellMarket();
		}
	}
}
