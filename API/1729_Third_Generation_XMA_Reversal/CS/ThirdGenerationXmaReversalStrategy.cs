using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 3rd Generation XMA reversal strategy.
/// </summary>
public class ThirdGenerationXmaReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema1 = null!;
	private ExponentialMovingAverage _ema2 = null!;
	private decimal _alpha;
	private decimal? _prev1;
	private decimal? _prev2;

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ThirdGenerationXmaReversalStrategy()
	{
		_maLength = Param(nameof(MaLength), 50)
			.SetDisplay("MA Length", "Base length for the moving average", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev1 = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema1 = new ExponentialMovingAverage { Length = MaLength * 2 };
		_ema2 = new ExponentialMovingAverage { Length = MaLength };
		_alpha = 2m * (2m * MaLength - 1m) / (2m * MaLength - 2m);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ema1Value = _ema1.Process(candle);
		if (!ema1Value.IsFinal)
			return;

		var ema2Value = _ema2.Process(ema1Value.GetValue<decimal>());
		if (!ema2Value.IsFinal)
			return;

		var xma = (_alpha + 1m) * ema1Value.GetValue<decimal>() - _alpha * ema2Value.GetValue<decimal>();

		if (_prev1.HasValue && _prev2.HasValue)
		{
			if (_prev1 < _prev2 && xma > _prev1 && Position <= 0)
				BuyMarket();
			else if (_prev1 > _prev2 && xma < _prev1 && Position >= 0)
				SellMarket();
		}

		_prev2 = _prev1;
		_prev1 = xma;
	}
}
