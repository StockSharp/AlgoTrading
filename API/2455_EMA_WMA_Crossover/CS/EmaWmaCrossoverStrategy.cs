using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA and WMA crossover strategy with fixed risk management.
/// Goes long when EMA crosses below WMA and short when EMA crosses above WMA.
/// Position size is calculated from a percentage of equity.
/// </summary>
public class EmaWmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _prevEma;
	private decimal _prevWma;
	private bool _hasPrev;

	/// <summary>
	/// EMA period length.
	/// </summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>
	/// WMA period length.
	/// </summary>
	public int WmaPeriod { get => _wmaPeriod.Value; set => _wmaPeriod.Value = value; }

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossTicks { get => _stopLossTicks.Value; set => _stopLossTicks.Value = value; }

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks { get => _takeProfitTicks.Value; set => _takeProfitTicks.Value = value; }

	/// <summary>
	/// Percent of equity risked per trade.
	/// </summary>
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="EmaWmaCrossoverStrategy"/>.
	/// </summary>
	public EmaWmaCrossoverStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 28)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "EMA period length", "Indicators")
		.SetCanOptimize(true);

		_wmaPeriod = Param(nameof(WmaPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("WMA Period", "WMA period length", "Indicators")
		.SetCanOptimize(true);

		_stopLossTicks = Param(nameof(StopLossTicks), 50)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss Ticks", "Stop loss distance in ticks", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 50)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit Ticks", "Take profit distance in ticks", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Percent", "Percent of equity risked per trade", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_hasPrev = false;
		_prevEma = 0m;
		_prevWma = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tick = Security?.PriceStep ?? 1m;
		_stopLossDistance = StopLossTicks * tick;
		_takeProfitDistance = TakeProfitTicks * tick;

		StartProtection(
		takeProfit: new Unit(_takeProfitDistance, UnitTypes.Absolute),
		stopLoss: new Unit(_stopLossDistance, UnitTypes.Absolute));

		var ema = new ExponentialMovingAverage { Length = EmaPeriod, CandlePrice = CandlePrice.Open };
		var wma = new WeightedMovingAverage { Length = WmaPeriod, CandlePrice = CandlePrice.Open };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, wma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, wma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal wma)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_hasPrev)
		{
			_prevEma = ema;
			_prevWma = wma;
			_hasPrev = true;
			return;
		}

		var crossDown = ema < wma && _prevEma > _prevWma;
		var crossUp = ema > wma && _prevEma < _prevWma;

		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = equity * RiskPercent / 100m;
		var volume = _stopLossDistance > 0m ? riskAmount / _stopLossDistance : 0m;

		var step = Security?.VolumeStep ?? 1m;
		if (step > 0m)
		volume = Math.Floor(volume / step) * step;

		if (crossDown && Position <= 0)
		{
			var qty = volume > 0m ? volume + Math.Abs(Position) : Volume + Math.Abs(Position);
			BuyMarket(qty);
		}
		else if (crossUp && Position >= 0)
		{
			var qty = volume > 0m ? volume + Math.Abs(Position) : Volume + Math.Abs(Position);
			SellMarket(qty);
		}

		_prevEma = ema;
		_prevWma = wma;
	}
}
