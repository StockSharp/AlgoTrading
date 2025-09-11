using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI strategy with percentage-based take profit and stop loss.
/// </summary>
public class RsiStrategyWithManualTpAndSlStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private RelativeStrengthIndex _rsi;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _prevRsi;
	private bool _hasPrevRsi;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }

	/// <summary>
	/// Lookback period for high/low.
	/// </summary>
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public RsiStrategyWithManualTpAndSlStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought Level", "RSI overbought level", "RSI");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold Level", "RSI oversold level", "RSI");

		_lookback = Param(nameof(Lookback), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "High/low lookback", "General");

		_takeProfit = Param(nameof(TakeProfit), 1m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highest = _highest.Process(candle).ToDecimal();
		var lowest = _lowest.Process(candle).ToDecimal();

		if (!_rsi.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevRsi = rsiValue;
			_hasPrevRsi = true;
			return;
		}

		var crossAbove = _hasPrevRsi && _prevRsi <= Oversold && rsiValue > Oversold;
		var crossBelow = _hasPrevRsi && _prevRsi >= Overbought && rsiValue < Overbought;

		var close = candle.ClosePrice;
		var longSignal = crossAbove && close > highest * 0.7m;
		var shortSignal = crossBelow && close < lowest * 1.3m;

		if (longSignal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket();
		}
		else if (shortSignal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);

			SellMarket();
		}

		_prevRsi = rsiValue;
		_hasPrevRsi = true;
	}
}
