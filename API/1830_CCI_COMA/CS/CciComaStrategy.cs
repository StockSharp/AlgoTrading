using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI with multi-timeframe moving average alignment.
/// </summary>
public class CciComaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	private readonly TimeSpan[] _timeframes = new TimeSpan[]
	{
		TimeSpan.FromMinutes(5),
		TimeSpan.FromMinutes(15),
		TimeSpan.FromMinutes(30),
		TimeSpan.FromHours(1),
		TimeSpan.FromHours(4),
		TimeSpan.FromDays(1)
	};

	private readonly bool[] _trendUp;
	private readonly bool[] _trendDown;
	private readonly bool[] _ready;

	private CommodityChannelIndex _cci = null!;
	private RelativeStrengthIndex _rsi = null!;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// CCI length.
	/// </summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CciComaStrategy"/> class.
	/// </summary>
	public CciComaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cciPeriod = Param(nameof(CciPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation length", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Indicators");

		_trendUp = new bool[_timeframes.Length];
		_trendDown = new bool[_timeframes.Length];
		_ready = new bool[_timeframes.Length];
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var mainSub = SubscribeCandles(CandleType);
		mainSub
			.Bind(_cci, _rsi, ProcessMainCandle)
			.Start();

		for (var i = 0; i < _timeframes.Length; i++)
		{
			var fast = new SimpleMovingAverage { Length = 1 };
			var slow = new SimpleMovingAverage { Length = 2 };
			var index = i;

			SubscribeCandles(_timeframes[i].TimeFrame())
				.Bind(fast, slow, (c, f, s) => ProcessTrendCandle(index, c, f, s))
				.Start();
		}
	}

	private void ProcessTrendCandle(int index, ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_trendUp[index] = fast >= slow;
		_trendDown[index] = fast <= slow;
		_ready[index] = true;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal cci, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_cci.IsFormed || !_rsi.IsFormed)
			return;

		var allUp = true;
		var allDown = true;
		for (var i = 0; i < _timeframes.Length; i++)
		{
			if (!_ready[i])
				return;

			if (!_trendUp[i])
				allUp = false;
			if (!_trendDown[i])
				allDown = false;
		}

		var longSignal = cci >= 0m && rsi > 50m && candle.ClosePrice > candle.OpenPrice && allUp;
		var shortSignal = cci <= 0m && rsi < 50m && candle.ClosePrice < candle.OpenPrice && allDown;

		if (longSignal && Position <= 0)
			BuyMarket();
		else if (shortSignal && Position >= 0)
			SellMarket();
	}
}
