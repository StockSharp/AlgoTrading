using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA & MA crossover strategy.
/// Enters long when EMA of the MA is below the MA and short when above.
/// </summary>
public class EmaMaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _ma;
	private EMA _ema;
	private int _previousPos;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Exponential moving average length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EmaMaCrossoverStrategy"/>.
	/// </summary>
	public EmaMaCrossoverStrategy()
	{
		_maLength = Param(nameof(MaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Period of the simple moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_emaLength = Param(nameof(EmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Period of the exponential moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Parameters");
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
		_previousPos = 0;
		_ma = null;
		_ema = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new SMA { Length = MaLength };
		_ema = new EMA { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maValue = _ma.Process(candle.ClosePrice, candle.ServerTime, true).ToDecimal();
		var emaValue = _ema.Process(maValue, candle.ServerTime, true).ToDecimal();

		var pos = emaValue < maValue ? 1 : emaValue > maValue ? -1 : _previousPos;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousPos = pos;
			return;
		}

		if (pos == 1 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (pos == -1 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (pos == 0)
			ClosePosition();

		_previousPos = pos;
	}
}
