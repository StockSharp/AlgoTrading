using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the MACD Four Colors 2 Martingale expert advisor.
/// It opens new positions when the MACD color sequence matches the original rules and scales volume using a martingale factor.
/// </summary>
public class MacdFourColors2MartingaleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<decimal> _maxDrawdown;
	private readonly StrategyParam<decimal> _targetProfit;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;

	// Indicator state history for color calculations.
	private readonly List<decimal> _macdHistory = new();
	private readonly List<int> _colorHistory = new();

	// Martingale bookkeeping.
	private decimal _lastVolume;
	private decimal? _lowestLongPrice;
	private decimal? _highestShortPrice;
	private Sides? _pendingEntryDirection;

	/// <summary>
	/// Type of candles used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base volume for the first order in the sequence.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the previous filled volume when martingale is active.
	/// </summary>
	public decimal LotCoefficient
	{
		get => _lotCoefficient.Value;
		set => _lotCoefficient.Value = value;
	}

	/// <summary>
	/// Maximum allowed floating loss before forcing a full exit.
	/// </summary>
	public decimal MaxDrawdown
	{
		get => _maxDrawdown.Value;
		set => _maxDrawdown.Value = value;
	}

	/// <summary>
	/// Floating profit target that triggers closing all positions.
	/// </summary>
	public decimal TargetProfit
	{
		get => _targetProfit.Value;
		set => _targetProfit.Value = value;
	}

	/// <summary>
	/// Period of the fast EMA inside the MACD calculation.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow EMA inside the MACD calculation.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the signal EMA that smooths MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters with defaults close to the original EA.
	/// </summary>
	public MacdFourColors2MartingaleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for MACD analysis", "General");

		_initialVolume = Param(nameof(InitialVolume), 1m)
			.SetDisplay("Initial Volume", "Volume of the first order in a martingale cycle", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_lotCoefficient = Param(nameof(LotCoefficient), 2m)
			.SetDisplay("Lot Coefficient", "Multiplier applied after a losing position", "Money management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_maxDrawdown = Param(nameof(MaxDrawdown), 50m)
			.SetDisplay("Max Drawdown", "Negative floating PnL that forces liquidation", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 20m);

		_targetProfit = Param(nameof(TargetProfit), 150m)
			.SetDisplay("Target Profit", "Positive floating PnL that locks profits", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 400m, 25m);

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetDisplay("Fast EMA Period", "Length of the fast EMA in MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 2);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetDisplay("Slow EMA Period", "Length of the slow EMA in MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(18, 40, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal Period", "Length of the signal EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Configure MACD with signal smoothing to reproduce color transitions.
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEmaPeriod },
				LongMa = { Length = SlowEmaPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Work only with finished candles.
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd)
			return;

		UpdateMacdHistory(macd);

		var floatingPnL = CalculateUnrealizedPnL(candle.ClosePrice);
		if (ShouldCloseAll(floatingPnL))
		{
			if (Position != 0)
				ClosePosition();

			ResetState();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pendingEntryDirection != null)
			return;

		if (_macdHistory.Count < 3)
			return;

		var prev2Color = _colorHistory[^3];
		var prevColor = _colorHistory[^2];
		var prevMacd = _macdHistory[^2];

		var buySignal = prev2Color == 1 && prevColor == 4 && prevMacd < 0m;
		var sellSignal = prev2Color == 2 && prevColor == 3 && prevMacd > 0m;

		if (buySignal)
		{
			if (Position < 0)
				return;

			var price = candle.ClosePrice;
			if (_lowestLongPrice.HasValue && price >= _lowestLongPrice.Value)
				return;

			var volume = CalculateNextVolume();
			if (volume <= 0m)
				return;

			_pendingEntryDirection = Sides.Buy;
			BuyMarket(volume);
		}
		else if (sellSignal)
		{
			if (Position > 0)
				return;

			var price = candle.ClosePrice;
			if (_highestShortPrice.HasValue && price <= _highestShortPrice.Value)
				return;

			var volume = CalculateNextVolume();
			if (volume <= 0m)
				return;

			_pendingEntryDirection = Sides.Sell;
			SellMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
			return;

		var direction = trade.Order.Side;
		var volume = trade.Trade.Volume;

		if (_pendingEntryDirection != null && direction == _pendingEntryDirection && volume > 0m)
		{
			_lastVolume = volume;

			if (direction == Sides.Buy)
			{
				var price = trade.Trade.Price;
				_lowestLongPrice = _lowestLongPrice.HasValue
					? Math.Min(_lowestLongPrice.Value, price)
					: price;
				_highestShortPrice = null;
			}
			else if (direction == Sides.Sell)
			{
				var price = trade.Trade.Price;
				_highestShortPrice = _highestShortPrice.HasValue
					? Math.Max(_highestShortPrice.Value, price)
					: price;
				_lowestLongPrice = null;
			}

			_pendingEntryDirection = null;
		}
		else
		{
			if (Position == 0)
			{
				ResetState();
			}
			else if (Position > 0)
			{
				_highestShortPrice = null;
			}
			else if (Position < 0)
			{
				_lowestLongPrice = null;
			}
		}
	}

	private void UpdateMacdHistory(decimal macd)
	{
		var color = 0;
		if (_macdHistory.Count > 0)
		{
			var prev = _macdHistory[^1];
			if (macd > prev && macd < 0m)
				color = 1;
			else if (macd > prev && macd > 0m)
				color = 2;
			else if (macd < prev && macd > 0m)
				color = 3;
			else if (macd < prev && macd < 0m)
				color = 4;
		}

		_macdHistory.Add(macd);
		_colorHistory.Add(color);

		const int maxHistory = 10;
		if (_macdHistory.Count > maxHistory)
		{
			_macdHistory.RemoveAt(0);
			_colorHistory.RemoveAt(0);
		}
	}

	private decimal CalculateNextVolume()
	{
		var volume = _lastVolume <= 0m ? InitialVolume : _lastVolume;
		if (volume <= 0m)
			return 0m;

		if (_lastVolume > 0m && LotCoefficient > 0m)
			volume *= LotCoefficient;

		return volume;
	}

	private bool ShouldCloseAll(decimal floatingPnL)
	{
		if (MaxDrawdown != 0m)
		{
			if (MaxDrawdown > 0m && floatingPnL <= -MaxDrawdown)
				return true;

			if (MaxDrawdown < 0m && floatingPnL <= MaxDrawdown)
				return true;
		}

		if (TargetProfit != 0m)
		{
			if (TargetProfit > 0m && floatingPnL >= TargetProfit)
				return true;

			if (TargetProfit < 0m && floatingPnL >= -TargetProfit)
				return true;
		}

		return false;
	}

	private decimal CalculateUnrealizedPnL(decimal price)
	{
		var priceStep = Security.PriceStep ?? 0m;
		var stepPrice = Security.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m || Position == 0)
			return 0m;

		var diff = price - PositionAvgPrice;
		var steps = diff / priceStep;
		return steps * stepPrice * Position;
	}

	private void ResetState()
	{
		_lastVolume = 0m;
		_lowestLongPrice = null;
		_highestShortPrice = null;
		_pendingEntryDirection = null;
		_macdHistory.Clear();
		_colorHistory.Clear();
	}
}
