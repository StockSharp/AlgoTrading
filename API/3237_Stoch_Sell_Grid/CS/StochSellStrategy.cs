using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic driven short strategy. Sells when fast and slow stochastic
/// both cross below oversold level, buys back on profit target or stochastic recovery.
/// </summary>
public class StochSellStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastKPeriod;
	private readonly StrategyParam<int> _slowKPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;

	private decimal _prevFastK;
	private decimal _prevSlowK;
	private bool _hasPrev;

	/// <summary>
	/// Constructor.
	/// </summary>
	public StochSellStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_fastKPeriod = Param(nameof(FastKPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast %K", "Fast stochastic K period", "Indicators");

		_slowKPeriod = Param(nameof(SlowKPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow %K", "Slow stochastic K period", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), 25m)
			.SetDisplay("Oversold Level", "Sell trigger level", "Signals");

		_overboughtLevel = Param(nameof(OverboughtLevel), 75m)
			.SetDisplay("Overbought Level", "Buy back level", "Signals");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastKPeriod
	{
		get => _fastKPeriod.Value;
		set => _fastKPeriod.Value = value;
	}

	public int SlowKPeriod
	{
		get => _slowKPeriod.Value;
		set => _slowKPeriod.Value = value;
	}

	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var fastStoch = new StochasticOscillator
		{
			K = { Length = FastKPeriod },
			D = { Length = 3 }
		};

		var slowStoch = new StochasticOscillator
		{
			K = { Length = SlowKPeriod },
			D = { Length = 3 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastStoch, slowStoch, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = (StochasticOscillatorValue)fastValue;
		var slow = (StochasticOscillatorValue)slowValue;

		if (fast.K is not decimal fastK || slow.K is not decimal slowK)
			return;

		if (_hasPrev)
		{
			// Sell signal: both stochastics cross below oversold
			var fastCross = _prevFastK > OversoldLevel && fastK <= OversoldLevel;
			var slowBelow = slowK < OversoldLevel;

			if (fastCross && slowBelow && Position >= 0)
			{
				SellMarket();
			}

			// Buy back: fast stochastic crosses above overbought
			var fastRecovery = _prevFastK < OverboughtLevel && fastK >= OverboughtLevel;

			if (fastRecovery && Position < 0)
			{
				BuyMarket();
			}
		}

		_prevFastK = fastK;
		_prevSlowK = slowK;
		_hasPrev = true;
	}
}
