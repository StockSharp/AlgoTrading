using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses Laguerre-filtered DI+ and DI- to trade directional shifts.
/// </summary>
public class LaguerreAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _gamma;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private decimal _prevUp;
	private decimal _prevDown;
	private decimal _l0Up, _l1Up, _l2Up, _l3Up;
	private decimal _l0Down, _l1Down, _l2Down, _l3Down;
	private bool _isInitialized;

	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal Gamma { get => _gamma.Value; set => _gamma.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LaguerreAdxStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

		_gamma = Param(nameof(Gamma), 0.764m)
			.SetDisplay("Gamma", "Laguerre smoothing factor", "Indicators");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_prevUp = _prevDown = 0;
		_l0Up = _l1Up = _l2Up = _l3Up = 0;
		_l0Down = _l1Down = _l2Down = _l3Down = 0;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var passthrough = new SimpleMovingAverage { Length = 1 };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(passthrough, (candle, _) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var adxResult = _adx.Process(candle);
				if (!adxResult.IsFormed)
					return;

				var adxVal = (AverageDirectionalIndexValue)adxResult;
				var plus = adxVal.Dx.Plus ?? 0m;
				var minus = adxVal.Dx.Minus ?? 0m;

				var up = LaguerreRsi(plus, ref _l0Up, ref _l1Up, ref _l2Up, ref _l3Up);
				var down = LaguerreRsi(minus, ref _l0Down, ref _l1Down, ref _l2Down, ref _l3Down);

				if (!_isInitialized)
				{
					_prevUp = up;
					_prevDown = down;
					_isInitialized = true;
					return;
				}

				// Crossover signals
				if (_prevUp <= _prevDown && up > down && Position <= 0)
				{
					if (Position < 0) BuyMarket();
					BuyMarket();
				}
				else if (_prevUp >= _prevDown && up < down && Position >= 0)
				{
					if (Position > 0) SellMarket();
					SellMarket();
				}

				_prevUp = up;
				_prevDown = down;
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private decimal LaguerreRsi(decimal value, ref decimal l0, ref decimal l1, ref decimal l2, ref decimal l3)
	{
		var l0Prev = l0;
		var l1Prev = l1;
		var l2Prev = l2;

		l0 = (1m - Gamma) * value + Gamma * l0;
		l1 = -Gamma * l0 + l0Prev + Gamma * l1;
		l2 = -Gamma * l1 + l1Prev + Gamma * l2;
		l3 = -Gamma * l2 + l2Prev + Gamma * l3;

		decimal cu = 0, cd = 0;
		if (l0 >= l1) cu = l0 - l1; else cd = l1 - l0;
		if (l1 >= l2) cu += l1 - l2; else cd += l2 - l1;
		if (l2 >= l3) cu += l2 - l3; else cd += l3 - l2;

		return cu + cd == 0 ? 0 : cu / (cu + cd);
	}
}
