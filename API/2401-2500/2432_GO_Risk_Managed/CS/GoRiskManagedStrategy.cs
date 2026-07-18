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
	private SimpleMovingAverage _closeMa;
	private decimal? _prevGo;

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
		_maPeriod = Param(nameof(MaPeriod), 10)
			.SetDisplay("MA Period", "Moving average period", "Indicator")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_prevGo = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_openMa = new SimpleMovingAverage { Length = MaPeriod };
		_closeMa = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_closeMa, (candle, close) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var openResult = _openMa.Process(candle.OpenPrice, candle.CloseTime, true);
				if (!_openMa.IsFormed || !_closeMa.IsFormed)
					return;

				var open = openResult.ToDecimal();
				var go = close - open;

				if (_prevGo is not decimal prevGo)
				{
					_prevGo = go;
					return;
				}

				if (prevGo <= 0m && go > 0m && Position <= 0)
					BuyMarket();
				else if (prevGo >= 0m && go < 0m && Position >= 0)
					SellMarket();

				_prevGo = go;
			})
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
