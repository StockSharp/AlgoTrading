using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy with optional reversal and trailing stop.
/// Opens long when price crosses above SAR and short when price crosses below.
/// </summary>
public class PsarBug6Strategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _trailing;
	private readonly StrategyParam<decimal> _trailStop;
	private readonly StrategyParam<bool> _sarClose;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DataType> _candleType;

	private ParabolicSar _psar = null!;
	private decimal _prevSar;
	private decimal _prevClose;
	private bool _initialized;

	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public bool Trailing { get => _trailing.Value; set => _trailing.Value = value; }
	public decimal TrailStop { get => _trailStop.Value; set => _trailStop.Value = value; }
	public bool SarClose { get => _sarClose.Value; set => _sarClose.Value = value; }
	public bool Reverse { get => _reverse.Value; set => _reverse.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PsarBug6Strategy()
	{
		_sarStep = Param(nameof(SarStep), 0.001m)
			.SetDisplay("SAR Step", "Acceleration factor step", "Indicator");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration factor", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 0.009m)
			.SetDisplay("Stop Loss", "Initial stop loss distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0.002m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");

		_trailing = Param(nameof(Trailing), false)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailStop = Param(nameof(TrailStop), 0.001m)
			.SetDisplay("Trail Stop", "Trailing stop distance", "Risk");

		_sarClose = Param(nameof(SarClose), true)
			.SetDisplay("Close On SAR Flip", "Exit when SAR changes side", "General");

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Invert trading signals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_psar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_psar, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _psar);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(Trailing ? TrailStop : StopLoss, UnitTypes.Absolute),
			isStopTrailing: Trailing,
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevSar = sar;
			_prevClose = candle.ClosePrice;
			_initialized = true;
			return;
		}

		var crossUp = sar < candle.ClosePrice && _prevSar > _prevClose;
		var crossDown = sar > candle.ClosePrice && _prevSar < _prevClose;

		if (Reverse)
		{
			var temp = crossUp;
			crossUp = crossDown;
			crossDown = temp;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevSar = sar;
			_prevClose = candle.ClosePrice;
			return;
		}

		if (SarClose)
		{
			if (Position > 0 && crossDown)
				SellMarket(Position);
			else if (Position < 0 && crossUp)
				BuyMarket(-Position);
		}

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevSar = sar;
		_prevClose = candle.ClosePrice;
	}
}
