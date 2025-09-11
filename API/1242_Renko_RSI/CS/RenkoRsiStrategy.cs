using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trades Renko bricks with RSI signals.
/// Buys when RSI crosses above the oversold level and sells when it drops below the overbought level.
/// </summary>
public class RenkoRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _renkoAtrLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;

	private DataType _renkoType;
	private RelativeStrengthIndex _rsi;
	private decimal _prevRsi;
	private bool _isFirst = true;

	/// <summary>
	/// ATR period used to calculate Renko brick size.
	/// </summary>
	public int RenkoAtrLength { get => _renkoAtrLength.Value; set => _renkoAtrLength.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// Initialize <see cref="RenkoRsiStrategy"/>.
	/// </summary>
	public RenkoRsiStrategy()
	{
		_renkoAtrLength = Param(nameof(RenkoAtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for renko brick size", "Renko")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_rsiLength = Param(nameof(RsiLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 80m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_rsiOversold = Param(nameof(RsiOversold), 20m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		_renkoType ??= DataType.Create(typeof(RenkoCandleMessage), new RenkoCandleArg
		{
			BuildFrom = RenkoBuildFrom.Atr,
			Length = RenkoAtrLength
		});

		return [(Security, _renkoType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(_renkoType);
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

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isFirst)
		{
			if (Position <= 0 && _prevRsi <= RsiOversold && rsi > RsiOversold)
				BuyMarket();
			else if (Position >= 0 && _prevRsi >= RsiOverbought && rsi < RsiOverbought)
				SellMarket();
		}
		else
		{
			_isFirst = false;
		}

		_prevRsi = rsi;
	}
}

