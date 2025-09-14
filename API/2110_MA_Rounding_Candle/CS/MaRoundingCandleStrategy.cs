using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA Rounding Candle strategy.
/// Opens a long position when a smoothed candle is bullish and a short position when it is bearish.
/// </summary>
public class MaRoundingCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _openMa = null!;
	private SimpleMovingAverage _closeMa = null!;
	private int _prevColor = 1;

	public MaRoundingCandleStrategy()
	{
	    _maLength = Param(nameof(MaLength), 12)
	        .SetDisplay("Moving average length")
	        .SetCanOptimize(true, 2, 100, 1);
	    _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)));
	}

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    _openMa = new SimpleMovingAverage { Length = MaLength };
	    _closeMa = new SimpleMovingAverage { Length = MaLength };

	    var subscription = SubscribeCandles(CandleType);
	    subscription.Bind(ProcessCandle).Start();

	    var area = CreateChartArea();
	    if (area != null)
	    {
	        DrawCandles(area, subscription);
	        DrawIndicator(area, _openMa);
	        DrawIndicator(area, _closeMa);
	        DrawOwnTrades(area);
	    }

	    StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    var openVal = _openMa.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();
	    var closeVal = _closeMa.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

	    var color = openVal < closeVal ? 2 : openVal > closeVal ? 0 : 1;

	    if (_prevColor == 2 && color != 2 && Position <= 0)
	        BuyMarket(Volume + Math.Abs(Position));
	    else if (_prevColor == 0 && color != 0 && Position >= 0)
	        SellMarket(Volume + Position);

	    _prevColor = color;
	}
}
