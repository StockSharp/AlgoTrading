using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI oscillator smoothed through Jurik MA. Middle-level reversals and
/// extreme-level exits are based on formed indicators and completed candles.
/// </summary>
public class ColorJLaguerreStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength, _jmaLength;
	private readonly StrategyParam<decimal> _highLevel, _middleLevel, _lowLevel, _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private JurikMovingAverage _jma;
	private decimal? _previous;
	private Order _pending;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int JmaLength { get => _jmaLength.Value; set => _jmaLength.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal MiddleLevel { get => _middleLevel.Value; set => _middleLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorJLaguerreStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI lookback period", "Indicator");
		_jmaLength = Param(nameof(JmaLength), 14).SetGreaterThanZero()
			.SetDisplay("JMA Length", "Jurik smoothing of RSI", "Indicator");
		_highLevel = Param(nameof(HighLevel), 85m)
			.SetDisplay("High Level", "Close long at this threshold", "Levels");
		_middleLevel = Param(nameof(MiddleLevel), 50m)
			.SetDisplay("Middle Level", "Entry crossover threshold", "Levels");
		_lowLevel = Param(nameof(LowLevel), 15m)
			.SetDisplay("Low Level", "Close short at this threshold", "Levels");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss %", "Native percent stop distance", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_jma = null;
		_previous = null;
		_pending = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previous = null;
		_pending = null;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		_jma = new JurikMovingAverage { Length = JmaLength };
		Indicators.Add(_jma);
		SubscribeCandles(CandleType)
			.Bind(rsi, (candle, value) =>
			{
				if (candle.State == CandleStates.Finished && rsi.IsFormed)
					ProcessCandle(candle, value);
			}).Start();

		StartProtection(
			takeProfit: new Unit(0m, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		var result = _jma.Process(rsiValue, candle.OpenTime, true);
		if (!result.IsFormed)
			return;
		var value = result.ToDecimal();
		var crossUp = _previous is decimal prior && prior <= MiddleLevel && value > MiddleLevel;
		var crossDown = _previous is decimal before && before >= MiddleLevel && value < MiddleLevel;
		_previous = value;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (Position > 0m && value >= HighLevel)
		{
			_pending = SellMarket(Position);
			return;
		}
		if (Position < 0m && value <= LowLevel)
		{
			_pending = BuyMarket(-Position);
			return;
		}

		if (crossUp)
		{
			if (Position < 0m)
				_pending = BuyMarket(-Position + Volume);
			else if (Position == 0m)
				_pending = BuyMarket(Volume);
		}
		else if (crossDown)
		{
			if (Position > 0m)
				_pending = SellMarket(Position + Volume);
			else if (Position == 0m)
				_pending = SellMarket(Volume);
		}
	}
}
