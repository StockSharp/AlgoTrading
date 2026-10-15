using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Directed Movement Candle strategy.
/// Uses RSI levels to detect momentum shifts and trade accordingly.
/// </summary>
public class DirectedMovementCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal? _prevColor;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DirectedMovementCandleStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI period", "Indicator");
		_highLevel = Param(nameof(HighLevel), 70m)
			.SetDisplay("High Level", "Upper threshold", "Indicator");
		_lowLevel = Param(nameof(LowLevel), 30m)
			.SetDisplay("Low Level", "Lower threshold", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "Data");
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
		_rsi = null;
		_prevColor = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevColor = null;

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// color: 2=overbought, 1=neutral, 0=oversold
		var color = 1m;
		if (rsiValue >= HighLevel)
			color = 2m;
		else if (rsiValue <= LowLevel)
			color = 0m;

		if (_prevColor == null)
		{
			_prevColor = color;
			return;
		}

		// RSI crosses into overbought zone -> buy
		if (color == 2m && _prevColor < 2m && Position <= 0)
			BuyMarket();
		// RSI crosses into oversold zone -> sell
		else if (color == 0m && _prevColor > 0m && Position >= 0)
			SellMarket();

		_prevColor = color;
	}
}
