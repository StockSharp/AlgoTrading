using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MovingAverageEntanglementStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<decimal> _deadZonePercentage;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _fastMa = null!;
	private MovingAverage _slowMa = null!;
	private AverageTrueRange _atr = null!;

	private bool _prevBuyCondition;
	private bool _prevSellCondition;

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public int VolatilityPeriod
	{
		get => _volatilityPeriod.Value;
		set => _volatilityPeriod.Value = value;
	}

	public decimal DeadZonePercentage
	{
		get => _deadZonePercentage.Value;
		set => _deadZonePercentage.Value = value;
	}

	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
	}

	public MaType MaTypeParam
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MovingAverageEntanglementStrategy()
	{
		_fastLength = Param(nameof(FastLength), 3)
			.SetDisplay("Fast MA", "Fast MA length", "Indicators")
			.SetGreaterThanZero();

		_slowLength = Param(nameof(SlowLength), 14)
			.SetDisplay("Slow MA", "Slow MA length", "Indicators")
			.SetGreaterThanZero();

		_atrLength = Param(nameof(AtrLength), 10)
			.SetDisplay("ATR Length", "ATR length", "Indicators")
			.SetGreaterThanZero();

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 10)
			.SetDisplay("Volatility Period", "Length for gap MA", "Indicators")
			.SetGreaterThanZero();

		_deadZonePercentage = Param(nameof(DeadZonePercentage), 40m)
			.SetDisplay("Dead Zone %", "ATR dead zone percentage", "Trading")
			.SetGreaterThanZero();

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.5m)
			.SetDisplay("Deviation Multiplier", "Std dev multiplier", "Trading");

		_maType = Param(nameof(MaTypeParam), MaType.Sma)
			.SetDisplay("MA Type", "Type of moving average", "Indicators");

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
		_prevBuyCondition = false;
		_prevSellCondition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = CreateMa(MaTypeParam, FastLength);
		_slowMa = CreateMa(MaTypeParam, SlowLength);
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var gapping = Math.Abs(fastMaValue - slowMaValue);
		var atrDeadZone = atrValue * DeadZonePercentage * 0.01m;

		var buyCondition = fastMaValue > slowMaValue && gapping > atrDeadZone;
		var sellCondition = fastMaValue < slowMaValue && gapping > atrDeadZone;

		if (buyCondition && !_prevBuyCondition && Position <= 0)
		{
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		}
		else if (sellCondition && !_prevSellCondition && Position >= 0)
		{
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		}

		_prevBuyCondition = buyCondition;
		_prevSellCondition = sellCondition;
	}

	private static MovingAverage CreateMa(MaType type, int length)
	{
		return type switch
		{
		MaType.Ema => new ExponentialMovingAverage { Length = length },
		_ => new SimpleMovingAverage { Length = length },
		};
	}

	public enum MaType
	{
		Sma = 1,
		Ema
	}
}
