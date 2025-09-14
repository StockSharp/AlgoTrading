using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossover of custom Leading indicator and its EMA.
/// Opens long position when NetLead crosses below EMA and short when crosses above.
/// </summary>
public class ExpLeadingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _alpha1;
	private readonly StrategyParam<decimal> _alpha2;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private bool _isInitialized;
	private bool _hasPrev2;
	private decimal _pricePrev;
	private decimal _leadPrev;
	private decimal _netLeadPrev;
	private decimal _emaPrev;
	private decimal _prevNetLead;
	private decimal _prevEma;
	private decimal _prev2NetLead;
	private decimal _prev2Ema;

	/// <summary>
	/// Alpha1 coefficient for Leading indicator.
	/// </summary>
	public decimal Alpha1 { get => _alpha1.Value; set => _alpha1.Value = value; }

	/// <summary>
	/// Alpha2 coefficient for Leading indicator.
	/// </summary>
	public decimal Alpha2 { get => _alpha2.Value; set => _alpha2.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ExpLeadingStrategy()
	{
		_alpha1 = Param(nameof(Alpha1), 0.25m)
			.SetDisplay("Alpha1", "Alpha1 coefficient", "Indicator");

		_alpha2 = Param(nameof(Alpha2), 0.33m)
			.SetDisplay("Alpha2", "Alpha2 coefficient", "Indicator");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)))
			.SetDisplay("Candle Type", "Candle data type", "General");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in price", "Protection");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in price", "Protection");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(new Unit(TakeProfit, UnitTypes.Price), new Unit(StopLoss, UnitTypes.Price));

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = (candle.HighPrice + candle.LowPrice) / 2m;

		if (!_isInitialized)
		{
			_pricePrev = price;
			_leadPrev = price;
			_netLeadPrev = price;
			_emaPrev = price;
			_prevNetLead = price;
			_prevEma = price;
			_isInitialized = true;
			return;
		}

		var lead = 2m * price + (Alpha1 - 2m) * _pricePrev + (1m - Alpha1) * _leadPrev;
		var netLead = Alpha2 * lead + (1m - Alpha2) * _netLeadPrev;
		var ema = 0.5m * price + 0.5m * _emaPrev;

		if (_hasPrev2)
		{
			var buySignal = _prev2NetLead > _prev2Ema && _prevNetLead < _prevEma;
			var sellSignal = _prev2NetLead < _prev2Ema && _prevNetLead > _prevEma;

			if (buySignal && Position <= 0)
				BuyMarket();
			else if (sellSignal && Position >= 0)
				SellMarket();
		}
		else
		{
			_hasPrev2 = true;
		}

		_prev2NetLead = _prevNetLead;
		_prev2Ema = _prevEma;
		_prevNetLead = netLead;
		_prevEma = ema;
		_pricePrev = price;
		_leadPrev = lead;
		_netLeadPrev = netLead;
		_emaPrev = ema;
	}
}
