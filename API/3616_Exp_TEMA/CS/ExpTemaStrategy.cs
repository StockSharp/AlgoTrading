namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades TEMA slope reversals similar to the original Exp_TEMA expert advisor.
/// </summary>
public class ExpTemaStrategy : Strategy
{
	private readonly StrategyParam<int> _temaPeriod;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private TripleExponentialMovingAverage _tema = null!;
	private decimal? _prev1;
	private decimal? _prev2;
	private decimal? _prev3;

	public ExpTemaStrategy()
	{
		_temaPeriod = Param(nameof(TemaPeriod), 15)
			.SetDisplay("TEMA period", "Length of the Triple Exponential Moving Average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade volume", "Base order size used for entries.", "Risk")
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop-loss distance", "Protective stop distance expressed in price steps.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100, 2000, 100);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take-profit distance", "Profit target distance expressed in price steps.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(200, 3000, 100);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Type of candles used to calculate the indicator.", "General");
	}

	public int TemaPeriod
	{
		get => _temaPeriod.Value;
		set => _temaPeriod.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_prev1 = null;
		_prev2 = null;
		_prev3 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		Unit? stop = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Step) : null;
		Unit? take = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Step) : null;
		StartProtection(stop, take);

		_tema = new TripleExponentialMovingAverage
		{
			Length = TemaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_tema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal temaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prev1 is null)
		{
			_prev1 = temaValue;
			return;
		}

		if (_prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = temaValue;
			return;
		}

		if (_prev3 is null)
		{
			_prev3 = _prev2;
			_prev2 = _prev1;
			_prev1 = temaValue;
			return;
		}

		var dtema1 = _prev1.Value - _prev2.Value;
		var dtema2 = _prev2.Value - _prev3.Value;

		if (Position > 0m && dtema1 < 0m)
		{
			CancelActiveOrders();
			SellMarket(Position);
		}
		else if (Position < 0m && dtema1 > 0m)
		{
			CancelActiveOrders();
			BuyMarket(-Position);
		}

		var turnedUp = dtema2 < 0m && dtema1 > 0m;
		var turnedDown = dtema2 > 0m && dtema1 < 0m;

		if (turnedUp && Position <= 0m)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (turnedDown && Position >= 0m)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
				SellMarket(volume);
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = temaValue;
	}
}
