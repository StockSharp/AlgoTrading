using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TrendTraderRemasteredStrategy : Strategy {
	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _increment;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSar;
	private bool _prevPriceAbove;

	public decimal Acceleration {
		get => _acceleration.Value;
		set => _acceleration.Value = value;
	}
	public decimal Increment {
		get => _increment.Value;
		set => _increment.Value = value;
	}
	public decimal MaxAcceleration {
		get => _maxAcceleration.Value;
		set => _maxAcceleration.Value = value;
	}
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TrendTraderRemasteredStrategy() {
		_acceleration =
			Param(nameof(Acceleration), 0.02m)
				.SetGreaterThanZero()
				.SetDisplay("Start", "Initial PSAR acceleration", "PSAR");

		_increment = Param(nameof(Increment), 0.02m)
						 .SetGreaterThanZero()
						 .SetDisplay("Increment", "PSAR increment", "PSAR");

		_maxAcceleration =
			Param(nameof(MaxAcceleration), 0.2m)
				.SetGreaterThanZero()
				.SetDisplay("Max", "Maximum PSAR acceleration", "PSAR");

		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
		return [(Security, CandleType)];
	}

	protected override void OnReseted() {
		base.OnReseted();
		_prevSar = 0m;
		_prevPriceAbove = false;
	}

	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		var psar = new ParabolicSar { Acceleration = Acceleration,
									  AccelerationStep = Increment,
									  AccelerationMax = MaxAcceleration };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(psar, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, psar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sar) {
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceAbove = candle.ClosePrice > sar;
		var crossed = _prevSar > 0m && priceAbove != _prevPriceAbove;

		if (crossed) {
			var volume = Volume + Math.Abs(Position);
			if (priceAbove && Position <= 0) {
				BuyMarket(volume);
			} else if (!priceAbove && Position >= 0) {
				SellMarket(volume);
			}
		} else if ((Position > 0 && !priceAbove) ||
				   (Position < 0 && priceAbove)) {
			ClosePosition();
		}

		_prevSar = sar;
		_prevPriceAbove = priceAbove;
	}
}
