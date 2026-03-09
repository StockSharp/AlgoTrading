namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// CCI Expert strategy: CCI crossover with level-based signals.
/// Buys when CCI crosses above +1 from below, sells when crosses below -1 from above.
/// </summary>
public class CciExpertStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;

	private decimal? _prevCci;
	private decimal? _prevPrevCci;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	public CciExpertStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCci = null;
		_prevPrevCci = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCci = null;
		_prevPrevCci = null;
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevCci is decimal prev && _prevPrevCci is decimal prev2)
		{
			// Long: CCI stayed above +100 for 2 bars while prior bar was below
			var longSignal = cciValue > 100m && prev > 100m && prev2 < 100m;
			// Short: CCI stayed below -100 for 2 bars while prior bar was above
			var shortSignal = cciValue < -100m && prev < -100m && prev2 > -100m;

			if (longSignal && Position <= 0)
				BuyMarket();
			else if (shortSignal && Position >= 0)
				SellMarket();
		}

		_prevPrevCci = _prevCci;
		_prevCci = cciValue;
	}
}
