using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bykov Trend: armed, two-threshold Williams %R trend transitions.
/// The upper crossing is valid only after the lower extreme has been observed;
/// the lower crossing is valid only after the upper extreme has been observed.
/// </summary>
public class BykovTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _risk;
	private readonly StrategyParam<int> _ssp;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousWpr;
	private bool _longArmed;
	private bool _shortArmed;
	private Order _pendingOrder;

	public int Risk { get => _risk.Value; set => _risk.Value = value; }
	/// <summary>Williams %R look-back, matching the published SSP parameter.</summary>
	public int SSP { get => _ssp.Value; set => _ssp.Value = value; }
	/// <summary>Backward-compatible property alias for earlier local examples.</summary>
	public int Ssp { get => SSP; set => SSP = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BykovTrendStrategy()
	{
		_risk = Param(nameof(Risk), 3).SetGreaterThanZero()
			.SetDisplay("Risk", "K=33-Risk threshold parameter", "Indicator");
		_ssp = Param(nameof(SSP), 9).SetGreaterThanZero()
			.SetDisplay("SSP", "Williams %R look-back", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	private void ResetSignals()
	{
		_previousWpr = null;
		_longArmed = _shortArmed = false;
		_pendingOrder = null;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetSignals();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (Risk < 1 || Risk >= 33)
			throw new InvalidOperationException("Bykov Trend Risk must lie from 1 to 32 to preserve two distinct thresholds.");
		ResetSignals();
		var wpr = new WilliamsR { Length = SSP };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wpr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wpr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var k = 33m - Risk;
		var lower = -100m + k;
		var upper = -k;
		if (wpr <= lower)
			_longArmed = true;
		if (wpr >= upper)
			_shortArmed = true;

		var longSignal = _previousWpr is decimal prevLong && _longArmed
			&& prevLong <= upper && wpr > upper;
		var shortSignal = _previousWpr is decimal prevShort && _shortArmed
			&& prevShort >= lower && wpr < lower;
		_previousWpr = wpr;
		if (longSignal)
			_longArmed = false;
		if (shortSignal)
			_shortArmed = false;

		if (!IsFormedAndOnlineAndAllowTrading() ||
			(_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (longSignal && Position <= 0m)
			_pendingOrder = BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0m)
			_pendingOrder = SellMarket(Volume + Math.Abs(Position));
	}
}
