namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// DoubleUp2 martingale strategy driven by CCI and MACD extremes.
/// </summary>
public class DoubleUp2MartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<int> _exitDistancePoints;
	private readonly StrategyParam<decimal> _balanceDivisor;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _martingaleStep;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Absolute threshold that both indicators must exceed.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Profit distance in points before locking gains.
	/// </summary>
	public int ExitDistancePoints
	{
		get => _exitDistancePoints.Value;
		set => _exitDistancePoints.Value = value;
	}

	/// <summary>
	/// Divisor applied to portfolio value for base volume estimation.
	/// </summary>
	public decimal BalanceDivisor
	{
		get => _balanceDivisor.Value;
		set => _balanceDivisor.Value = value;
	}

	/// <summary>
	/// Minimum trade volume allowed.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}

	/// <summary>
	/// Multiplier used when the martingale step increases.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DoubleUp2MartingaleStrategy"/>.
	/// </summary>
	public DoubleUp2MartingaleStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Lookback for the Commodity Channel Index", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4, 20, 1);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 33)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal smoothing period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 15, 1);

		_threshold = Param(nameof(Threshold), 230m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold", "Absolute level required from both indicators", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(50m, 400m, 10m);

		_exitDistancePoints = Param(nameof(ExitDistancePoints), 120)
			.SetGreaterThanZero()
			.SetDisplay("Exit Distance", "Required profit in points before closing", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(20, 300, 10);

		_balanceDivisor = Param(nameof(BalanceDivisor), 50001m)
			.SetGreaterThanZero()
			.SetDisplay("Balance Divisor", "Divisor used to derive base volume from equity", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(10000m, 100000m, 5000m);

		_minimumVolume = Param(nameof(MinimumVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Volume", "Lower bound for calculated trade volume", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 1m, 0.01m);

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Multiplier", "Volume multiplier applied after losses", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Primary Candle", "Timeframe used for CCI and MACD", "Data");
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

		_entryPrice = 0m;
		_martingaleStep = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var cci = new CommodityChannelIndex
		{
			Length = CciPeriod
		};

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortLength = MacdFastPeriod,
			LongLength = MacdSlowPeriod,
			SignalLength = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal macdValue, decimal macdSignal, decimal macdHistogram)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Security == null)
			return;

		if (_martingaleStep < 0)
			_martingaleStep = 0;

		var baseVolume = CalculateBaseVolume();
		if (baseVolume <= 0m)
			return;

		var tradeVolume = CalculateMartingaleVolume(baseVolume);
		if (tradeVolume <= 0m)
			return;

		if (cciValue > Threshold && macdValue > Threshold)
		{
			TryEnterShort(candle, tradeVolume);
		}
		else if (cciValue < -Threshold && macdValue < -Threshold)
		{
			TryEnterLong(candle, tradeVolume);
		}

		TryTakeProfit(candle);
	}

	private decimal CalculateBaseVolume()
	{
		var balance = Portfolio?.CurrentValue;
		var divisor = BalanceDivisor;

		decimal baseVolume = MinimumVolume;

		if (balance is decimal equity && equity > 0m && divisor > 0m)
			baseVolume = Math.Max(MinimumVolume, equity / divisor);
		else if (Volume > 0m)
			baseVolume = Math.Max(MinimumVolume, Volume);

		return baseVolume;
	}

	private decimal CalculateMartingaleVolume(decimal baseVolume)
	{
		var multiplier = MartingaleMultiplier;
		if (multiplier <= 0m)
			multiplier = 1m;

		var power = (decimal)Math.Pow((double)multiplier, _martingaleStep);
		return baseVolume * power;
	}

	private void TryEnterShort(ICandleMessage candle, decimal tradeVolume)
	{
		if (Position < 0m)
			return;

		if (Position > 0m)
		{
			AdjustMartingaleAfterClose(true, candle.ClosePrice);
		}

		var totalVolume = tradeVolume + Math.Max(0m, Position);
		if (totalVolume <= 0m)
			return;

		SellMarket(totalVolume);
		_entryPrice = candle.ClosePrice;
	}

	private void TryEnterLong(ICandleMessage candle, decimal tradeVolume)
	{
		if (Position > 0m)
			return;

		if (Position < 0m)
		{
			AdjustMartingaleAfterClose(false, candle.ClosePrice);
		}

		var totalVolume = tradeVolume + Math.Max(0m, -Position);
		if (totalVolume <= 0m)
			return;

		BuyMarket(totalVolume);
		_entryPrice = candle.ClosePrice;
	}

	private void TryTakeProfit(ICandleMessage candle)
	{
		if (Position == 0m || _entryPrice == 0m)
			return;

		var step = Security?.PriceStep;
		var point = step.HasValue && step.Value > 0m ? step.Value : 1m;
		var targetDistance = ExitDistancePoints * point;

		if (targetDistance <= 0m)
			return;

		if (Position > 0m)
		{
			if (candle.ClosePrice - _entryPrice >= targetDistance)
			{
				SellMarket(Position);
				_martingaleStep += 2;
				_entryPrice = 0m;
			}
		}
		else if (Position < 0m)
		{
			if (_entryPrice - candle.ClosePrice >= targetDistance)
			{
				BuyMarket(-Position);
				_martingaleStep += 2;
				_entryPrice = 0m;
			}
		}
	}

	private void AdjustMartingaleAfterClose(bool wasLong, decimal exitPrice)
	{
		if (_entryPrice == 0m)
			return;

		var profit = wasLong ? exitPrice - _entryPrice : _entryPrice - exitPrice;

		if (profit < 0m)
		{
			_martingaleStep++;
		}
		else if (profit > 0m)
		{
			_martingaleStep = 0;
		}

		_entryPrice = 0m;
	}
}
