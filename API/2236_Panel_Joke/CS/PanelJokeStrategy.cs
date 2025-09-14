using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple heuristic strategy comparing current and previous candles.
/// Counts how many price components moved up or down and trades on majority.
/// </summary>
public class PanelJokeStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableAutopilot;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;
	private bool _hasPrev;

	/// <summary>
	/// Enables automatic order execution.
	/// </summary>
	public bool EnableAutopilot
	{
		get => _enableAutopilot.Value;
		set => _enableAutopilot.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public PanelJokeStrategy()
	{
		_enableAutopilot = Param(nameof(EnableAutopilot), true)
			.SetDisplay("Enable Autopilot", "Automatically trade based on signals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for incoming candles", "General");
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

		_prevOpen = 0;
		_prevHigh = 0;
		_prevLow = 0;
		_prevClose = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrev)
		{
			_prevOpen = candle.OpenPrice;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevClose = candle.ClosePrice;
			_hasPrev = true;
			return;
		}

		var buy = 0;
		var sell = 0;

		// Compare each OHLC component
		if (candle.OpenPrice > _prevOpen)
			buy++;
		else
			sell++;

		if (candle.HighPrice > _prevHigh)
			buy++;
		else
			sell++;

		if (candle.LowPrice > _prevLow)
			buy++;
		else
			sell++;

		var avgHL = (candle.HighPrice + candle.LowPrice) / 2m;
		var prevAvgHL = (_prevHigh + _prevLow) / 2m;
		if (avgHL > prevAvgHL)
			buy++;
		else
			sell++;

		if (candle.ClosePrice > _prevClose)
			buy++;
		else
			sell++;

		var avgHLC = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var prevAvgHLC = (_prevHigh + _prevLow + _prevClose) / 3m;
		if (avgHLC > prevAvgHLC)
			buy++;
		else
			sell++;

		var avgHLCC = (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m;
		var prevAvgHLCC = (_prevHigh + _prevLow + 2m * _prevClose) / 4m;
		if (avgHLCC > prevAvgHLCC)
			buy++;
		else
			sell++;

		if (EnableAutopilot)
		{
			if (buy > sell && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (sell > buy && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevOpen = candle.OpenPrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
	}
}
