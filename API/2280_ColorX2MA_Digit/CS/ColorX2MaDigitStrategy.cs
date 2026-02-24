using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy based on two sequential moving averages.
/// Trades on crossovers of fast and slow SMAs.
/// </summary>
public class ColorX2MaDigitStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorX2MaDigitStrategy()
	{
		_fastLength = Param(nameof(FastLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Length of the first smoothing", "Parameters");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Length of the second smoothing", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

		_prevFast = null;
		_prevSlow = null;

		var fastMa = new ExponentialMovingAverage { Length = FastLength };
		var slowMa = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fastMa;
			_prevSlow = slowMa;
			return;
		}

		var wasAbove = _prevFast > _prevSlow;
		var isAbove = fastMa > slowMa;

		// Fast MA crossed above slow MA -> buy
		if (!wasAbove && isAbove && Position <= 0)
			BuyMarket();
		// Fast MA crossed below slow MA -> sell
		else if (wasAbove && !isAbove && Position >= 0)
			SellMarket();

		_prevFast = fastMa;
		_prevSlow = slowMa;
	}
}
