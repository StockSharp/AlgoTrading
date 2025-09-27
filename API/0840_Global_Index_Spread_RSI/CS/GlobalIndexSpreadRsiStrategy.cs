using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades ES against a global index using spread RSI.
/// </summary>
public class GlobalIndexSpreadRsiStrategy : Strategy
{
	private readonly StrategyParam<Security> _global;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<DataType> _candleType;

	private readonly RelativeStrengthIndex _rsi = new();
	private readonly Dictionary<Security, decimal> _lastPrices = [];

	private decimal _entryPrice;
	private decimal _prevProfit;
	private DateTimeOffset _prevTime;

	/// <summary>
	/// Global index security.
	/// </summary>
	public Security GlobalSecurity
	{
		get => _global.Value;
		set => _global.Value = value;
	}

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI oversold threshold.
	/// </summary>
	public decimal OversoldThreshold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// RSI overbought threshold.
	/// </summary>
	public decimal OverboughtThreshold
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GlobalIndexSpreadRsiStrategy"/>.
	/// </summary>
	public GlobalIndexSpreadRsiStrategy()
	{
		_global = Param<Security>(nameof(GlobalSecurity), null)
			.SetDisplay("Global Index", "Global index security", "Universe");

		_rsiLength = Param(nameof(RsiLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "Parameters");

		_oversold = Param(nameof(OversoldThreshold), 35m)
			.SetDisplay("Oversold", "RSI oversold level", "Parameters");

		_overbought = Param(nameof(OverboughtThreshold), 78m)
			.SetDisplay("Overbought", "RSI overbought level", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null || GlobalSecurity == null)
			throw new InvalidOperationException("Securities must be set.");

		yield return (Security, CandleType);
		yield return (GlobalSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastPrices.Clear();
		_rsi.Reset();
		_entryPrice = 0m;
		_prevProfit = 0m;
		_prevTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (GlobalSecurity == null)
			throw new InvalidOperationException("Global security must be set.");

		base.OnStarted(time);

		_rsi.Length = RsiLength;

		var mainSub = SubscribeCandles(CandleType, true, Security);
		mainSub.Bind(c => ProcessCandle(c, Security)).Start();

		SubscribeCandles(CandleType, true, GlobalSecurity)
			.Bind(c => ProcessCandle(c, GlobalSecurity))
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastPrices[security] = candle.ClosePrice;

		if (security != Security)
			return;

		if (!_lastPrices.TryGetValue(GlobalSecurity, out var globalClose))
			return;

		var spread = (candle.ClosePrice - globalClose) / globalClose * 100m;
		var rsiValue = _rsi.Process(new DecimalIndicatorValue(_rsi, spread));
		if (!rsiValue.IsFinal)
			return;

		var rsi = rsiValue.ToDecimal();

		if (rsi < OversoldThreshold && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_prevTime = candle.CloseTime;
			_prevProfit = 0m;
		}
		else if (rsi > OverboughtThreshold && Position > 0)
		{
			SellMarket(Position);
			_entryPrice = 0m;
			_prevTime = default;
		}

		if (Position > 0 && _entryPrice > 0m)
		{
			var profit = (candle.ClosePrice - _entryPrice) / _entryPrice * 100m;
			if (_prevTime != default)
				DrawLine(_prevTime, _prevProfit, candle.CloseTime, profit);
			_prevTime = candle.CloseTime;
			_prevProfit = profit;
		}
	}
}

