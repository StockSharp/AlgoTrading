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
/// GO strategy based on moving averages of open, high, low and close prices.
/// Closes opposite positions when the GO value changes sign.
/// Opens new trades in the direction of the GO value.
/// </summary>
public class GoRiskManagedStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _openMa;
	private SimpleMovingAverage _highMa;
	private SimpleMovingAverage _lowMa;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for indicators and trading logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="GoRiskManagedStrategy"/>.
	/// </summary>
	public GoRiskManagedStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "Moving average period", "Indicator")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_openMa = new SimpleMovingAverage { Length = MaPeriod };
		_highMa = new SimpleMovingAverage { Length = MaPeriod };
		_lowMa = new SimpleMovingAverage { Length = MaPeriod };
		var closeMa = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(closeMa, (candle, close) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var openResult = _openMa.Process(candle.OpenPrice, candle.CloseTime, true);
				var highResult = _highMa.Process(candle.HighPrice, candle.CloseTime, true);
				var lowResult = _lowMa.Process(candle.LowPrice, candle.CloseTime, true);

				if (!_openMa.IsFormed || !_highMa.IsFormed || !_lowMa.IsFormed || !closeMa.IsFormed)
					return;

				var open = openResult.ToDecimal();
				var high = highResult.ToDecimal();
				var low = lowResult.ToDecimal();

				var go = ((close - open) + (high - open) + (low - open) + (close - low) + (close - high)) * candle.TotalVolume;

				if (go > 0 && Position <= 0)
				{
					BuyMarket();
				}
				else if (go < 0 && Position >= 0)
				{
					SellMarket();
				}
			})
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
