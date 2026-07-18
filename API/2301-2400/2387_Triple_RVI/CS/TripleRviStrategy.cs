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
/// Strategy that trades using Relative Vigor Index with multiple period confirmations.
/// Uses a long-period RVI for trend, mid-period for confirmation, and short for entry.
/// </summary>
public class TripleRviStrategy : Strategy
{
	private readonly StrategyParam<int> _rviPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private int _trend1;
	private int _trend2;
	private decimal _prevSignal = decimal.MinValue;

	public int RviPeriod { get => _rviPeriod.Value; set => _rviPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TripleRviStrategy()
	{
		_rviPeriod = Param(nameof(RviPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("RVI Period", "Base period of RVI", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Trading timeframe", "General");
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

		_trend1 = 0;
		_trend2 = 0;
		_prevSignal = decimal.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trend1 = 0;
		_trend2 = 0;
		_prevSignal = decimal.MinValue;

		var trendRsi = new RelativeStrengthIndex { Length = RviPeriod * 3 };
		var midRsi = new RelativeStrengthIndex { Length = RviPeriod * 2 };
		var signalRsi = new RelativeStrengthIndex { Length = RviPeriod };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(trendRsi, midRsi, signalRsi, ProcessCandle).Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal trendValue, decimal midValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_trend1 = trendValue > 55m ? 1 : trendValue < 45m ? -1 : 0;
		_trend2 = midValue > 55m ? 1 : midValue < 45m ? -1 : 0;

		if (_prevSignal == decimal.MinValue)
		{
			_prevSignal = signalValue;
			return;
		}

		var crossUp = _prevSignal <= 50m && signalValue > 50m;
		var crossDown = _prevSignal >= 50m && signalValue < 50m;

		if (crossUp && _trend1 > 0 && _trend2 > 0 && Position <= 0)
			BuyMarket();
		else if (crossDown && _trend1 < 0 && _trend2 < 0 && Position >= 0)
			SellMarket();

		if (Position > 0 && (_trend1 < 0 || _trend2 < 0))
			SellMarket();
		else if (Position < 0 && (_trend1 > 0 || _trend2 > 0))
			BuyMarket();

		_prevSignal = signalValue;
	}
}
