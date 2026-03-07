using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Puria strategy based on three moving averages and MACD filter.
/// Enters long when fast EMA is above two slow EMAs and MACD is positive.
/// Enters short when fast EMA is below two slow EMAs and MACD is negative.
/// </summary>
public class PuriaStrategy : Strategy
{
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _ma3Period;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd;

	private decimal _prevMa75;
	private decimal _prevMa85;
	private decimal _prevMa5;
	private decimal _prevClose;
	private decimal _prevMacd;
	private bool _initialized;

	public int Ma1Period { get => _ma1Period.Value; set => _ma1Period.Value = value; }
	public int Ma2Period { get => _ma2Period.Value; set => _ma2Period.Value = value; }
	public int Ma3Period { get => _ma3Period.Value; set => _ma3Period.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PuriaStrategy()
	{
		_ma1Period = Param(nameof(Ma1Period), 30)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Slow EMA period", "Moving Averages");

		_ma2Period = Param(nameof(Ma2Period), 40)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Second slow EMA period", "Moving Averages");

		_ma3Period = Param(nameof(Ma3Period), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA3 Period", "Fast EMA period", "Moving Averages");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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
		_macd = default;
		_prevMa75 = default;
		_prevMa85 = default;
		_prevMa5 = default;
		_prevClose = default;
		_prevMacd = default;
		_initialized = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma75 = new ExponentialMovingAverage { Length = Ma1Period };
		var ma85 = new ExponentialMovingAverage { Length = Ma2Period };
		var ma5 = new ExponentialMovingAverage { Length = Ma3Period };

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = 15 },
			LongMa = { Length = 26 }
		};

		Indicators.Add(_macd);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma75, ma85, ma5, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma75Value, decimal ma85Value, decimal ma5Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Process MACD manually
		var macdResult = _macd.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!macdResult.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdVal = macdResult.ToDecimal();

		if (!_initialized)
		{
			_prevMa75 = ma75Value;
			_prevMa85 = ma85Value;
			_prevMa5 = ma5Value;
			_prevClose = candle.ClosePrice;
			_prevMacd = macdVal;
			_initialized = true;
			return;
		}

		var buySignal = _prevMa5 > _prevMa75 && _prevMa5 > _prevMa85 && _prevClose > _prevMa5 && _prevMacd > 0m;
		var sellSignal = _prevMa5 < _prevMa75 && _prevMa5 < _prevMa85 && _prevClose < _prevMa5 && _prevMacd < 0m;

		if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevMa75 = ma75Value;
		_prevMa85 = ma85Value;
		_prevMa5 = ma5Value;
		_prevClose = candle.ClosePrice;
		_prevMacd = macdVal;
	}
}
