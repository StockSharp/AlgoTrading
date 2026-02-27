using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Stochastic oscillator with position re-opening capability.
/// Opens a position when the market enters oversold/overbought zones and
/// re-enters in the same direction after price moves by a defined step.
/// </summary>
public class JBrainTrendReopenStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _kSmoothing;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _priceStep;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _buyEnabled;
	private readonly StrategyParam<bool> _sellEnabled;

	private decimal _lastEntryPrice;
	private int _entriesCount;
	private bool _isLong;

	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public int KSmoothing { get => _kSmoothing.Value; set => _kSmoothing.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal PriceStep { get => _priceStep.Value; set => _priceStep.Value = value; }
	public int MaxPositions { get => _maxPositions.Value; set => _maxPositions.Value = value; }
	public bool BuyEnabled { get => _buyEnabled.Value; set => _buyEnabled.Value = value; }
	public bool SellEnabled { get => _sellEnabled.Value; set => _sellEnabled.Value = value; }

	public JBrainTrendReopenStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Main period for Stochastic oscillator", "Indicators");

		_kSmoothing = Param(nameof(KSmoothing), 3)
			.SetGreaterThanZero()
			.SetDisplay("K Smoothing", "Smoothing for %K line", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Smoothing for %D line", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Timeframe", "Timeframe for calculations", "General");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_priceStep = Param(nameof(PriceStep), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Re-entry Step", "Price move to add position", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum entries in one direction", "Risk");

		_buyEnabled = Param(nameof(BuyEnabled), true)
			.SetDisplay("Allow Long", "Enable long trades", "General");

		_sellEnabled = Param(nameof(SellEnabled), true)
			.SetDisplay("Allow Short", "Enable short trades", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastEntryPrice = 0m;
		_entriesCount = 0;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochPeriod;
		stochastic.D.Length = DPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var price = candle.ClosePrice;

		if (Position == 0)
			_entriesCount = 0;

		// Buy signal: oversold
		if (k < 20 && Position <= 0 && BuyEnabled)
		{
			BuyMarket();
			_isLong = true;
			_lastEntryPrice = price;
			_entriesCount = 1;
			return;
		}

		// Sell signal: overbought
		if (k > 80 && Position >= 0 && SellEnabled)
		{
			SellMarket();
			_isLong = false;
			_lastEntryPrice = price;
			_entriesCount = 1;
			return;
		}

		// Exit long on overbought
		if (Position > 0 && k > 80)
		{
			SellMarket();
			return;
		}

		// Exit short on oversold
		if (Position < 0 && k < 20)
		{
			BuyMarket();
			return;
		}

		// Re-entry logic
		if (_entriesCount > 0 && _entriesCount < MaxPositions)
		{
			if (_isLong && Position > 0 && price - _lastEntryPrice >= PriceStep)
			{
				BuyMarket();
				_lastEntryPrice = price;
				_entriesCount++;
			}
			else if (!_isLong && Position < 0 && _lastEntryPrice - price >= PriceStep)
			{
				SellMarket();
				_lastEntryPrice = price;
				_entriesCount++;
			}
		}
	}
}
