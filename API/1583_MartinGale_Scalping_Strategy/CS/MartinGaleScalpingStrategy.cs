using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MartinGale scalping strategy based on SMA cross with pyramiding entries.
/// </summary>
public class MartinGaleScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<string> _tradingMode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxPyramids;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _prevSlow;
	private int _pyramids;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Take profit multiplier.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss multiplier.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trading mode (Long, Short, BiDir).
	/// </summary>
	public string TradingMode
	{
		get => _tradingMode.Value;
		set => _tradingMode.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum pyramid levels.
	/// </summary>
	public int MaxPyramids
	{
		get => _maxPyramids.Value;
		set => _maxPyramids.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MartinGaleScalpingStrategy"/>.
	/// </summary>
	public MartinGaleScalpingStrategy()
	{
		_fastLength = Param(nameof(FastLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length", "Length for fast SMA", "General")
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Length", "Length for slow SMA", "General")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 1.03m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Mult", "Take profit multiplier", "Risk")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 0.95m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Mult", "Stop loss multiplier", "Risk")
			.SetCanOptimize(true);

		_tradingMode = Param(nameof(TradingMode), "Long")
			.SetDisplay("Trading Mode", "Trade direction", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_maxPyramids = Param(nameof(MaxPyramids), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Pyramids", "Maximum pyramid entries", "General");
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
		_stopPrice = 0m;
		_takePrice = 0m;
		_prevSlow = 0m;
		_pyramids = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSma = new SimpleMovingAverage { Length = FastLength };
		_slowSma = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var crossover = fast > slow;
		var crossunder = fast < slow;

		if (Position == 0)
		{
			_pyramids = 0;
			if (crossover && AllowLong())
				EnterLong(candle, slow);
			else if (crossunder && AllowShort())
				EnterShort(candle, slow);
		}
		else if (Position > 0)
		{
			if ((candle.ClosePrice > _takePrice || candle.ClosePrice < _stopPrice) && crossunder)
			{
				SellMarket(Position);
				ResetLevels();
			}
			else if (crossover && AllowLong() && _pyramids < MaxPyramids)
			{
				BuyMarket();
				_pyramids++;
				UpdateLevels(candle, slow);
			}
		}
		else if (Position < 0)
		{
			if ((candle.ClosePrice > _takePrice || candle.ClosePrice < _stopPrice) && crossover)
			{
				BuyMarket(Math.Abs(Position));
				ResetLevels();
			}
			else if (crossunder && AllowShort() && _pyramids < MaxPyramids)
			{
				SellMarket();
				_pyramids++;
				UpdateLevels(candle, slow);
			}
		}

		_prevSlow = slow;
	}

	private void EnterLong(ICandleMessage candle, decimal slow)
	{
		BuyMarket();
		_pyramids = 1;
		UpdateLevels(candle, slow);
	}

	private void EnterShort(ICandleMessage candle, decimal slow)
	{
		SellMarket();
		_pyramids = 1;
		UpdateLevels(candle, slow);
	}

	private void UpdateLevels(ICandleMessage candle, decimal slow)
	{
		if (_prevSlow == 0m)
			return;

		_stopPrice = Position > 0
			? candle.ClosePrice - StopLoss * _prevSlow
			: candle.ClosePrice + StopLoss * _prevSlow;

		_takePrice = Position > 0
			? candle.ClosePrice + TakeProfit * _prevSlow
			: candle.ClosePrice - TakeProfit * _prevSlow;
	}

	private void ResetLevels()
	{
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	private bool AllowLong() => TradingMode != "Short";
	private bool AllowShort() => TradingMode != "Long";
}
