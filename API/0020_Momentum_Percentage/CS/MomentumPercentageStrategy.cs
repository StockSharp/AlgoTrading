using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on price momentum percentage change.
/// Uses Momentum indicator with SMA trend filter.
/// Buys when momentum crosses above zero and price is above SMA.
/// Sells when momentum crosses below zero and price is below SMA.
/// </summary>
public class MomentumPercentageStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMom;
	private bool _hasPrevValues;
	private int _cooldown;

	/// <summary>
	/// Momentum period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
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
	/// Initializes a new instance of the <see cref="MomentumPercentageStrategy"/>.
	/// </summary>
	public MomentumPercentageStrategy()
	{
		_momentumPeriod = Param(nameof(MomentumPeriod), 10)
			.SetDisplay("Momentum Period", "Period for momentum calculation", "Indicators")
			.SetOptimize(8, 20, 4);

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetDisplay("SMA Period", "Period for SMA trend filter", "Indicators")
			.SetOptimize(15, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevMom = default;
		_hasPrevValues = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new Momentum { Length = MomentumPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(momentum, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, momentum);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (smaValue == 0)
			return;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevMom = momValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMom = momValue;
			return;
		}

		var price = candle.ClosePrice;

		// Momentum crosses above zero + price above SMA = buy
		if (_prevMom <= 0 && momValue > 0 && price > smaValue && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = 30;
		}
		// Momentum crosses below zero + price below SMA = sell
		else if (_prevMom >= 0 && momValue < 0 && price < smaValue && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = 30;
		}

		_prevMom = momValue;
	}
}
