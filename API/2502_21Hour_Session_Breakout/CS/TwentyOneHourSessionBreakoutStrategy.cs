using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 21-hour session breakout strategy. Places simulated stop entries via candle breakout logic.
/// </summary>
public class TwentyOneHourSessionBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _firstSessionStartHour;
	private readonly StrategyParam<int> _firstSessionStopHour;
	private readonly StrategyParam<decimal> _stepPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _sessionOpen;
	private decimal _entryPrice;
	private bool _inSession;

	public int FirstSessionStartHour
	{
		get => _firstSessionStartHour.Value;
		set => _firstSessionStartHour.Value = value;
	}

	public int FirstSessionStopHour
	{
		get => _firstSessionStopHour.Value;
		set => _firstSessionStopHour.Value = value;
	}

	public decimal StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TwentyOneHourSessionBreakoutStrategy()
	{
		_firstSessionStartHour = Param(nameof(FirstSessionStartHour), 2)
			.SetDisplay("Session Start", "Hour of the trading window start", "Schedule");

		_firstSessionStopHour = Param(nameof(FirstSessionStopHour), 20)
			.SetDisplay("Session Stop", "Hour of the trading window stop", "Schedule");

		_stepPoints = Param(nameof(StepPoints), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Step Points", "Distance from session open to breakout level", "Orders");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Points", "Take-profit distance", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles used to drive the trading schedule", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_sessionOpen = null;
		_entryPrice = 0m;
		_inSession = false;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;
		var priceStep = Security?.PriceStep ?? 1m;

		// Session start: record the open price
		if (hour >= FirstSessionStartHour && hour < FirstSessionStopHour)
		{
			if (!_inSession)
			{
				_sessionOpen = candle.OpenPrice;
				_inSession = true;
			}

			if (_sessionOpen == null)
				return;

			var stepOffset = StepPoints * priceStep;
			var buyLevel = _sessionOpen.Value + stepOffset;
			var sellLevel = _sessionOpen.Value - stepOffset;

			// Breakout entry
			if (Position == 0)
			{
				if (candle.HighPrice >= buyLevel)
				{
					BuyMarket();
					_entryPrice = buyLevel;
				}
				else if (candle.LowPrice <= sellLevel)
				{
					SellMarket();
					_entryPrice = sellLevel;
				}
			}

			// Take profit
			if (Position > 0)
			{
				var tp = _entryPrice + TakeProfitPoints * priceStep;
				if (candle.HighPrice >= tp)
				{
					SellMarket();
					_sessionOpen = candle.ClosePrice;
				}
			}
			else if (Position < 0)
			{
				var tp = _entryPrice - TakeProfitPoints * priceStep;
				if (candle.LowPrice <= tp)
				{
					BuyMarket();
					_sessionOpen = candle.ClosePrice;
				}
			}
		}
		else
		{
			// Session end: close position
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();

			_inSession = false;
			_sessionOpen = null;
		}
	}
}
