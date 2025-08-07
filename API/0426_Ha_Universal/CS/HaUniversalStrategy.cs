namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Heikin Ashi Universal Long/Short Futures Strategy
/// </summary>
public class HaUniversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private Highest _smaHigh;
	private Lowest _smaLow;
	
	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private int _hlv;
	private decimal _sslDown;
	private decimal _sslUp;
	private decimal _prevSslUp;
	private decimal _prevSslDown;

	public HaUniversalStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_period = Param(nameof(Period), 3)
			.SetDisplay("Period", "SSL period", "Strategy");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[] { (Security, CandleType) };

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaClose = default;
		_prevHaOpen = default;
		_hlv = default;
		_sslDown = default;
		_sslUp = default;
		_prevSslUp = default;
		_prevSslDown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_smaHigh = new Highest { Length = Period };
		_smaLow = new Lowest { Length = Period };

		// Subscribe to candles
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		// Enable protection
		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Skip if strategy is not ready
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Skip non-finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate Heikin-Ashi values
		decimal haOpen, haClose, haHigh, haLow;

		if (_prevHaOpen == 0)
		{
			// First candle
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
			haHigh = candle.HighPrice;
			haLow = candle.LowPrice;
		}
		else
		{
			// Calculate based on previous HA candle
			haOpen = (_prevHaOpen + _prevHaClose) / 2;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
			haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
			haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);
		}

		// Process indicators with HA values using candle's time
		var highValue = _smaHigh.Process(haHigh, candle.ServerTime, candle.State == CandleStates.Finished);
		var lowValue = _smaLow.Process(haLow, candle.ServerTime, candle.State == CandleStates.Finished);

		if (!_smaHigh.IsFormed || !_smaLow.IsFormed)
		{
			// Store current values for next candle
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
			return;
		}

		// Calculate SSL Channel
		var smaHighValue = highValue.ToDecimal();
		var smaLowValue = lowValue.ToDecimal();

		// Update HLV (High-Low Value)
		if (haClose > smaHighValue)
			_hlv = 1;
		else if (haClose < smaLowValue)
			_hlv = -1;
		// else keep previous _hlv value

		// Calculate SSL lines
		_sslDown = _hlv < 0 ? smaHighValue : smaLowValue;
		_sslUp = _hlv < 0 ? smaLowValue : smaHighValue;

		// Check for crossovers
		var bullishCross = _sslUp > _sslDown && _prevSslUp <= _prevSslDown;
		var bearishCross = _sslDown > _sslUp && _prevSslDown <= _prevSslUp;

		// Execute trades
		if (bullishCross && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (bearishCross && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		// Store current values for next candle
		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevSslUp = _sslUp;
		_prevSslDown = _sslDown;
	}
}