using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that holds the primary equity instrument when the benchmark crude-oil proxy shows positive momentum and exits when the signal weakens.
/// </summary>
public class CrudeOilPredictsEquityStrategy : Strategy
{
	private readonly StrategyParam<string> _oilSecurityId;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<decimal> _oilThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _oilSecurity = null!;
	private RateOfChange _oilMomentum = null!;
	private SimpleMovingAverage _equityTrend = null!;
	private decimal _latestEquityPrice;
	private decimal _latestEquityTrend;
	private decimal _latestOilMomentum;
	private bool _equityUpdated;
	private bool _oilUpdated;
	private int _cooldownRemaining;

	/// <summary>
	/// Crude oil benchmark identifier.
	/// </summary>
	public string OilSecurityId
	{
		get => _oilSecurityId.Value;
		set => _oilSecurityId.Value = value;
	}

	/// <summary>
	/// Number of candles used to compute oil momentum.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Equity trend filter length.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Minimum oil momentum required to hold equity exposure.
	/// </summary>
	public decimal OilThreshold
	{
		get => _oilThreshold.Value;
		set => _oilThreshold.Value = value;
	}

	/// <summary>
	/// Closed candles to wait before another position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type used for both instruments.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CrudeOilPredictsEquityStrategy()
	{
		_oilSecurityId = Param(nameof(OilSecurityId), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Oil Security Id", "Identifier of the crude-oil benchmark security", "General");

		_lookback = Param(nameof(Lookback), 20)
			.SetRange(5, 120)
			.SetDisplay("Lookback", "Number of candles used to compute oil momentum", "Indicators");

		_trendLength = Param(nameof(TrendLength), 20)
			.SetRange(5, 120)
			.SetDisplay("Trend Length", "Equity trend filter length", "Indicators");

		_oilThreshold = Param(nameof(OilThreshold), 0m)
			.SetRange(-20m, 20m)
			.SetDisplay("Oil Threshold", "Minimum oil momentum required to hold equity exposure", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetRange(0, 100)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2.5m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for both instruments", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (!OilSecurityId.IsEmpty())
			yield return (new Security { Id = OilSecurityId }, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_oilSecurity = null!;
		_oilMomentum = null!;
		_equityTrend = null!;
		_latestEquityPrice = 0m;
		_latestEquityTrend = 0m;
		_latestOilMomentum = 0m;
		_equityUpdated = false;
		_oilUpdated = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary equity security is not specified.");

		if (OilSecurityId.IsEmpty())
			throw new InvalidOperationException("Oil security identifier is not specified.");

		_oilSecurity = this.LookupById(OilSecurityId) ?? new Security { Id = OilSecurityId };
		_oilMomentum = new RateOfChange { Length = Lookback };
		_equityTrend = new SimpleMovingAverage { Length = TrendLength };

		var equitySubscription = SubscribeCandles(CandleType, security: Security);
		var oilSubscription = SubscribeCandles(CandleType, security: _oilSecurity);

		equitySubscription
			.Bind(ProcessEquityCandle)
			.Start();

		oilSubscription
			.Bind(ProcessOilCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, equitySubscription);
			DrawCandles(area, oilSubscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent));
	}

	private void ProcessEquityCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestEquityPrice = candle.ClosePrice;
		_latestEquityTrend = _equityTrend.Process(candle).ToDecimal();
		_equityUpdated = _equityTrend.IsFormed;
		TryProcessSignal();
	}

	private void ProcessOilCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var oilValue = _oilMomentum.Process(candle);
		if (!oilValue.IsEmpty && _oilMomentum.IsFormed)
		{
			_latestOilMomentum = oilValue.ToDecimal();
			_oilUpdated = true;
			TryProcessSignal();
		}
	}

	private void TryProcessSignal()
	{
		if (!_equityUpdated || !_oilUpdated)
			return;

		_equityUpdated = false;
		_oilUpdated = false;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var bullishSignal = _latestOilMomentum > OilThreshold && _latestEquityPrice >= _latestEquityTrend;
		var exitSignal = _latestOilMomentum <= OilThreshold || _latestEquityPrice < _latestEquityTrend;

		if (_cooldownRemaining == 0 && Position == 0 && bullishSignal)
		{
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (Position > 0 && exitSignal)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
	}
}
