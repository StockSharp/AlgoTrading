using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>Completed-candle momentum smoothed by a Jurik moving average.</summary>
public class ColorJMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumLength, _jmaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent, _takeProfitPercent;
	private readonly StrategyParam<bool> _enableStopLoss, _enableLong, _enableShort;

	private JurikMovingAverage _jma;
	private decimal? _previous, _beforePrevious;
	private Order _pending;

	public int MomentumLength { get => _momentumLength.Value; set => _momentumLength.Value = value; }
	public int JmaLength { get => _jmaLength.Value; set => _jmaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public bool EnableStopLoss { get => _enableStopLoss.Value; set => _enableStopLoss.Value = value; }
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

	public ColorJMomentumStrategy()
	{
		_momentumLength = Param(nameof(MomentumLength), 8).SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Momentum lookback", "Indicator");
		_jmaLength = Param(nameof(JmaLength), 8).SetGreaterThanZero()
			.SetDisplay("JMA Length", "Smoothing length", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Signal candle timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetNotNegative()
			.SetDisplay("Stop Loss %", "Optional native percent risk distance", "Risk");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m).SetNotNegative()
			.SetDisplay("Take Profit %", "Native percent target distance", "Risk");
		_enableStopLoss = Param(nameof(EnableStopLoss), true)
			.SetDisplay("Enable Stop Loss", "Activate initial stop", "Risk");
		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long entries", "Trading");
		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short entries", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_jma = null;
		_previous = _beforePrevious = null;
		_pending = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previous = _beforePrevious = null;
		_pending = null;
		var momentum = new Momentum { Length = MomentumLength };
		_jma = new JurikMovingAverage { Length = JmaLength };
		Indicators.Add(_jma);
		SubscribeCandles(CandleType).Bind(momentum, ProcessCandle).Start();
		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(EnableStopLoss ? StopLossPercent : 0m, UnitTypes.Percent),
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var result = _jma.Process(momentum, candle.OpenTime, true);
		if (!result.IsFormed)
			return;
		var value = result.ToDecimal();
		var up = _beforePrevious is decimal before && _previous is decimal previous
			&& previous < before && value > previous;
		var down = _beforePrevious is decimal beforeDown && _previous is decimal previousDown
			&& previousDown > beforeDown && value < previousDown;
		_beforePrevious = _previous;
		_previous = value;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)))
			return;
		if (up)
		{
			if (Position < 0m)
				_pending = BuyMarket(-Position + (EnableLong ? Volume : 0m));
			else if (Position == 0m && EnableLong)
				_pending = BuyMarket(Volume);
		}
		else if (down)
		{
			if (Position > 0m)
				_pending = SellMarket(Position + (EnableShort ? Volume : 0m));
			else if (Position == 0m && EnableShort)
				_pending = SellMarket(Volume);
		}
	}
}
