using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual strategy selector with two SMA based long-only modes.
/// Strategy 1 uses fixed or trailing take profit.
/// Strategy 2 adds higher timeframe trend filter, ATR stop and partial take profit.
/// </summary>
public class DualStrategySelectorV2CryptogyaniStrategy : Strategy
{
	private readonly StrategyParam<string> _strategyOption;

	private readonly StrategyParam<int> _s1FastLength;
	private readonly StrategyParam<int> _s1SlowLength;
	private readonly StrategyParam<string> _s1TakeProfitMode;
	private readonly StrategyParam<decimal> _s1TakeProfitPerc;
	private readonly StrategyParam<decimal> _s1TakeProfitPips;
	private readonly StrategyParam<bool> _s1TrailingTakeProfitEnabled;

	private readonly StrategyParam<int> _s2FastLength;
	private readonly StrategyParam<int> _s2SlowLength;
	private readonly StrategyParam<int> _s2AtrLength;
	private readonly StrategyParam<decimal> _s2AtrMultiplier;
	private readonly StrategyParam<decimal> _s2PartialTakeProfitPerc;
	private readonly StrategyParam<DataType> _s2TimeframeTrend;

	private readonly StrategyParam<DataType> _candleType;

	private decimal _s1PrevFast;
	private decimal _s1PrevSlow;
	private decimal _s2PrevFast;
	private decimal _s2PrevSlow;

	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;
	private decimal _trailingStopPrice;
	private decimal _higherTimeframeTrendMa;

	private bool _hasPartialExit;

	public DualStrategySelectorV2CryptogyaniStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Main timeframe for strategy.", "General");

		_strategyOption = Param(nameof(StrategyOption), "Strategy 1")
			.SetDisplay("Select strategy", "Choose between two strategies.", "General");

		_s1FastLength = Param(nameof(S1FastLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length (S1)", "Fast SMA length for strategy 1.", "Strategy 1");

		_s1SlowLength = Param(nameof(S1SlowLength), 49)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Length (S1)", "Slow SMA length for strategy 1.", "Strategy 1");

		_s1TakeProfitMode = Param(nameof(S1TakeProfitMode), "Percentage")
			.SetDisplay("Take Profit Mode (S1)", "Take profit mode: Percentage or Pips.", "Strategy 1");

		_s1TakeProfitPerc = Param(nameof(S1TakeProfitPerc), 7m.Percents())
			.SetDisplay("Take Profit % (S1)", "Take profit percentage for strategy 1.", "Strategy 1");

		_s1TakeProfitPips = Param(nameof(S1TakeProfitPips), 50m)
			.SetDisplay("Take Profit Pips (S1)", "Take profit in pips for strategy 1.", "Strategy 1");

		_s1TrailingTakeProfitEnabled = Param(nameof(S1TrailingTakeProfitEnabled), false)
			.SetDisplay("Enable Trailing (S1)", "Enable trailing take profit.", "Strategy 1");

		_s2FastLength = Param(nameof(S2FastLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length (S2)", "Fast SMA length for strategy 2.", "Strategy 2");

		_s2SlowLength = Param(nameof(S2SlowLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Length (S2)", "Slow SMA length for strategy 2.", "Strategy 2");

		_s2AtrLength = Param(nameof(S2AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length (S2)", "ATR length for stop loss.", "Strategy 2");

		_s2AtrMultiplier = Param(nameof(S2AtrMultiplier), 1.5m)
			.SetDisplay("ATR Multiplier (S2)", "ATR multiplier for stop loss.", "Strategy 2");

		_s2PartialTakeProfitPerc = Param(nameof(S2PartialTakeProfitPerc), 50m)
			.SetDisplay("Partial Take Profit % (S2)", "Percent of position to close at target.", "Strategy 2");

		_s2TimeframeTrend = Param(nameof(S2TimeframeTrend), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Trend Timeframe (S2)", "Higher timeframe for trend filter.", "Strategy 2");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public string StrategyOption
	{
		get => _strategyOption.Value;
		set => _strategyOption.Value = value;
	}

	public int S1FastLength
	{
		get => _s1FastLength.Value;
		set => _s1FastLength.Value = value;
	}

	public int S1SlowLength
	{
		get => _s1SlowLength.Value;
		set => _s1SlowLength.Value = value;
	}

	public string S1TakeProfitMode
	{
		get => _s1TakeProfitMode.Value;
		set => _s1TakeProfitMode.Value = value;
	}

	public decimal S1TakeProfitPerc
	{
		get => _s1TakeProfitPerc.Value;
		set => _s1TakeProfitPerc.Value = value;
	}

	public decimal S1TakeProfitPips
	{
		get => _s1TakeProfitPips.Value;
		set => _s1TakeProfitPips.Value = value;
	}

	public bool S1TrailingTakeProfitEnabled
	{
		get => _s1TrailingTakeProfitEnabled.Value;
		set => _s1TrailingTakeProfitEnabled.Value = value;
	}

	public int S2FastLength
	{
		get => _s2FastLength.Value;
		set => _s2FastLength.Value = value;
	}

	public int S2SlowLength
	{
		get => _s2SlowLength.Value;
		set => _s2SlowLength.Value = value;
	}

	public int S2AtrLength
	{
		get => _s2AtrLength.Value;
		set => _s2AtrLength.Value = value;
	}

	public decimal S2AtrMultiplier
	{
		get => _s2AtrMultiplier.Value;
		set => _s2AtrMultiplier.Value = value;
	}

	public decimal S2PartialTakeProfitPerc
	{
		get => _s2PartialTakeProfitPerc.Value;
		set => _s2PartialTakeProfitPerc.Value = value;
	}

	public DataType S2TimeframeTrend
	{
		get => _s2TimeframeTrend.Value;
		set => _s2TimeframeTrend.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var s1Fast = new SimpleMovingAverage { Length = S1FastLength };
		var s1Slow = new SimpleMovingAverage { Length = S1SlowLength };
		var s2Fast = new SimpleMovingAverage { Length = S2FastLength };
		var s2Slow = new SimpleMovingAverage { Length = S2SlowLength };
		var s2Atr = new AverageTrueRange { Length = S2AtrLength };
		var trendSma = new SimpleMovingAverage { Length = S2SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(s1Fast, s1Slow, s2Fast, s2Slow, s2Atr, ProcessCandle)
			.Start();

		var trendSubscription = SubscribeCandles(S2TimeframeTrend);
		trendSubscription
			.Bind(trendSma, ProcessTrend)
			.Start();
	}

	private void ProcessTrend(ICandleMessage candle, decimal trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_higherTimeframeTrendMa = trendValue;
	}

	private void ProcessCandle(ICandleMessage candle, decimal s1Fast, decimal s1Slow, decimal s2Fast, decimal s2Slow, decimal s2Atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var s1Cross = _s1PrevFast < _s1PrevSlow && s1Fast > s1Slow;
		var s2Cross = _s2PrevFast < _s2PrevSlow && s2Fast > s2Slow;

		_s1PrevFast = s1Fast;
		_s1PrevSlow = s1Slow;
		_s2PrevFast = s2Fast;
		_s2PrevSlow = s2Slow;

		if (StrategyOption == "Strategy 1")
		{
			if (Position == 0 && s1Cross)
			{
				BuyMarket();
				_trailingStopPrice = 0m;
			}

			if (Position > 0)
			{
				if (S1TakeProfitMode == "Percentage")
					_takeProfitPrice = candle.ClosePrice * (1 + S1TakeProfitPerc);
				else
					_takeProfitPrice = candle.ClosePrice + S1TakeProfitPips * Security.PriceStep;

				if (S1TrailingTakeProfitEnabled)
				{
					var trail = candle.HighPrice - S1TakeProfitPips * Security.PriceStep;
					if (trail > _trailingStopPrice)
						_trailingStopPrice = trail;

					if (candle.LowPrice <= _trailingStopPrice)
					{
						SellMarket();
						_trailingStopPrice = 0m;
					}
				}
				else if (candle.HighPrice >= _takeProfitPrice)
				{
					SellMarket();
				}
			}
		}
		else if (StrategyOption == "Strategy 2")
		{
			_stopLossPrice = candle.ClosePrice - s2Atr * S2AtrMultiplier;
			_takeProfitPrice = candle.ClosePrice * (1 + S2PartialTakeProfitPerc / 100m);

			if (Position == 0 && s2Cross && candle.ClosePrice > _higherTimeframeTrendMa)
			{
				BuyMarket();
				_hasPartialExit = false;
			}

			if (Position > 0)
			{
				if (!_hasPartialExit && candle.HighPrice >= _takeProfitPrice)
				{
					SellMarket(Position * S2PartialTakeProfitPerc / 100m);
					_hasPartialExit = true;
				}

				if (candle.LowPrice <= _stopLossPrice)
				{
					SellMarket();
					_hasPartialExit = false;
				}
			}
		}
	}
}
