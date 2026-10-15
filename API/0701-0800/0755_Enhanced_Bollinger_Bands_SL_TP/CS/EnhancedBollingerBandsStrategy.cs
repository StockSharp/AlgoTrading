using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enhanced Bollinger Bands SL TP strategy.
/// A close back above the lower band after the previous close was at or below the previous lower band places a buy limit order at
/// the close; a close back below the upper band after the previous close was at or above the previous upper band places a sell
/// limit order. An unfilled entry order is cancelled on the next candle. Stop loss and take profit are StopLossPips and
/// TakeProfitPips times PipValue from the entry.
/// </summary>
public class EnhancedBollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _pipValue;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private Order _entryOrder;

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Price of one pip.
	/// </summary>
	public decimal PipValue
	{
		get => _pipValue.Value;
		set => _pipValue.Value = value;
	}

	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EnhancedBollingerBandsStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Bollinger Bands length", "Indicators");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Bollinger Bands deviation multiplier", "Indicators");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "General");

		_pipValue = Param(nameof(PipValue), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Value", "Price of one pip", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 10m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Pips", "Stop loss in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetNotNegative()
			.SetDisplay("Take Profit Pips", "Take profit in pips", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
		_entryOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		StartProtection(
			TakeProfitPips > 0m ? new Unit(TakeProfitPips * PipValue, UnitTypes.Absolute) : new Unit(),
			StopLossPips > 0m ? new Unit(StopLossPips * PipValue, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_entryOrder != null && _entryOrder.State == OrderStates.Active)
			CancelOrder(_entryOrder);

		_entryOrder = null;

		if (!bollingerValue.IsFormed || bollingerValue is not IBollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		var prevClose = _prevClose;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;
		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;

		if (prevClose is not decimal pc || prevUpper is not decimal pu || prevLower is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (EnableLong && pc <= pl && close > lower && Position <= 0)
			_entryOrder = BuyLimit(close, Volume + Math.Abs(Position));
		else if (EnableShort && pc >= pu && close < upper && Position >= 0)
			_entryOrder = SellLimit(close, Volume + Math.Abs(Position));
	}
}
