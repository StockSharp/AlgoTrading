using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-term ZigZag investment strategy with SMA trend filter.
/// </summary>
public class ZigDanZagUltimateInvestmentLongTermStrategy : Strategy
{
	private readonly StrategyParam<int> _zigzagDepth;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastZigzag;
	private decimal _lastZigzagHigh;
	private decimal _lastZigzagLow;
	private int _direction;
	private decimal _sma;

	public int ZigzagDepth
	{
		get => _zigzagDepth.Value;
		set => _zigzagDepth.Value = value;
	}

	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ZigDanZagUltimateInvestmentLongTermStrategy()
	{
		_zigzagDepth = Param(nameof(ZigzagDepth), 12)
			.SetDisplay("ZigZag Depth", "Pivot search depth", "ZigZag");
		_smaLength = Param(nameof(SmaLength), 200)
			.SetDisplay("SMA Length", "Long-term trend filter", "Trend");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastZigzag = 0m;
		_lastZigzagHigh = 0m;
		_lastZigzagLow = 0m;
		_direction = 0;
		_sma = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highest = new Highest { Length = ZigzagDepth };
		var lowest = new Lowest { Length = ZigzagDepth };
		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_sma = smaValue;

		// update last ZigZag pivot
		if (candle.HighPrice >= highest && _direction != 1)
		{
			_lastZigzag = candle.HighPrice;
			_lastZigzagHigh = candle.HighPrice;
			_direction = 1;
		}
		else if (candle.LowPrice <= lowest && _direction != -1)
		{
			_lastZigzag = candle.LowPrice;
			_lastZigzagLow = candle.LowPrice;
			_direction = -1;
		}

		// long-only logic using SMA as trend filter
		if (_lastZigzag == _lastZigzagLow && candle.ClosePrice > _sma && Position <= 0)
			BuyMarket();
		else if (_lastZigzag == _lastZigzagHigh && candle.ClosePrice < _sma && Position > 0)
			SellMarket();
	}
}
