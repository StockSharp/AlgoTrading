using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD based strategy converted from MetaTrader 5 Expert Advisor.
/// Uses MACD indicator with custom pattern checks on recent values and optional trailing stop.
/// </summary>
public class ExpertMacdEurusd1HourStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _trailingPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _main0, _main1, _main2, _main3;
	private decimal _signal0, _signal1, _signal2, _signal3;
	private decimal _longStopPrice, _shortStopPrice;
	private int _counter;

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Signal line length for MACD.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public decimal TrailingPoints { get => _trailingPoints.Value; set => _trailingPoints.Value = value; }

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy with default parameters.
	/// </summary>
	public ExpertMacdEurusd1HourStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetDisplay("Fast Length", "Fast EMA length for MACD", "Parameters")
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 15)
			.SetDisplay("Slow Length", "Slow EMA length for MACD", "Parameters")
			.SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 3)
			.SetDisplay("Signal Length", "Signal length for MACD", "Parameters")
			.SetCanOptimize(true);

		_trailingPoints = Param(nameof(TrailingPoints), 25m)
			.SetDisplay("Trailing Points", "Trailing stop distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
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
		_main0 = _main1 = _main2 = _main3 = 0m;
		_signal0 = _signal1 = _signal2 = _signal3 = 0m;
		_longStopPrice = _shortStopPrice = 0m;
		_counter = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		var value = (MovingAverageConvergenceDivergenceSignalValue)indicatorValue;
		var main = value.Macd;
		var signal = value.Signal;

		// shift stored values
		_main3 = _main2;
		_main2 = _main1;
		_main1 = _main0;
		_main0 = main;

		_signal3 = _signal2;
		_signal2 = _signal1;
		_signal1 = _signal0;
		_signal0 = signal;

		if (_counter < 3)
		{
			_counter++;
			return;
		}

		var trailOffset = TrailingPoints * (Security.PriceStep ?? 1m);

		var buySignal = _signal3 > _signal2 && _signal2 > _signal1 && _signal1 < _signal0 &&
			_main3 > _main2 && _main2 < _main1 && _main1 < _main0 &&
			_main1 < -0.00020m && _main3 < 0m && _main0 > 0.00020m;

		var sellSignal = _signal3 < _signal2 && _signal2 < _signal1 && _signal1 > _signal0 &&
			_main3 < _main2 && _main2 > _main1 && _main1 > _main0 &&
			_main1 > 0.00020m && _main3 > 0m && _main0 < -0.00035m;

		if (buySignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_longStopPrice = candle.ClosePrice - trailOffset;
			_shortStopPrice = 0m;
		}
		else if (sellSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_shortStopPrice = candle.ClosePrice + trailOffset;
			_longStopPrice = 0m;
		}

		// trailing stop management
		if (TrailingPoints > 0m)
		{
			if (Position > 0 && _longStopPrice > 0m)
			{
				var newStop = candle.ClosePrice - trailOffset;
				_longStopPrice = Math.Max(_longStopPrice, newStop);
				if (candle.LowPrice <= _longStopPrice)
				{
					SellMarket(Math.Abs(Position));
					_longStopPrice = 0m;
				}
			}
			else if (Position < 0 && _shortStopPrice > 0m)
			{
				var newStop = candle.ClosePrice + trailOffset;
				_shortStopPrice = Math.Min(_shortStopPrice, newStop);
				if (candle.HighPrice >= _shortStopPrice)
				{
					BuyMarket(Math.Abs(Position));
					_shortStopPrice = 0m;
				}
			}
		}

		// exit on MACD slope reversal
		if (Position > 0 && _main0 < _main1)
		{
			SellMarket(Math.Abs(Position));
			_longStopPrice = 0m;
		}
		else if (Position < 0 && _main0 > _main1)
		{
			BuyMarket(Math.Abs(Position));
			_shortStopPrice = 0m;
		}
	}
}
