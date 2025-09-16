namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Moving average crossover strategy with RSI confirmation and martingale sizing.
/// </summary>
public class TwoMaRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _balanceDivider;
	private readonly StrategyParam<int> _maxDoublings;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage? _fastEma;
	private ExponentialMovingAverage? _slowEma;
	private RelativeStrengthIndex? _rsi;

	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private int _martingaleStage;
	private bool _isClosing;

	/// <summary>
	/// Initializes a new instance of the <see cref="TwoMaRsiStrategy"/> class.
	/// </summary>
	public TwoMaRsiStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetDisplay("Fast EMA Length", "Length of the fast exponential moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_slowLength = Param(nameof(SlowLength), 20)
			.SetDisplay("Slow EMA Length", "Length of the slow exponential moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "Number of bars for the RSI calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Upper RSI threshold for short entries", "Signals");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Lower RSI threshold for long entries", "Signals");

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetDisplay("Stop Loss (points)", "Stop loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1500m)
			.SetDisplay("Take Profit (points)", "Take profit distance in price steps", "Risk");

		_balanceDivider = Param(nameof(BalanceDivider), 1000m)
			.SetDisplay("Balance Divider", "Divides portfolio value to estimate base order volume", "Money Management");

		_maxDoublings = Param(nameof(MaxDoublings), 1)
			.SetDisplay("Max Doublings", "Maximum number of martingale doublings", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series for the strategy", "General");
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Overbought threshold for RSI.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Oversold threshold for RSI.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Divider applied to the portfolio value to calculate the base order volume.
	/// </summary>
	public decimal BalanceDivider
	{
		get => _balanceDivider.Value;
		set => _balanceDivider.Value = value;
	}

	/// <summary>
	/// Maximum number of martingale doublings.
	/// </summary>
	public int MaxDoublings
	{
		get => _maxDoublings.Value;
		set => _maxDoublings.Value = value;
	}

	/// <summary>
	/// Candle data type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastEma = null;
		_slowEma = null;
		_rsi = null;
		_previousFast = null;
		_previousSlow = null;
		_entryPrice = default;
		_stopPrice = default;
		_takeProfitPrice = default;
		_martingaleStage = 0;
		_isClosing = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new ExponentialMovingAverage
		{
			Length = FastLength
		};

		_slowEma = new ExponentialMovingAverage
		{
			Length = SlowLength
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0 && _isClosing)
		{
			_isClosing = false;
			_entryPrice = default;
			_stopPrice = default;
			_takeProfitPrice = default;
		}

		if (_fastEma == null || _slowEma == null || _rsi == null)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_rsi.IsFormed)
		{
			_previousFast = fast;
			_previousSlow = slow;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousFast = fast;
			_previousSlow = slow;
			return;
		}

		var point = GetPoint();

		if (Position > 0)
		{
			var stopHit = candle.LowPrice <= _stopPrice;
			var takeHit = candle.HighPrice >= _takeProfitPrice;

			if (!_isClosing && stopHit)
			{
				_isClosing = true;
				ClosePosition();
				RegisterLoss();
			}
			else if (!_isClosing && takeHit)
			{
				_isClosing = true;
				ClosePosition();
				RegisterWin();
			}
		}
		else if (Position < 0)
		{
			var stopHit = candle.HighPrice >= _stopPrice;
			var takeHit = candle.LowPrice <= _takeProfitPrice;

			if (!_isClosing && stopHit)
			{
				_isClosing = true;
				ClosePosition();
				RegisterLoss();
			}
			else if (!_isClosing && takeHit)
			{
				_isClosing = true;
				ClosePosition();
				RegisterWin();
			}
		}
		else if (!_isClosing)
		{
			if (_previousFast is null || _previousSlow is null)
			{
				_previousFast = fast;
				_previousSlow = slow;
				return;
			}

			var prevFast = _previousFast.Value;
			var prevSlow = _previousSlow.Value;

			var crossUp = prevFast < prevSlow && fast > slow && rsi < RsiOversold;
			var crossDown = prevFast > prevSlow && fast < slow && rsi > RsiOverbought;

			if (crossUp)
			{
				var volume = CalculateOrderVolume();
				if (volume > 0m)
				{
					BuyMarket(volume);
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice - StopLossPoints * point;
					_takeProfitPrice = _entryPrice + TakeProfitPoints * point;
				}
			}
			else if (crossDown)
			{
				var volume = CalculateOrderVolume();
				if (volume > 0m)
				{
					SellMarket(volume);
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice + StopLossPoints * point;
					_takeProfitPrice = _entryPrice - TakeProfitPoints * point;
				}
			}
		}

		_previousFast = fast;
		_previousSlow = slow;
	}

	private decimal GetPoint()
	{
		var step = Security?.PriceStep ?? 1m;
		return step > 0m ? step : 1m;
	}

	private decimal CalculateOrderVolume()
	{
		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var baseVolume = step;
		var divider = BalanceDivider;
		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (divider > 0m && balance > 0m)
		{
			var count = Math.Floor((double)(balance / divider));
			baseVolume = (decimal)count * step;
			if (baseVolume < step)
				baseVolume = step;
		}

		var multiplier = CalculateMartingaleMultiplier();
		var volume = baseVolume * multiplier;

		if (volume < step)
			volume = step;

		var ratio = volume / step;
		volume = Math.Ceiling(ratio) * step;

		return volume;
	}

	private decimal CalculateMartingaleMultiplier()
	{
		if (MaxDoublings <= 0 || _martingaleStage <= 0)
			return 1m;

		var stage = Math.Min(_martingaleStage, MaxDoublings);
		return (decimal)Math.Pow(2d, stage);
	}

	private void RegisterWin()
	{
		_martingaleStage = 0;
	}

	private void RegisterLoss()
	{
		if (MaxDoublings <= 0)
		{
			_martingaleStage = 0;
			return;
		}

		if (_martingaleStage < MaxDoublings)
		{
			_martingaleStage++;
		}
		else
		{
			_martingaleStage = 0;
		}
	}
}
