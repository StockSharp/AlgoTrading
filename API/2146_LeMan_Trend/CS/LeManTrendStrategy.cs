namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Leman Trend strategy using high/low differences smoothed by EMA.
/// Opens long when bullish pressure exceeds bearish, short otherwise.
/// </summary>
public class LeManTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _min;
	private readonly StrategyParam<int> _midle;
	private readonly StrategyParam<int> _max;
	private readonly StrategyParam<int> _periodEma;
	private readonly StrategyParam<bool> _useLong;
	private readonly StrategyParam<bool> _useShort;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highMin = null!;
	private Highest _highMidle = null!;
	private Highest _highMax = null!;
	private Lowest _lowMin = null!;
	private Lowest _lowMidle = null!;
	private Lowest _lowMax = null!;
	private ExponentialMovingAverage _bulls = null!;
	private ExponentialMovingAverage _bears = null!;

	private decimal _prevBulls;
	private decimal _prevBears;

	public int Min
	{
		get => _min.Value;
		set => _min.Value = value;
	}

	public int Midle
	{
		get => _midle.Value;
		set => _midle.Value = value;
	}

	public int Max
	{
		get => _max.Value;
		set => _max.Value = value;
	}

	public int PeriodEma
	{
		get => _periodEma.Value;
		set => _periodEma.Value = value;
	}

	public bool UseLong
	{
		get => _useLong.Value;
		set => _useLong.Value = value;
	}

	public bool UseShort
	{
		get => _useShort.Value;
		set => _useShort.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LeManTrendStrategy()
	{
		_min = Param(nameof(Min), 13)
			.SetGreaterThanZero()
			.SetDisplay("Min Period", "Minimum lookback for highs/lows", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 25, 1);

		_midle = Param(nameof(Midle), 21)
			.SetGreaterThanZero()
			.SetDisplay("Middle Period", "Middle lookback for highs/lows", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_max = Param(nameof(Max), 34)
			.SetGreaterThanZero()
			.SetDisplay("Max Period", "Maximum lookback for highs/lows", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_periodEma = Param(nameof(PeriodEma), 3)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Smoothing period for bulls/bears", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_useLong = Param(nameof(UseLong), true)
			.SetDisplay("Use Long", "Enable long trades", "Trading");

		_useShort = Param(nameof(UseShort), true)
			.SetDisplay("Use Short", "Enable short trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculations", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevBulls = default;
		_prevBears = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highMin = new Highest { Length = Min };
		_highMidle = new Highest { Length = Midle };
		_highMax = new Highest { Length = Max };
		_lowMin = new Lowest { Length = Min };
		_lowMidle = new Lowest { Length = Midle };
		_lowMax = new Lowest { Length = Max };
		_bulls = new ExponentialMovingAverage { Length = PeriodEma };
		_bears = new ExponentialMovingAverage { Length = PeriodEma };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highMin, _highMidle, _highMax, _lowMin, _lowMidle, _lowMax, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bulls);
			DrawIndicator(area, _bears);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highMin, decimal highMidle, decimal highMax,
		decimal lowMin, decimal lowMidle, decimal lowMax)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hh = (candle.HighPrice - highMin) + (candle.HighPrice - highMidle) + (candle.HighPrice - highMax);
		var ll = (lowMin - candle.LowPrice) + (lowMidle - candle.LowPrice) + (lowMax - candle.LowPrice);

		var bulls = _bulls.Process(hh).ToDecimal();
		var bears = _bears.Process(ll).ToDecimal();

		if (!_bulls.IsFormed || !_bears.IsFormed)
			return;

		if (UseLong && _prevBulls <= _prevBears && bulls > bears && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (UseShort && _prevBulls >= _prevBears && bulls < bears && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevBulls = bulls;
		_prevBears = bears;
	}
}