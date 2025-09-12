using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gemini Trend Following System.
/// Buys pullbacks in strong uptrends and exits on trend failure.
/// </summary>
public class GeminiTrendFollowingSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _sma50Length;
	private readonly StrategyParam<int> _sma200Length;
	private readonly StrategyParam<int> _rocPeriod;
	private readonly StrategyParam<decimal> _rocMinPercent;
	private readonly StrategyParam<bool> _useCatastrophicStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevSma50;
	private decimal _prevSma200;
	private decimal _prevLowest20;
	private decimal _stopLossPrice;
	private int _barsSincePullback = int.MaxValue;
	private bool _isFirstBar = true;
	private readonly Queue<decimal> _sma200Queue = new(20);

	/// <summary>
	/// 50-period SMA length.
	/// </summary>
	public int Sma50Length
	{
		get => _sma50Length.Value;
		set => _sma50Length.Value = value;
	}

	/// <summary>
	/// 200-period SMA length.
	/// </summary>
	public int Sma200Length
	{
		get => _sma200Length.Value;
		set => _sma200Length.Value = value;
	}

	/// <summary>
	/// ROC period.
	/// </summary>
	public int RocPeriod
	{
		get => _rocPeriod.Value;
		set => _rocPeriod.Value = value;
	}

	/// <summary>
	/// Minimum annual ROC percentage.
	/// </summary>
	public decimal RocMinPercent
	{
		get => _rocMinPercent.Value;
		set => _rocMinPercent.Value = value;
	}

	/// <summary>
	/// Use catastrophic stop-loss.
	/// </summary>
	public bool UseCatastrophicStop
	{
		get => _useCatastrophicStop.Value;
		set => _useCatastrophicStop.Value = value;
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
	/// Initializes a new instance of the <see cref="GeminiTrendFollowingSystemStrategy"/>.
	/// </summary>
	public GeminiTrendFollowingSystemStrategy()
	{
		_sma50Length = Param(nameof(Sma50Length), 50)
			.SetRange(10, 200)
			.SetDisplay("50 SMA Length", "Length for 50-period SMA", "Moving Averages")
			.SetCanOptimize(true);

		_sma200Length = Param(nameof(Sma200Length), 200)
			.SetRange(100, 400)
			.SetDisplay("200 SMA Length", "Length for 200-period SMA", "Moving Averages")
			.SetCanOptimize(true);

		_rocPeriod = Param(nameof(RocPeriod), 252)
			.SetRange(50, 300)
			.SetDisplay("ROC Period", "Period for Rate of Change calculation", "Performance")
			.SetCanOptimize(true);

		_rocMinPercent = Param(nameof(RocMinPercent), 15m)
			.SetRange(5m, 50m)
			.SetDisplay("Minimum Annual ROC %", "Minimum annual rate of change percent", "Performance")
			.SetCanOptimize(true);

		_useCatastrophicStop = Param(nameof(UseCatastrophicStop), true)
			.SetDisplay("Use Catastrophic Stop", "Enable catastrophic stop below 200 SMA", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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

		_prevClose = 0m;
		_prevSma50 = 0m;
		_prevSma200 = 0m;
		_prevLowest20 = 0m;
		_stopLossPrice = 0m;
		_barsSincePullback = int.MaxValue;
		_isFirstBar = true;
		_sma200Queue.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma50 = new SimpleMovingAverage { Length = Sma50Length };
		var sma200 = new SimpleMovingAverage { Length = Sma200Length };
		var roc = new RateOfChange { Length = RocPeriod };
		var lowest20 = new Lowest { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma50, sma200, roc, lowest20, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma50Value, decimal sma200Value, decimal rocValue, decimal lowest20Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isFirstBar)
		{
			_prevClose = candle.ClosePrice;
			_prevSma50 = sma50Value;
			_prevSma200 = sma200Value;
			_prevLowest20 = lowest20Value;
			_isFirstBar = false;
			_sma200Queue.Enqueue(sma200Value);
			return;
		}

		var pulledBackTo50 = _prevClose > _prevSma50 && candle.ClosePrice < sma50Value;
		if (pulledBackTo50)
			_barsSincePullback = 0;
		else if (_barsSincePullback < int.MaxValue)
			_barsSincePullback++;

		var recoveredAbove50 = _prevClose < _prevSma50 && candle.ClosePrice > sma50Value;
		var sma200TwentyAgo = _sma200Queue.Count == 20 ? _sma200Queue.Peek() : 0m;
		var isMajorUptrend = _sma200Queue.Count == 20 && sma50Value > sma200Value && rocValue > RocMinPercent && sma200Value > sma200TwentyAgo;
		var entryCondition = isMajorUptrend && recoveredAbove50 && _barsSincePullback < 10;

		if (entryCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);

			if (UseCatastrophicStop)
				_stopLossPrice = Math.Min(_prevLowest20, _prevSma200);
		}

		if (UseCatastrophicStop && Position > 0 && candle.LowPrice <= _stopLossPrice)
		{
			SellMarket(Position);
		}

		var trendExit = _prevSma50 > _prevSma200 && sma50Value < sma200Value;
		if (trendExit && Position > 0)
		{
			SellMarket(Position);
		}

		_sma200Queue.Enqueue(sma200Value);
		if (_sma200Queue.Count > 20)
			_sma200Queue.Dequeue();

		_prevClose = candle.ClosePrice;
		_prevSma50 = sma50Value;
		_prevSma200 = sma200Value;
		_prevLowest20 = lowest20Value;
	}
}
