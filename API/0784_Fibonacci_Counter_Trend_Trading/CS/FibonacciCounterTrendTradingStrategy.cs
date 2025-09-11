using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci Counter-Trend Trading strategy.
/// Opens long when price falls below selected lower band and short when price rises above upper band.
/// Optionally closes positions when price crosses the VWMA basis.
/// </summary>
public class FibonacciCounterTrendTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _mult;
	private readonly StrategyParam<string> _upperLevel;
	private readonly StrategyParam<string> _lowerLevel;
	private readonly StrategyParam<bool> _exitBasis;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevBasis;
	private bool _isFirst;

	/// <summary>
	/// VWMA period.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Standard deviation multiplier.
	/// </summary>
	public decimal Mult { get => _mult.Value; set => _mult.Value = value; }

	/// <summary>
	/// Selected upper Fibonacci level.
	/// </summary>
	public string UpperLevel { get => _upperLevel.Value; set => _upperLevel.Value = value; }

	/// <summary>
	/// Selected lower Fibonacci level.
	/// </summary>
	public string LowerLevel { get => _lowerLevel.Value; set => _lowerLevel.Value = value; }

	/// <summary>
	/// Close on VWMA cross.
	/// </summary>
	public bool ExitBasis { get => _exitBasis.Value; set => _exitBasis.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="FibonacciCounterTrendTradingStrategy"/>.
	/// </summary>
	public FibonacciCounterTrendTradingStrategy()
	{
		_length = Param(nameof(Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "VWMA length", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_mult = Param(nameof(Mult), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bands Mult", "Deviation multiplier", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_upperLevel = Param(nameof(UpperLevel), "1")
			.SetDisplay("Upper Level", "Fibonacci upper level", "General")
			.SetOptions("0.236", "0.382", "0.5", "0.618", "0.764", "1");

		_lowerLevel = Param(nameof(LowerLevel), "1")
			.SetDisplay("Lower Level", "Fibonacci lower level", "General")
			.SetOptions("0.236", "0.382", "0.5", "0.618", "0.764", "1");

		_exitBasis = Param(nameof(ExitBasis), false)
			.SetDisplay("Exit at Basis", "Close when price crosses VWMA", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_prevBasis = 0;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var vwma = new VolumeWeightedMovingAverage { Length = Length };
		var std = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(vwma, std, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwma);
			DrawOwnTrades(area);
		}
	}

	private static decimal LevelToFactor(string level)
	{
		return level switch
		{
			"0.236" => 0.236m,
			"0.382" => 0.382m,
			"0.5" => 0.5m,
			"0.618" => 0.618m,
			"0.764" => 0.764m,
			_ => 1m,
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal basis, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upperFactor = LevelToFactor(UpperLevel);
		var lowerFactor = LevelToFactor(LowerLevel);

		var dev = stdValue * Mult;

		var upper = basis + dev * upperFactor;
		var lower = basis - dev * lowerFactor;

		if (_isFirst)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			_prevBasis = basis;
			_isFirst = false;
			return;
		}

		var buySignal = _prevClose >= _prevLower && candle.ClosePrice < lower;
		var sellSignal = _prevClose <= _prevUpper && candle.ClosePrice > upper;

		if (buySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		if (ExitBasis)
		{
			var crossBasis =
				(_prevClose <= _prevBasis && candle.ClosePrice > basis) ||
				(_prevClose >= _prevBasis && candle.ClosePrice < basis);

			if (crossBasis && Position != 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));
			}
		}

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
		_prevBasis = basis;
	}
}
