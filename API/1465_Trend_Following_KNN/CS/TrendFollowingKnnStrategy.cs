using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified trend following strategy using average price change and moving average.
/// Buys when recent change is positive and price above MA, sells when negative and below MA.
/// </summary>
public class TrendFollowingKnnStrategy : Strategy
{
	private readonly StrategyParam<int> _windowSize;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma;
	private SimpleMovingAverage _changeSma;
	private decimal? _prevClose;

	/// <summary>
	/// Number of bars for average change calculation.
	/// </summary>
	public int WindowSize
	{
		get => _windowSize.Value;
		set => _windowSize.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrendFollowingKnnStrategy"/>.
	/// </summary>
	public TrendFollowingKnnStrategy()
	{
		_windowSize = Param(nameof(WindowSize), 20)
		.SetGreaterThanZero()
		.SetDisplay("Window Size", "Bars for average change", "General")
		.SetCanOptimize(true);

		_maLength = Param(nameof(MaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("MA Length", "Moving average length", "General")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ma = default;
		_changeSma = default;
		_prevClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new SimpleMovingAverage { Length = MaLength };
		_changeSma = new SimpleMovingAverage { Length = WindowSize };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_prevClose == null)
		{
				_prevClose = candle.ClosePrice;
				return;
		}

		var change = candle.ClosePrice - _prevClose.Value;
		_prevClose = candle.ClosePrice;

		var changeValue = _changeSma.Process(change);
		if (!changeValue.IsFinal)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var avgChange = changeValue.ToDecimal();

		if (avgChange > 0 && candle.ClosePrice > maValue && Position <= 0)
		{
				BuyMarket();
		}
		else if (avgChange < 0 && candle.ClosePrice < maValue && Position >= 0)
		{
				SellMarket();
		}
	}
}
