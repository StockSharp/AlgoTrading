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
/// Strategy based on turning points of double smoothed moving average.
/// Applies simple moving average followed by Jurik moving average and
/// enters on local extrema of the smoothed line.
/// </summary>
public class ExpX2MaStrategy : Strategy
{
	private readonly StrategyParam<int> _firstMaLength;
	private readonly StrategyParam<int> _secondMaLength;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private SimpleMovingAverage _sma = null!;
	private ExponentialMovingAverage _ema2 = null!;

	private decimal? _prevPrevValue;
	private decimal? _prevValue;
	private decimal _entryPrice;

	/// <summary>
	/// Period of the first smoothing (SMA).
	/// </summary>
	public int FirstMaLength { get => _firstMaLength.Value; set => _firstMaLength.Value = value; }

	/// <summary>
	/// Period of the second smoothing (JMA).
	/// </summary>
	public int SecondMaLength { get => _secondMaLength.Value; set => _secondMaLength.Value = value; }

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Type of candles used.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions on reverse signals.
	/// </summary>
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }

	/// <summary>
	/// Allow closing short positions on reverse signals.
	/// </summary>
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ExpX2MaStrategy()
	{
		_firstMaLength = Param(nameof(FirstMaLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("First MA Length", "Period for first smoothing", "General")
		;

		_secondMaLength = Param(nameof(SecondMaLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Second MA Length", "Period for second smoothing", "General")
		;

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Points", "Fixed stop loss in price points", "Risk Management")
		;

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Points", "Fixed take profit in price points", "Risk Management")
		;

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_buyOpen = Param(nameof(BuyOpen), true)
		.SetDisplay("Enable Buy", "Allow opening long positions", "Signals");

		_sellOpen = Param(nameof(SellOpen), true)
		.SetDisplay("Enable Sell", "Allow opening short positions", "Signals");

		_buyClose = Param(nameof(BuyClose), true)
		.SetDisplay("Close Long", "Allow closing long positions", "Signals");

		_sellClose = Param(nameof(SellClose), true)
		.SetDisplay("Close Short", "Allow closing short positions", "Signals");
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
		_prevPrevValue = null;
		_prevValue = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = FirstMaLength };
		_ema2 = new ExponentialMovingAverage { Length = SecondMaLength };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var smaValue = _sma.Process(candle);
		var emaValue = _ema2.Process(smaValue);

		if (!emaValue.IsFinal)
			return;

		var current = emaValue.GetValue<decimal>();

		// Handle stop loss and take profit before new signals
		if (Position > 0)
		{
			if ((StopLossPoints > 0m && candle.ClosePrice <= _entryPrice - StopLossPoints) ||
				(TakeProfitPoints > 0m && candle.ClosePrice >= _entryPrice + TakeProfitPoints))
			{
				SellMarket();
				_entryPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if ((StopLossPoints > 0m && candle.ClosePrice >= _entryPrice + StopLossPoints) ||
				(TakeProfitPoints > 0m && candle.ClosePrice <= _entryPrice - TakeProfitPoints))
			{
				BuyMarket();
				_entryPrice = 0m;
			}
		}

		if (_prevValue.HasValue && _prevPrevValue.HasValue)
		{
			var isLocalMin = _prevValue.Value < _prevPrevValue.Value && current > _prevValue.Value;
			var isLocalMax = _prevValue.Value > _prevPrevValue.Value && current < _prevValue.Value;

			if (isLocalMin)
			{
				if (SellClose && Position < 0)
					BuyMarket();

				if (BuyOpen && Position <= 0)
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
				}
			}
			else if (isLocalMax)
			{
				if (BuyClose && Position > 0)
					SellMarket();

				if (SellOpen && Position >= 0)
				{
					SellMarket();
					_entryPrice = candle.ClosePrice;
				}
			}
		}

		_prevPrevValue = _prevValue;
		_prevValue = current;
	}
}
