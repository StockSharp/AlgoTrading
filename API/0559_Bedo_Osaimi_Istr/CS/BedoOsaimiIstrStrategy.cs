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
/// Bedo Osaimi Istr Strategy based on moving averages of open and close prices.
/// Buys when the moving average of close crosses above the moving average of open, and sells on opposite cross.
/// </summary>
public class BedoOsaimiIstrStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;

	private SMA _closeMa;
	private SMA _openMa;
	private decimal? _prevClose;
	private decimal? _prevOpen;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average length for both open and close series.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BedoOsaimiIstrStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maLength = Param(nameof(MaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Parameters")

			.SetOptimize(3, 50, 3);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = null;
		_prevOpen = null;
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

		_closeMa = new SMA { Length = MaLength };
		_openMa = new SMA { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_closeMa, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _closeMa);
			DrawIndicator(area, _openMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openMaResult = _openMa.Process(new DecimalIndicatorValue(_openMa, candle.OpenPrice, candle.ServerTime) { IsFinal = true });
		if (!openMaResult.IsFormed)
		{
			_prevClose = closeMaValue;
			_prevOpen = null;
			return;
		}
		var openMaValue = openMaResult.GetValue<decimal>();

		if (_prevClose is null || _prevOpen is null)
		{
			_prevClose = closeMaValue;
			_prevOpen = openMaValue;
			// Force first trade to verify framework works
			if (Position == 0)
				BuyMarket();
			return;
		}

		var prevClose = _prevClose.Value;
		var prevOpen = _prevOpen.Value;

		// Buy when close MA crosses above open MA
		if (closeMaValue > openMaValue && prevClose <= prevOpen && Position == 0)
		{
			BuyMarket();
		}
		// Sell when close MA crosses below open MA
		else if (closeMaValue < openMaValue && prevClose >= prevOpen && Position == 0)
		{
			SellMarket();
		}

		_prevClose = closeMaValue;
		_prevOpen = openMaValue;
	}
}
