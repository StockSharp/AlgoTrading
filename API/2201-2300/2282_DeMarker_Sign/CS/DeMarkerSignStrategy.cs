using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on DeMarker oscillator crossing predefined levels.
/// </summary>
public class DeMarkerSignStrategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevDeMarker;

	public int DeMarkerPeriod { get => _deMarkerPeriod.Value; set => _deMarkerPeriod.Value = value; }
	public decimal UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public decimal DownLevel { get => _downLevel.Value; set => _downLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DeMarkerSignStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
			.SetDisplay("DeMarker Period", "Indicator period", "General");

		_upLevel = Param(nameof(UpLevel), 0.7m)
			.SetDisplay("Upper Level", "Sell when DeMarker falls below", "General");

		_downLevel = Param(nameof(DownLevel), 0.3m)
			.SetDisplay("Lower Level", "Buy when DeMarker rises above", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_prevDeMarker = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevDeMarker = null;

		var deMarker = new DeMarker { Length = DeMarkerPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(deMarker, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deMarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarker)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevDeMarker is null)
		{
			_prevDeMarker = deMarker;
			return;
		}

		// DeMarker crosses above lower level -> buy
		if (deMarker > DownLevel && _prevDeMarker <= DownLevel && Position <= 0)
			BuyMarket();
		// DeMarker crosses below upper level -> sell
		else if (deMarker < UpLevel && _prevDeMarker >= UpLevel && Position >= 0)
			SellMarket();

		_prevDeMarker = deMarker;
	}
}
