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

	private RelativeVigorIndex _rviTrend;
	private RelativeVigorIndex _rviMid;
	private int _trend1;
	private int _trend2;
	private decimal? _prevAvg3;
	private decimal? _prevSig3;

	public int RviPeriod { get => _rviPeriod.Value; set => _rviPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TripleRviStrategy()
	{
		_rviPeriod = Param(nameof(RviPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("RVI Period", "Base period of RVI", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Trading timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trend1 = 0;
		_trend2 = 0;
		_prevAvg3 = null;
		_prevSig3 = null;

		// Longer period RVI for trend
		_rviTrend = new RelativeVigorIndex();
		_rviTrend.Average.Length = RviPeriod * 3;

		// Medium period RVI for confirmation
		_rviMid = new RelativeVigorIndex();
		_rviMid.Average.Length = RviPeriod * 2;

		// Short period RVI for entry signals
		var rviSignal = new RelativeVigorIndex();
		rviSignal.Average.Length = RviPeriod;

		var sub = SubscribeCandles(CandleType);
		sub.BindEx(rviSignal, (candle, val) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			// Process trend and mid RVIs manually
			var trendResult = _rviTrend.Process(candle);
			var midResult = _rviMid.Process(candle);

			if (!_rviTrend.IsFormed || !_rviMid.IsFormed)
				return;

			var trendVal = (IRelativeVigorIndexValue)trendResult;
			var midVal = (IRelativeVigorIndexValue)midResult;
			var sigVal = (IRelativeVigorIndexValue)val;

			if (trendVal.Average is decimal tAvg && trendVal.Signal is decimal tSig)
				_trend1 = tAvg > tSig ? 1 : tAvg < tSig ? -1 : 0;

			if (midVal.Average is decimal mAvg && midVal.Signal is decimal mSig)
				_trend2 = mAvg > mSig ? 1 : mAvg < mSig ? -1 : 0;

			if (sigVal.Average is not decimal avg || sigVal.Signal is not decimal sig)
				return;

			if (_prevAvg3 is decimal prevAvg && _prevSig3 is decimal prevSig)
			{
				var crossUp = prevAvg < prevSig && avg >= sig;
				var crossDown = prevAvg > prevSig && avg <= sig;

				if (crossUp && _trend1 > 0 && _trend2 > 0 && Position <= 0)
					BuyMarket();
				else if (crossDown && _trend1 < 0 && _trend2 < 0 && Position >= 0)
					SellMarket();

				// Exit on trend reversal
				if (Position > 0 && (_trend1 < 0 || _trend2 < 0))
					SellMarket();
				else if (Position < 0 && (_trend1 > 0 || _trend2 > 0))
					BuyMarket();
			}

			_prevAvg3 = avg;
			_prevSig3 = sig;
		}).Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
