using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class LabouchereEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _lotSequence;
	private readonly StrategyParam<bool> _newRecycle;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<bool> _isReversed;
	private readonly StrategyParam<bool> _useOppositeExit;
	private readonly StrategyParam<bool> _useWorkTime;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _stopTime;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;

	private List<decimal> _sequence = new();
	private List<decimal> _initialSequence = new();
	private decimal? _prevK;
	private decimal? _prevD;
	private decimal _openPnL;
	private Order _stopOrder;
	private Order _tpOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public string LotSequence { get => _lotSequence.Value; set => _lotSequence.Value = value; }
	public bool NewRecycle { get => _newRecycle.Value; set => _newRecycle.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public bool IsReversed { get => _isReversed.Value; set => _isReversed.Value = value; }
	public bool UseOppositeExit { get => _useOppositeExit.Value; set => _useOppositeExit.Value = value; }
	public bool UseWorkTime { get => _useWorkTime.Value; set => _useWorkTime.Value = value; }
	public TimeSpan StartTime { get => _startTime.Value; set => _startTime.Value = value; }
	public TimeSpan StopTime { get => _stopTime.Value; set => _stopTime.Value = value; }
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }

	public LabouchereEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "General");

		_lotSequence = Param(nameof(LotSequence), "0.01,0.02,0.01,0.02,0.01,0.01,0.01,0.01")
			.SetDisplay("Lot Sequence", "Comma separated initial lot sequence", "Money Management");

		_newRecycle = Param(nameof(NewRecycle), true)
			.SetDisplay("Recycle Sequence", "Restart sequence when completed", "Money Management");

		_stopLoss = Param(nameof(StopLoss), 40)
			.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 50)
			.SetDisplay("Take Profit", "Take profit in price steps", "Risk Management");

		_isReversed = Param(nameof(IsReversed), false)
			.SetDisplay("Reverse Signals", "Reverse indicator signals", "Signals");

		_useOppositeExit = Param(nameof(UseOppositeExit), false)
			.SetDisplay("Exit On Opposite", "Close position on opposite signal", "Signals");

		_useWorkTime = Param(nameof(UseWorkTime), false)
			.SetDisplay("Use Work Time", "Enable trading time filter", "Time");

		_startTime = Param(nameof(StartTime), TimeSpan.Zero)
			.SetDisplay("Start Time", "Allowed trading start time", "Time");

		_stopTime = Param(nameof(StopTime), TimeSpan.FromHours(24))
			.SetDisplay("Stop Time", "Allowed trading end time", "Time");

		_kPeriod = Param(nameof(KPeriod), 10)
			.SetDisplay("K Period", "Stochastic %K period", "Indicator");

		_dPeriod = Param(nameof(DPeriod), 190)
			.SetDisplay("D Period", "Stochastic %D period", "Indicator");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sequence = ParseSequence(LotSequence);
		_initialSequence = new List<decimal>(_sequence);

		var stoch = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsTradingTime(candle.OpenTime))
			return;

		var stoch = (StochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		var signal = 0;

		if (_prevK.HasValue && _prevD.HasValue)
		{
			if (_prevK <= _prevD && k > d)
				signal = 1;
			else if (_prevK >= _prevD && k < d)
				signal = -1;
		}

		_prevK = k;
		_prevD = d;

		if (IsReversed)
			signal = -signal;

		if (UseOppositeExit && signal != 0)
		{
			if (Position > 0 && signal < 0)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (Position < 0 && signal > 0)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}

		if (Position != 0 || signal == 0)
			return;

		if (_sequence.Count <= 1)
		{
			if (NewRecycle)
			{
				_sequence = new List<decimal>(_initialSequence);
			}
			else
			{
				return;
			}
		}

		var volume = _sequence[0] + _sequence[^1];

		_openPnL = PnL;

		if (signal > 0)
		{
			BuyMarket(volume);
			RegisterProtection(true, candle.ClosePrice, volume);
		}
		else
		{
			SellMarket(volume);
			RegisterProtection(false, candle.ClosePrice, volume);
		}
	}

	private bool IsTradingTime(DateTimeOffset time)
	{
		if (!UseWorkTime)
			return true;

		var t = time.TimeOfDay;

		var start = StartTime;
		var stop = StopTime;

		if (start == stop)
			return true;

		if (start < stop)
			return t >= start && t <= stop;

		return t >= start || t <= stop;
	}

	private void RegisterProtection(bool isLong, decimal price, decimal volume)
	{
		if (StopLoss <= 0 && TakeProfit <= 0)
			return;

		var step = Security.PriceStep;
		var stopOffset = StopLoss > 0 ? StopLoss * step : 0m;
		var takeOffset = TakeProfit > 0 ? TakeProfit * step : 0m;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_tpOrder != null && _tpOrder.State == OrderStates.Active)
			CancelOrder(_tpOrder);

		if (stopOffset > 0)
		{
			_stopOrder = isLong
			? SellStop(volume, price - stopOffset)
			: BuyStop(volume, price + stopOffset);
		}

		if (takeOffset > 0)
		{
			_tpOrder = isLong
			? SellLimit(volume, price + takeOffset)
			: BuyLimit(volume, price - takeOffset);
		}
	}

	private List<decimal> ParseSequence(string seq)
	{
		return seq.Split(',', StringSplitOptions.RemoveEmptyEntries)
		.Select(s => decimal.Parse(s.Trim(), CultureInfo.InvariantCulture))
		.ToList();
	}

	private void AdjustSequence(bool isProfit)
	{
		if (isProfit)
		{
			if (_sequence.Count > 1)
			{
			_sequence.RemoveAt(_sequence.Count - 1);
			_sequence.RemoveAt(0);
			}

			if (_sequence.Count <= 1 && NewRecycle)
			_sequence = new List<decimal>(_initialSequence);
		}
		else
		{
			var add = _sequence[0] + _sequence[^1];
			_sequence.Add(add);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0 || delta == 0)
			return;

		var result = PnL - _openPnL;
		AdjustSequence(result > 0);

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_tpOrder != null && _tpOrder.State == OrderStates.Active)
			CancelOrder(_tpOrder);

		_stopOrder = null;
		_tpOrder = null;
	}
}
