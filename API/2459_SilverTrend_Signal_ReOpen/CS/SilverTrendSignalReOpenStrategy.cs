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
/// Strategy using SilverTrend-style indicator logic with optional position re-opening.
/// Uses Williams %R zone detection to identify trend changes.
/// Re-opens positions in the trend direction at fixed price intervals.
/// </summary>
public class SilverTrendSignalReOpenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _ssp;
	private readonly StrategyParam<int> _risk;
	private readonly StrategyParam<decimal> _priceStep;
	private readonly StrategyParam<int> _posTotal;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal _entryPrice;
	private decimal _lastReopenPrice;
	private int _positionsCount;
	private bool _uptrend;
	private bool _prevUptrend;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Ssp { get => _ssp.Value; set => _ssp.Value = value; }
	public int Risk { get => _risk.Value; set => _risk.Value = value; }
	public decimal PriceStep { get => _priceStep.Value; set => _priceStep.Value = value; }
	public int PosTotal { get => _posTotal.Value; set => _posTotal.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	public SilverTrendSignalReOpenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_ssp = Param(nameof(Ssp), 9)
			.SetGreaterThanZero()
			.SetDisplay("SSP", "Williams %R period", "Indicators");

		_risk = Param(nameof(Risk), 3)
			.SetGreaterThanZero()
			.SetDisplay("Risk", "Risk parameter for zone width", "Indicators");

		_priceStep = Param(nameof(PriceStep), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Price Step", "Distance to add position", "Trading");

		_posTotal = Param(nameof(PosTotal), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of positions", "Trading");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance", "Trading");
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
		_entryPrice = 0m;
		_lastReopenPrice = 0m;
		_positionsCount = 0;
		_uptrend = false;
		_prevUptrend = false;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_uptrend = false;
		_prevUptrend = false;
		_positionsCount = 0;
		_entryPrice = 0m;
		_lastReopenPrice = 0m;

		var rsi = new RelativeStrengthIndex { Length = Ssp };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		if (rsi < 40m)
			_uptrend = false;
		if (rsi > 60m)
			_uptrend = true;

		var buySignal = !_prevUptrend && _uptrend;
		var sellSignal = _prevUptrend && !_uptrend;

		if (_hasPrev)
		{
			// Check exits for existing positions
			if (Position > 0)
			{
				var stopPrice = _entryPrice - StopLoss;
				var takePrice = _entryPrice + TakeProfit;

				if (sellSignal || close <= stopPrice || close >= takePrice)
				{
					SellMarket(Math.Abs(Position));
					_positionsCount = 0;
					_entryPrice = 0m;
					_lastReopenPrice = 0m;
				}
				else if (PriceStep > 0m && close - _lastReopenPrice >= PriceStep && _positionsCount < PosTotal)
				{
					BuyMarket();
					_lastReopenPrice = close;
					_positionsCount++;
				}
			}
			else if (Position < 0)
			{
				var stopPrice = _entryPrice + StopLoss;
				var takePrice = _entryPrice - TakeProfit;

				if (buySignal || close >= stopPrice || close <= takePrice)
				{
					BuyMarket(Math.Abs(Position));
					_positionsCount = 0;
					_entryPrice = 0m;
					_lastReopenPrice = 0m;
				}
				else if (PriceStep > 0m && _lastReopenPrice - close >= PriceStep && _positionsCount < PosTotal)
				{
					SellMarket();
					_lastReopenPrice = close;
					_positionsCount++;
				}
			}

			// Open new positions on signal
			if (Position == 0)
			{
				if (buySignal)
				{
					BuyMarket();
					_entryPrice = close;
					_lastReopenPrice = close;
					_positionsCount = 1;
				}
				else if (sellSignal)
				{
					SellMarket();
					_entryPrice = close;
					_lastReopenPrice = close;
					_positionsCount = 1;
				}
			}
		}

		_prevUptrend = _uptrend;
		_hasPrev = true;
	}
}
