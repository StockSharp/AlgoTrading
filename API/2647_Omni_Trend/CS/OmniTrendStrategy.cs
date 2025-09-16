using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy that replicates the Omni Trend MetaTrader expert.
/// </summary>
public class OmniTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MovingAverageMethod> _maType;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _volatilityFactor;
	private readonly StrategyParam<decimal> _moneyRisk;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyOpen;
	private readonly StrategyParam<bool> _enableSellOpen;
	private readonly StrategyParam<bool> _enableBuyClose;
	private readonly StrategyParam<bool> _enableSellClose;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private readonly Queue<SignalInfo> _pendingSignals = new();

	private IIndicator? _ma;
	private AverageTrueRange? _atr;
	private decimal _previousSmin;
	private decimal _previousSmax;
	private decimal _previousTrendUp;
	private decimal _previousTrendDown;
	private int _previousTrend;
	private bool _isInitialized;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	public OmniTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to build Omni Trend signals", "General")
			.SetCanOptimize();

		_maLength = Param(nameof(MaLength), 13)
			.SetDisplay("MA Length", "Moving average period", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_maType = Param(nameof(MaType), MovingAverageMethod.Exponential)
			.SetDisplay("MA Type", "Moving average calculation method", "Indicators")
			.SetCanOptimize();

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Close)
			.SetDisplay("Applied Price", "Price field used by the moving average", "Indicators")
			.SetCanOptimize();

		_atrLength = Param(nameof(AtrLength), 11)
			.SetDisplay("ATR Length", "ATR period for volatility bands", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_volatilityFactor = Param(nameof(VolatilityFactor), 1.3m)
			.SetDisplay("Volatility Factor", "Multiplier applied to ATR", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_moneyRisk = Param(nameof(MoneyRisk), 0.15m)
			.SetDisplay("Money Risk", "Offset factor used to position trend bands", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Delay in bars before acting on a signal", "Trading")
			.SetCanOptimize();

		_enableBuyOpen = Param(nameof(EnableBuyOpen), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableSellOpen = Param(nameof(EnableSellOpen), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_enableBuyClose = Param(nameof(EnableBuyClose), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions", "Trading");

		_enableSellClose = Param(nameof(EnableSellClose), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take Profit (points)", "Profit target distance expressed in price steps", "Risk");

		Volume = 1m;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = Math.Max(1, value);
	}

	public MovingAverageMethod MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	public AppliedPriceType AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = Math.Max(1, value);
	}

	public decimal VolatilityFactor
	{
		get => _volatilityFactor.Value;
		set => _volatilityFactor.Value = value;
	}

	public decimal MoneyRisk
	{
		get => _moneyRisk.Value;
		set => _moneyRisk.Value = value;
	}

	public int SignalBar
	{
		get => Math.Max(0, _signalBar.Value);
		set => _signalBar.Value = Math.Max(0, value);
	}

	public bool EnableBuyOpen
	{
		get => _enableBuyOpen.Value;
		set => _enableBuyOpen.Value = value;
	}

	public bool EnableSellOpen
	{
		get => _enableSellOpen.Value;
		set => _enableSellOpen.Value = value;
	}

	public bool EnableBuyClose
	{
		get => _enableBuyClose.Value;
		set => _enableBuyClose.Value = value;
	}

	public bool EnableSellClose
	{
		get => _enableSellClose.Value;
		set => _enableSellClose.Value = value;
	}

	public int StopLossPoints
	{
		get => Math.Max(0, _stopLossPoints.Value);
		set => _stopLossPoints.Value = Math.Max(0, value);
	}

	public int TakeProfitPoints
	{
		get => Math.Max(0, _takeProfitPoints.Value);
		set => _takeProfitPoints.Value = Math.Max(0, value);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_pendingSignals.Clear();
		_ma = null;
		_atr = null;
		_previousSmin = 0m;
		_previousSmax = 0m;
		_previousTrendUp = 0m;
		_previousTrendDown = 0m;
		_previousTrend = 0;
		_isInitialized = false;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = CreateMovingAverage(MaType, MaLength);
		_atr = new AverageTrueRange
		{
			Length = AtrLength,
			Type = MovingAverageType.Exponential
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_ma is not null)
				DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_ma is null || _atr is null)
			return;

		var atrValue = _atr.Process(new CandleIndicatorValue(_atr, candle));
		var appliedPrice = GetAppliedPrice(candle, AppliedPrice);
		var maValue = _ma.Process(new DecimalIndicatorValue(_ma, appliedPrice));

		if (!atrValue.IsFinal || !maValue.IsFinal)
			return;

		CheckRiskManagement(candle);

		var atr = atrValue.GetValue<decimal>();
		var ma = maValue.GetValue<decimal>();
		var signal = CalculateSignal(candle, ma, atr);

		_pendingSignals.Enqueue(signal);
		while (_pendingSignals.Count > SignalBar)
		{
			var pending = _pendingSignals.Dequeue();
			ExecuteSignal(candle, pending);
		}
	}

	private SignalInfo CalculateSignal(ICandleMessage candle, decimal ma, decimal atr)
	{
		var smax = ma + VolatilityFactor * atr;
		var smin = ma - VolatilityFactor * atr;

		if (!_isInitialized)
		{
			_previousSmax = smax;
			_previousSmin = smin;
			_previousTrendUp = 0m;
			_previousTrendDown = 0m;
			_previousTrend = 0;
			_isInitialized = true;
			return SignalInfo.Empty;
		}

		var trend = _previousTrend;
		if (candle.HighPrice > _previousSmax)
			trend = 1;
		else if (candle.LowPrice < _previousSmin)
			trend = -1;

		decimal? trendUp = null;
		decimal? trendDown = null;

		if (trend > 0)
		{
			if (smin < _previousSmin)
				smin = _previousSmin;

			var candidate = smin - (MoneyRisk - 1m) * atr;
			if (_previousTrend > 0 && _previousTrendUp > 0m && candidate < _previousTrendUp)
				candidate = _previousTrendUp;

			trendUp = candidate;
		}
		else if (trend < 0)
		{
			if (smax > _previousSmax)
				smax = _previousSmax;

			var candidate = smax + (MoneyRisk - 1m) * atr;
			if (_previousTrend < 0 && _previousTrendDown > 0m && candidate > _previousTrendDown)
				candidate = _previousTrendDown;

			trendDown = candidate;
		}

		var signal = SignalInfo.Empty;

		if (trend > 0)
		{
			if (_previousTrend <= 0 && trendUp.HasValue && EnableBuyOpen)
				signal.BuyOpen = true;

			if (trendUp.HasValue && EnableSellClose)
				signal.SellClose = true;
		}
		else if (trend < 0)
		{
			if (_previousTrend >= 0 && trendDown.HasValue && EnableSellOpen)
				signal.SellOpen = true;

			if (trendDown.HasValue && EnableBuyClose)
				signal.BuyClose = true;
		}

		_previousTrend = trend;
		_previousSmax = smax;
		_previousSmin = smin;
		_previousTrendUp = trendUp ?? 0m;
		_previousTrendDown = trendDown ?? 0m;

		return signal;
	}

	private void ExecuteSignal(ICandleMessage candle, SignalInfo signal)
	{
		if (signal.BuyClose && Position > 0)
		{
			var volume = Math.Abs(Position);
			if (volume > 0)
				SellMarket(volume);
			_longEntryPrice = null;
		}

		if (signal.SellClose && Position < 0)
		{
			var volume = Math.Abs(Position);
			if (volume > 0)
				BuyMarket(volume);
			_shortEntryPrice = null;
		}

		var executionPrice = SignalBar == 0 ? candle.ClosePrice : candle.OpenPrice;

		if (signal.BuyOpen && Position <= 0)
		{
			if (Position < 0)
			{
				var volume = Math.Abs(Position);
				BuyMarket(volume);
				_shortEntryPrice = null;
			}

			BuyMarket(Volume);
			_longEntryPrice = executionPrice;
		}

		if (signal.SellOpen && Position >= 0)
		{
			if (Position > 0)
			{
				var volume = Math.Abs(Position);
				SellMarket(volume);
				_longEntryPrice = null;
			}

			SellMarket(Volume);
			_shortEntryPrice = executionPrice;
		}
	}

	private void CheckRiskManagement(ICandleMessage candle)
	{
		if (Security is null)
			return;

		var step = Security.PriceStep;
		if (step <= 0m)
			return;

		if (Position > 0)
		{
			if (StopLossPoints > 0 && _longEntryPrice.HasValue)
			{
				var stopPrice = _longEntryPrice.Value - StopLossPoints * step;
				if (candle.LowPrice <= stopPrice || candle.ClosePrice <= stopPrice)
				{
					SellMarket(Math.Abs(Position));
					_longEntryPrice = null;
					return;
				}
			}

			if (TakeProfitPoints > 0 && _longEntryPrice.HasValue)
			{
				var targetPrice = _longEntryPrice.Value + TakeProfitPoints * step;
				if (candle.HighPrice >= targetPrice || candle.ClosePrice >= targetPrice)
				{
					SellMarket(Math.Abs(Position));
					_longEntryPrice = null;
					return;
				}
			}
		}
		else if (Position < 0)
		{
			if (StopLossPoints > 0 && _shortEntryPrice.HasValue)
			{
				var stopPrice = _shortEntryPrice.Value + StopLossPoints * step;
				if (candle.HighPrice >= stopPrice || candle.ClosePrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					_shortEntryPrice = null;
					return;
				}
			}

			if (TakeProfitPoints > 0 && _shortEntryPrice.HasValue)
			{
				var targetPrice = _shortEntryPrice.Value - TakeProfitPoints * step;
				if (candle.LowPrice <= targetPrice || candle.ClosePrice <= targetPrice)
				{
					BuyMarket(Math.Abs(Position));
					_shortEntryPrice = null;
					return;
				}
			}
		}
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType type)
	{
		return type switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod type, int length)
	{
		return type switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length }
		};
	}

	private struct SignalInfo
	{
		public static readonly SignalInfo Empty = new();
		public bool BuyOpen;
		public bool BuyClose;
		public bool SellOpen;
		public bool SellClose;
	}

	public enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}

	public enum AppliedPriceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}
}
