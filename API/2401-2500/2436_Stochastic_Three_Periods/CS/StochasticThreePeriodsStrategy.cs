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
/// Stochastic alignment strategy using fast and slow stochastic oscillators.
/// Enters when both stochastics agree on direction.
/// </summary>
public class StochasticThreePeriodsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _slowRsi;
	private decimal _prevSlow;
	private int _lastSignal;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StochasticThreePeriodsStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast K", "Fast stochastic K period", "Parameters");

		_slowPeriod = Param(nameof(SlowPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow K", "Slow stochastic K period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSlow = 0m;
		_lastSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastRsi = new RelativeStrengthIndex { Length = FastPeriod };
		_slowRsi = new RelativeStrengthIndex { Length = SlowPeriod };
		_prevSlow = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastRsi, (candle, fastValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var slowResult = _slowRsi.Process(candle.ClosePrice, candle.CloseTime, true);
				if (!_slowRsi.IsFormed || slowResult.IsEmpty)
					return;
				var slowValue = slowResult.ToDecimal();

				if (fastValue > slowValue && fastValue > 55m && slowValue > 50m && slowValue > _prevSlow && _lastSignal != 1 && Position <= 0)
				{
					BuyMarket();
					_lastSignal = 1;
				}
				else if (fastValue < slowValue && fastValue < 45m && slowValue < 50m && slowValue < _prevSlow && _lastSignal != -1 && Position >= 0)
				{
					SellMarket();
					_lastSignal = -1;
				}

				_prevSlow = slowValue;
			})
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
