using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI strategy that trades only inside a selected session.
/// </summary>
public class TutorialAddingSessionsToStrategiesStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _upper;
	private readonly StrategyParam<decimal> _lower;
	private readonly StrategyParam<string> _session;
	private readonly StrategyParam<DataType> _candleType;

	private TimeSpan _sessionStart;
	private TimeSpan _sessionEnd;
	private decimal? _prevRsi;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI level.
	/// </summary>
	public decimal Upper
	{
		get => _upper.Value;
		set => _upper.Value = value;
	}

	/// <summary>
	/// Lower RSI level.
	/// </summary>
	public decimal Lower
	{
		get => _lower.Value;
		set => _lower.Value = value;
	}

	/// <summary>
	/// Active trading session.
	/// </summary>
	public string Session
	{
		get => _session.Value;
		set => _session.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TutorialAddingSessionsToStrategiesStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_upper = Param(nameof(Upper), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("Upper Level", "Overbought threshold", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_lower = Param(nameof(Lower), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("Lower Level", "Oversold threshold", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_session = Param(nameof(Session), "1300-1700")
			.SetDisplay("Session", "Active trading session", "General")
			.SetOptions("0930-1600", "1300-1700", "1700-2100");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var parts = Session.Split('-');
		if (parts.Length != 2 ||
			!TimeSpan.TryParseExact(parts[0], "hhmm", CultureInfo.InvariantCulture, out _sessionStart) ||
			!TimeSpan.TryParseExact(parts[1], "hhmm", CultureInfo.InvariantCulture, out _sessionEnd))
			throw new InvalidOperationException("Invalid session format.");

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevRsi is null)
		{
			_prevRsi = rsi;
			return;
		}

		var inSession = IsInSession(candle.OpenTime);

		if (!inSession)
		{
			if (Position != 0)
				CloseAll("Out of Session");

			_prevRsi = rsi;
			return;
		}

		if (_prevRsi <= Lower && rsi > Lower && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (_prevRsi >= Upper && rsi < Upper && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevRsi = rsi;
	}

	private bool IsInSession(DateTimeOffset time)
	{
		var tod = time.TimeOfDay;
		return tod >= _sessionStart && tod <= _sessionEnd;
	}
}
