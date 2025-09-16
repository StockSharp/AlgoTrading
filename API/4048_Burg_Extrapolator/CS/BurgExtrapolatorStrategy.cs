using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Burg extrapolator strategy converted from MetaTrader 4 implementation.
/// Predicts the future price path with Burg linear prediction coefficients and trades on forecasted extremes.
/// </summary>
public class BurgExtrapolatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _maxRisk;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _minProfit;
	private readonly StrategyParam<int> _maxLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _pastBars;
	private readonly StrategyParam<decimal> _modelOrder;
	private readonly StrategyParam<bool> _useMomentum;
	private readonly StrategyParam<bool> _useRateOfChange;

	private readonly List<decimal> _openHistory = new();

	private double[] _samples = Array.Empty<double>();
	private double[] _coefficients = Array.Empty<double>();
	private double[] _predictions = Array.Empty<double>();

	private int _np;
	private int _no;
	private int _nf;

	private double _averagePrice;
	private bool _isFirstRun = true;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longHigh;
	private decimal? _shortLow;

	/// <summary>
	/// Initializes a new instance of the <see cref="BurgExtrapolatorStrategy"/> class.
	/// </summary>
	public BurgExtrapolatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for forecasting", "General");

		_maxRisk = Param(nameof(MaxRisk), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Risk", "Risk factor controlling position scaling", "Money Management");

		_maxTrades = Param(nameof(MaxTrades), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum stacked trades per direction", "Money Management");

		_minProfit = Param(nameof(MinProfit), 160)
			.SetGreaterThanZero()
			.SetDisplay("Min Profit", "Forecasted profit in points required to open trades", "Signals");

		_maxLoss = Param(nameof(MaxLoss), 130)
			.SetGreaterThanZero()
			.SetDisplay("Max Loss", "Forecasted adverse excursion closing existing trades", "Signals");

		_takeProfit = Param(nameof(TakeProfit), 0)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Optional fixed take profit in points", "Protection")
			.SetCanOptimize();

		_stopLoss = Param(nameof(StopLoss), 180)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Optional fixed stop loss in points", "Protection")
			.SetCanOptimize();

		_trailingStop = Param(nameof(TrailingStop), 10)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing distance in points (requires stop loss)", "Protection")
			.SetCanOptimize();

		_pastBars = Param(nameof(PastBars), 200)
			.SetGreaterThanZero()
			.SetDisplay("Past Bars", "History length used for Burg model", "Forecast");

		_modelOrder = Param(nameof(ModelOrder), 0.37m)
			.SetGreaterThanZero()
			.SetDisplay("Model Order", "Fraction of past bars used as Burg order", "Forecast");

		_useMomentum = Param(nameof(UseMomentum), true)
			.SetDisplay("Use Momentum", "Use logarithmic momentum instead of raw prices", "Forecast");

		_useRateOfChange = Param(nameof(UseRateOfChange), false)
			.SetDisplay("Use ROC", "Use percentage rate of change instead of raw prices", "Forecast");
	}

	/// <summary>
	/// Type of candles processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Risk factor used when stacking positions.
	/// </summary>
	public decimal MaxRisk
	{
		get => _maxRisk.Value;
		set => _maxRisk.Value = value;
	}

	/// <summary>
	/// Maximum number of trades allowed in one direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Minimum profit in points predicted by the Burg model to initiate new trades.
	/// </summary>
	public int MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Maximum loss in points predicted by the Burg model before closing positions.
	/// </summary>
	public int MaxLoss
	{
		get => _maxLoss.Value;
		set => _maxLoss.Value = value;
	}

	/// <summary>
	/// Optional take profit expressed in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Optional stop loss expressed in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Number of historical candles used by the Burg predictor.
	/// </summary>
	public int PastBars
	{
		get => _pastBars.Value;
		set => _pastBars.Value = value;
	}

	/// <summary>
	/// Fraction of <see cref="PastBars"/> used as Burg model order.
	/// </summary>
	public decimal ModelOrder
	{
		get => _modelOrder.Value;
		set => _modelOrder.Value = value;
	}

	/// <summary>
	/// Use logarithmic momentum transformation instead of raw prices.
	/// </summary>
	public bool UseMomentum
	{
		get => _useMomentum.Value;
		set => _useMomentum.Value = value;
	}

	/// <summary>
	/// Use percentage rate of change transformation instead of raw prices.
	/// </summary>
	public bool UseRateOfChange
	{
		get => _useRateOfChange.Value;
		set => _useRateOfChange.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openHistory.Clear();
		_samples = Array.Empty<double>();
		_coefficients = Array.Empty<double>();
		_predictions = Array.Empty<double>();
		_np = 0;
		_no = 0;
		_nf = 0;
		_averagePrice = 0.0;
		_isFirstRun = true;
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenCandlesFinished(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		AddOpenPrice(candle.OpenPrice);

		if (!EnsureModel())
			return;

		TrimHistory();

		if (_openHistory.Count < _np + 1)
			return;

		if (!UpdateSamples())
			return;

		var predictionCount = ComputePredictions();
		if (predictionCount <= 0)
			return;

		var (openSignal, closeSignal) = EvaluateSignals(predictionCount);

		if (ManageProtection(candle))
		{
			// Position has been closed by protective logic, wait for the next candle to re-evaluate.
			return;
		}

		HandleSignalClosures(openSignal, closeSignal);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (openSignal == 1)
		{
			TryOpenLong(candle);
		}
		else if (openSignal == -1)
		{
			TryOpenShort(candle);
		}
	}

	private void AddOpenPrice(decimal openPrice)
	{
		_openHistory.Add(openPrice);
	}

	private bool EnsureModel()
	{
		var np = PastBars;
		if (np < 3)
			return false;

		var modelOrder = ModelOrder;
		var no = (int)(modelOrder * np);
		if (no < 1)
			no = 1;
		if (no >= np - 1)
			no = np - 2;

		var nf = np - no - 1;
		if (nf < 1)
			nf = 1;

		var predictionLength = nf + 1;

		if (_np != np || _no != no || _nf != nf || _predictions.Length != predictionLength)
		{
			_np = np;
			_no = no;
			_nf = nf;
			_samples = new double[np];
			_coefficients = new double[no + 1];
			_predictions = new double[predictionLength];
			_averagePrice = 0.0;
			_isFirstRun = true;
		}

		return true;
	}

	private void TrimHistory()
	{
		var maxHistory = _np + 1;
		while (_openHistory.Count > maxHistory)
		{
			_openHistory.RemoveAt(0);
		}
	}

	private bool UpdateSamples()
	{
		if (_np <= 0)
			return false;

		var useMomentum = UseMomentum;
		var useRoc = !useMomentum && UseRateOfChange;

		if (useMomentum || useRoc)
		{
			if (!_isFirstRun)
			{
				for (var i = 0; i < _np - 1; i++)
					_samples[i] = _samples[i + 1];

				var current = GetOpen(0);
				var previous = GetOpen(1);
				if (previous == 0m)
					return false;

				var ratio = (double)(current / previous);
				_samples[_np - 1] = useMomentum ? Math.Log(ratio) : ratio - 1.0;
			}
			else
			{
				for (var i = 0; i < _np; i++)
				{
					var current = GetOpen(i);
					var previous = GetOpen(i + 1);
					if (previous == 0m)
						return false;

					var ratio = (double)(current / previous);
					_samples[_np - 1 - i] = useMomentum ? Math.Log(ratio) : ratio - 1.0;
				}

				_averagePrice = 0.0;
				_isFirstRun = false;
			}
		}
		else
		{
			if (_isFirstRun)
			{
				double sum = 0.0;
				for (var i = 0; i < _np; i++)
					sum += (double)GetOpen(i);

				_averagePrice = sum / _np;

				for (var i = 0; i < _np; i++)
				{
					var open = (double)GetOpen(i);
					_samples[_np - 1 - i] = open - _averagePrice;
				}

				_isFirstRun = false;
			}
			else
			{
				var newest = (double)GetOpen(0);
				var leaving = (double)GetOpen(_np);
				_averagePrice += (newest - leaving) / _np;

				for (var i = 0; i < _np; i++)
				{
					var open = (double)GetOpen(i);
					_samples[_np - 1 - i] = open - _averagePrice;
				}
			}
		}

		return true;
	}

	private int ComputePredictions()
	{
		Array.Clear(_coefficients, 0, _coefficients.Length);
		Array.Clear(_predictions, 0, _predictions.Length);

		double den = 0.0;
		for (var i = 0; i < _np; i++)
		{
			var value = _samples[i];
			ten += value * value;
		}

		ten *= 2.0;

		var df = new double[_np];
		var db = new double[_np];

		for (var i = 0; i < _np; i++)
		{
			var value = _samples[i];
			df[i] = value;
			db[i] = value;
		}

		double r = 0.0;

		for (var k = 1; k <= _no; k++)
		{
			double num = 0.0;
			for (var i = k; i < _np; i++)
				num += df[i] * db[i - 1];

			var denominator = (1.0 - r * r) * den - df[k - 1] * df[k - 1] - db[_np - 1] * db[_np - 1];
			if (Math.Abs(denominator) < 1e-12)
				return 0;

			r = -2.0 * num / denominator;
			_coefficients[k] = r;

			var half = k / 2;
			for (var i = 1; i <= half; i++)
			{
				var ki = k - i;
				var tmp = _coefficients[i];
				_coefficients[i] += r * _coefficients[ki];
				if (i != ki)
					_coefficients[ki] += r * tmp;
			}

			if (k < _no)
			{
				for (var i = _np - 1; i >= k; i--)
				{
					var tmp = df[i];
					df[i] += r * db[i - 1];
					db[i] = db[i - 1] + r * tmp;
				}
			}

			ten = denominator;
		}

		for (var n = _np - 1; n < _np + _nf; n++)
		{
			double sum = 0.0;
			for (var i = 1; i <= _no; i++)
			{
				if (n - i < _np)
					sum -= _coefficients[i] * _samples[n - i];
				else
					sum -= _coefficients[i] * _predictions[n - i - _np + 1];
			}

			var index = n - _np + 1;
			if (index < _predictions.Length)
				_predictions[index] = sum;
		}

		var useMomentum = UseMomentum;
		var useRoc = !useMomentum && UseRateOfChange;

		if (useMomentum || useRoc)
		{
			var startPrice = (double)GetOpen(0);
			_predictions[0] = startPrice;

			for (var i = 1; i < _predictions.Length; i++)
			{
				_predictions[i] = useMomentum
					? _predictions[i - 1] * Math.Exp(_predictions[i])
					: _predictions[i - 1] * (1.0 + _predictions[i]);
			}
		}
		else
		{
			for (var i = 0; i < _predictions.Length; i++)
				_predictions[i] += _averagePrice;
		}

		return _predictions.Length;
	}

	private (int openSignal, int closeSignal) EvaluateSignals(int predictionCount)
	{
		if (predictionCount == 0)
			return (0, 0);

		var step = Security?.PriceStep ?? 1m;
		var maxLossDelta = MaxLoss * step;
		var minProfitDelta = MinProfit * step;

		var ymax = (decimal)_predictions[0];
		var ymin = ymax;
		var imax = 0;
		var imin = 0;
		var openSignal = 0;
		var closeSignal = 0;

		var limit = Math.Min(_np, predictionCount);

		for (var i = 1; i < limit; i++)
		{
			var value = (decimal)_predictions[i];

			if (value > ymax && openSignal == 0)
			{
				ymax = value;
				imax = i;

				if (imin == 0 && ymax - ymin >= maxLossDelta)
					closeSignal = 1;

				if (imin == 0 && ymax - ymin >= minProfitDelta)
					openSignal = 1;
			}

			if (value < ymin && openSignal == 0)
			{
				ymin = value;
				imin = i;

				if (imax == 0 && ymax - ymin >= maxLossDelta)
					closeSignal = -1;

				if (imax == 0 && ymax - ymin >= minProfitDelta)
					openSignal = -1;
			}
		}

		return (openSignal, closeSignal);
	}

	private bool ManageProtection(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLoss * step;
		var takeDistance = TakeProfit * step;
		var trailingDistance = TrailingStop * step;

		if (Position > 0)
		{
			_longEntryPrice ??= candle.ClosePrice;
			_longHigh = _longHigh.HasValue ? Math.Max(_longHigh.Value, candle.HighPrice) : candle.HighPrice;

			if (StopLoss > 0 && _longEntryPrice.HasValue && candle.LowPrice <= _longEntryPrice.Value - stopDistance)
			{
				SellMarket(Position);
				ResetLongState();
				return true;
			}

			if (TakeProfit > 0 && _longEntryPrice.HasValue && candle.HighPrice >= _longEntryPrice.Value + takeDistance)
			{
				SellMarket(Position);
				ResetLongState();
				return true;
			}

			if (TrailingStop > 0 && StopLoss > 0 && _longHigh.HasValue && candle.LowPrice <= _longHigh.Value - trailingDistance)
			{
				SellMarket(Position);
				ResetLongState();
				return true;
			}
		}
		else
		{
			ResetLongState();
		}

		if (Position < 0)
		{
			_shortEntryPrice ??= candle.ClosePrice;
			_shortLow = _shortLow.HasValue ? Math.Min(_shortLow.Value, candle.LowPrice) : candle.LowPrice;

			if (StopLoss > 0 && _shortEntryPrice.HasValue && candle.HighPrice >= _shortEntryPrice.Value + stopDistance)
			{
				BuyMarket(-Position);
				ResetShortState();
				return true;
			}

			if (TakeProfit > 0 && _shortEntryPrice.HasValue && candle.LowPrice <= _shortEntryPrice.Value - takeDistance)
			{
				BuyMarket(-Position);
				ResetShortState();
				return true;
			}

			if (TrailingStop > 0 && StopLoss > 0 && _shortLow.HasValue && candle.HighPrice >= _shortLow.Value + trailingDistance)
			{
				BuyMarket(-Position);
				ResetShortState();
				return true;
			}
		}
		else
		{
			ResetShortState();
		}

		return false;
	}

	private void HandleSignalClosures(int openSignal, int closeSignal)
	{
		if (Position > 0 && (closeSignal == -1 || openSignal == -1))
		{
			SellMarket(Position);
			ResetLongState();
		}
		else if (Position < 0 && (closeSignal == 1 || openSignal == 1))
		{
			BuyMarket(-Position);
			ResetShortState();
		}
	}

	private void TryOpenLong(ICandleMessage candle)
	{
		var baseVolume = Volume;
		if (baseVolume <= 0m)
			return;

		var tradeCount = GetTradeCount(baseVolume);
		if (tradeCount >= MaxTrades)
			return;

		var orderVolume = CalculateOrderVolume(baseVolume, tradeCount);
		if (orderVolume <= 0m)
			return;

		BuyMarket(orderVolume);
		_longEntryPrice = candle.ClosePrice;
		_longHigh = candle.ClosePrice;
		ResetShortState();
	}

	private void TryOpenShort(ICandleMessage candle)
	{
		var baseVolume = Volume;
		if (baseVolume <= 0m)
			return;

		var tradeCount = GetTradeCount(baseVolume);
		if (tradeCount >= MaxTrades)
			return;

		var orderVolume = CalculateOrderVolume(baseVolume, tradeCount);
		if (orderVolume <= 0m)
			return;

		SellMarket(orderVolume);
		_shortEntryPrice = candle.ClosePrice;
		_shortLow = candle.ClosePrice;
		ResetLongState();
	}

	private int GetTradeCount(decimal baseVolume)
	{
		if (baseVolume <= 0m)
			return 0;

		var trades = Math.Abs(Position) / baseVolume;
		return (int)Math.Ceiling((double)(trades - 1e-8m));
	}

	private decimal CalculateOrderVolume(decimal baseVolume, int existingTrades)
	{
		var multiplier = 1m + existingTrades * MaxRisk;
		if (multiplier <= 0m)
			return 0m;

		return baseVolume * multiplier;
	}

	private decimal GetOpen(int shift)
	{
		var index = _openHistory.Count - 1 - shift;
		return index >= 0 && index < _openHistory.Count ? _openHistory[index] : 0m;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longHigh = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortLow = null;
	}
}
