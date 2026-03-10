using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Divergence-based strategy converted from the Divergence Trader MQL expert advisor.
/// Trades based on the divergence between fast and slow moving averages.
/// </summary>
public class DivergenceTraderBasketStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _stayOutThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousDifference;
	private decimal _entryPrice;

	public DivergenceTraderBasketStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 7)
			.SetDisplay("Fast SMA Period", "Length of the fast simple moving average.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 88)
			.SetDisplay("Slow SMA Period", "Length of the slow simple moving average.", "Indicators");

		_buyThreshold = Param(nameof(BuyThreshold), 0.0001m)
			.SetDisplay("Buy Threshold", "Minimum divergence value required before buying.", "Signals");

		_stayOutThreshold = Param(nameof(StayOutThreshold), 1000m)
			.SetDisplay("Stay-Out Threshold", "Upper divergence limit that disables new entries.", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations.", "General");
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	public decimal StayOutThreshold
	{
		get => _stayOutThreshold.Value;
		set => _stayOutThreshold.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousDifference = null;
		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousDifference = null;
		_entryPrice = 0;

		var fastMa = new SimpleMovingAverage { Length = FastPeriod };
		var slowMa = new SimpleMovingAverage { Length = SlowPeriod };

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

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentDiff = fastValue - slowValue;

		if (_previousDifference == null)
		{
			_previousDifference = currentDiff;
			return;
		}

		var prevDiff = _previousDifference.Value;
		_previousDifference = currentDiff;

		// Manage open position
		if (Position != 0)
		{
			// Exit on divergence sign change
			if (Position > 0 && currentDiff < 0)
			{
				SellMarket();
				_entryPrice = 0;
			}
			else if (Position < 0 && currentDiff > 0)
			{
				BuyMarket();
				_entryPrice = 0;
			}
			return;
		}

		// Entry logic: divergence crosses zero line
		if (currentDiff > 0 && prevDiff <= 0)
		{
			// Bullish divergence crossover
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (currentDiff < 0 && prevDiff >= 0)
		{
			// Bearish divergence crossover
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
	}
}
