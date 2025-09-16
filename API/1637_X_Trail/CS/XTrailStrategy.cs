using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy based on median price.
/// Enters long when fast MA crosses above slow MA.
/// Enters short when fast MA crosses below slow MA.
/// </summary>
public class XTrailStrategy : Strategy
{
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _ma1;
	private SMA _ma2;

	private decimal? _prevMa1;
	private decimal? _prevMa2;
	private decimal? _prev2Ma1;
	private decimal? _prev2Ma2;

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public XTrailStrategy()
	{
		_ma1Length = Param(nameof(Ma1Length), 1)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Length of the fast moving average", "General");

		_ma2Length = Param(nameof(Ma2Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Length of the slow moving average", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_ma1 = new SMA { Length = Ma1Length };
		_ma2 = new SMA { Length = Ma2Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var ma1 = _ma1.Process(median, candle.OpenTime, true).ToDecimal();
		var ma2 = _ma2.Process(median, candle.OpenTime, true).ToDecimal();

		if (_prevMa1 != null && _prevMa2 != null && _prev2Ma1 != null && _prev2Ma2 != null)
		{
			var crossUp = ma1 > ma2 && _prevMa1 > _prevMa2 && _prev2Ma1 < _prev2Ma2;
			var crossDown = ma1 < ma2 && _prevMa1 < _prevMa2 && _prev2Ma1 > _prev2Ma2;

			if (crossUp && Position <= 0)
				BuyMarket();
			else if (crossDown && Position >= 0)
				SellMarket();
		}

		_prev2Ma1 = _prevMa1;
		_prev2Ma2 = _prevMa2;
		_prevMa1 = ma1;
		_prevMa2 = ma2;
	}
}

