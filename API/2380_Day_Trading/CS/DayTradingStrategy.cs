using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Day trading strategy using Parabolic SAR, MACD, Stochastic, and Momentum indicators.
/// Enters long when multiple indicators confirm upward move and short when they confirm downward move.
/// Applies trailing stop and take profit.
/// </summary>
public class DayTradingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSar;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DayTradingStrategy"/>.
	/// </summary>
	public DayTradingStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_takeProfit = Param(nameof(TakeProfit), 50m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 25m)
			.SetDisplay("Trailing Stop", "Trailing stop in points", "Risk");

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
		_prevSar = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortMa = { Length = 12 },
			LongMa = { Length = 26 },
			SignalMa = { Length = 9 }
		};

		var stochastic = new StochasticOscillator
		{
			K = { Length = 5 },
			D = { Length = 3 },
			Smooth = 3
		};

		var parabolicSar = new ParabolicSar
		{
			Acceleration = 0.02m,
			AccelerationMax = 0.2m
		};

		var momentum = new Momentum { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, stochastic, parabolicSar, momentum, ProcessCandle)
			.Start();

		var pip = Security?.PriceStep ?? 1m;
		StartProtection(
			takeProfit: TakeProfit > 0 ? new Unit(TakeProfit * pip, UnitTypes.Absolute) : default,
			stopLoss: TrailingStop > 0 ? new Unit(TrailingStop * pip, UnitTypes.Absolute) : default,
			isStopTrailing: TrailingStop > 0
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochValue, IIndicatorValue sarValue, IIndicatorValue momValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal macdSignal)
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal stochK || stochTyped.D is not decimal stochD)
			return;

		var sar = sarValue.ToDecimal();
		var momentum = momValue.ToDecimal();

		var isBuying = sar <= candle.ClosePrice && _prevSar > sar && momentum < 100m && macd < macdSignal && stochK < 35m;
		var isSelling = sar >= candle.ClosePrice && _prevSar < sar && momentum > 100m && macd > macdSignal && stochK > 60m;

		_prevSar = sar;

		if (Position == 0)
		{
			if (isBuying)
			{
				BuyMarket(TradeVolume);
			}
			else if (isSelling)
			{
				SellMarket(TradeVolume);
			}
		}
		else if (Position > 0 && isSelling)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && isBuying)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
