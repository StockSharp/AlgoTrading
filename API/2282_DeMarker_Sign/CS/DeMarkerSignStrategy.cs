using System;
using System.Collections.Generic;

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

	private decimal _prevDeMarker;
	private bool _isFirst = true;

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

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var deMarker = new DeMarker { Length = DeMarkerPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(deMarker, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarker)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevDeMarker = deMarker;
			_isFirst = false;
			return;
		}

		var buySignal = deMarker > DownLevel && _prevDeMarker <= DownLevel;
		var sellSignal = deMarker < UpLevel && _prevDeMarker >= UpLevel;
		_prevDeMarker = deMarker;

		if (buySignal)
		{
			if (Position < 0)
				ClosePosition();

			if (Position == 0)
				BuyMarket();
		}
		else if (sellSignal)
		{
			if (Position > 0)
				ClosePosition();

			if (Position == 0)
				SellMarket();
		}
	}
}
