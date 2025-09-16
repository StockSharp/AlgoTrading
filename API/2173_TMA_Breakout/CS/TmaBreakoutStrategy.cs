using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters when price breaks the Triangular Moving Average by configurable offsets.
/// </summary>
public class TmaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevTma;
	private decimal? _prevClose;

	/// <summary>
	/// Period for the Triangular Moving Average.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Offset above the TMA to trigger a long entry.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Offset below the TMA to trigger a short entry.
	/// </summary>
	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the TMA breakout strategy.
	/// </summary>
	public TmaBreakoutStrategy()
	{
		_length = Param(nameof(Length), 30)
			.SetDisplay("TMA Length", "Period for the Triangular Moving Average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 10);

		_upLevel = Param(nameof(UpLevel), 300m)
			.SetDisplay("Upper Level", "Offset above TMA in price units", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100m, 500m, 100m);

		_downLevel = Param(nameof(DownLevel), 300m)
			.SetDisplay("Lower Level", "Offset below TMA in price units", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100m, 500m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevTma = _prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tma = new TriangularMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(tma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, tma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal tmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var prevTma = _prevTma;
		var prevClose = _prevClose;

		if (prevTma is null || prevClose is null)
		{
			_prevTma = tmaValue;
			_prevClose = candle.ClosePrice;
			return;
		}

		var signalUp = prevClose > prevTma + UpLevel;
		var signalDn = prevClose < prevTma - DownLevel;

		var volume = Volume + Math.Abs(Position);

		if (signalUp && Position <= 0)
			BuyMarket(volume);
		else if (signalDn && Position >= 0)
			SellMarket(volume);

		_prevTma = tmaValue;
		_prevClose = candle.ClosePrice;
	}
}
