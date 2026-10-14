using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA Stochastic strategy.
/// Price above the MaPeriod SMA is an uptrend, below it a downtrend. Stochastic %K dipping below StochOversold in an uptrend
/// prepares a long that is bought on the next upturn of %K while price stays above the SMA; %K reaching StochOverbought in a downtrend
/// prepares a short sold on the next downturn. Leaving the trend cancels the setup. An opposite signal reverses the position,
/// and a percent stop limits the loss.
/// </summary>
public class MaStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	// The prepared side: 1 long, -1 short, 0 none.
	private int _setup;

	/// <summary>
	/// Period of the trend SMA.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period of stochastic %K.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period of stochastic %D.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Level that prepares a long.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// Level that prepares a short.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MaStochasticStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the trend SMA", "Indicators");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Lookback period of stochastic %K", "Indicators");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Smoothing period of stochastic %D", "Indicators");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stochastic Oversold", "Level that prepares a long", "Indicators");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stochastic Overbought", "Level that prepares a short", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevK = null;
		_setup = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_setup = 0;

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = StochDPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, stochastic, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, stochastic);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFormed || !stochasticValue.IsFormed)
			return;

		if (stochasticValue is not IStochasticOscillatorValue { K: decimal k })
			return;

		var ma = smaValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var prevK = _prevK;
		_prevK = k;

		var uptrend = close > ma;
		var downtrend = close < ma;
		var signal = 0;

		if (prevK is decimal lastK)
		{
			if (_setup == 1 && uptrend && k > lastK)
				signal = 1;
			else if (_setup == -1 && downtrend && k < lastK)
				signal = -1;
		}

		if (signal != 0)
			_setup = 0;

		if (uptrend && k < StochOversold)
			_setup = 1;
		else if (downtrend && k > StochOverbought)
			_setup = -1;
		else if ((_setup == 1 && !uptrend) || (_setup == -1 && !downtrend))
			_setup = 0;

		if (signal == 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (signal > 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (signal < 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
