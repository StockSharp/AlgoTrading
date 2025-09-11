using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Strategy.
/// Buys when RSI crosses above the oversold level and sells when RSI crosses below the overbought level.
/// </summary>
public class RsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overSold;
	private readonly StrategyParam<decimal> _overBought;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal? _prevRsi;

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal OverSold
	{
		get => _overSold.Value;
		set => _overSold.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal OverBought
	{
		get => _overBought.Value;
		set => _overBought.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RsiStrategy"/>.
	/// </summary>
	public RsiStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General")
			.SetCanOptimize(true);

		_overSold = Param(nameof(OverSold), 25m)
			.SetDisplay("Oversold", "Oversold level", "General")
			.SetCanOptimize(true);

		_overBought = Param(nameof(OverBought), 75m)
			.SetDisplay("Overbought", "Overbought level", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_rsi = default;
		_prevRsi = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, ProcessCandle).Start();

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

		if (!_rsi.IsFormed)
		{
			_prevRsi = rsiValue;
			return;
		}

		if (_prevRsi is null)
		{
			_prevRsi = rsiValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsiValue;
			return;
		}

		var prev = _prevRsi.Value;
		var crossOver = prev < OverSold && rsiValue >= OverSold;
		var crossUnder = prev > OverBought && rsiValue <= OverBought;

		if (crossOver && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (crossUnder && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}

		_prevRsi = rsiValue;
	}
}
