using System;
using System.Collections.Generic;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_closeMa = new SMA { Length = MaLength };
		_openMa = new SMA { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_closeMa, _openMa, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _closeMa);
			DrawIndicator(area, _openMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeMaValue, decimal openMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevClose is null || _prevOpen is null)
		{
			_prevClose = closeMaValue;
			_prevOpen = openMaValue;
			return;
		}

		var prevClose = _prevClose.Value;
		var prevOpen = _prevOpen.Value;

		if (prevClose <= prevOpen && closeMaValue > openMaValue && Position <= 0)
		{
			RegisterBuy();
		}
		else if (prevClose >= prevOpen && closeMaValue < openMaValue && Position >= 0)
		{
			RegisterSell();
		}

		_prevClose = closeMaValue;
		_prevOpen = openMaValue;
	}
}
