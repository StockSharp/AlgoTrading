using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enhanced Bollinger Bands strategy with limit entries and fixed pip stops.
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

	/// <summary>
	/// Bollinger Bands period.
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
	/// Enable long positions.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Enable short positions.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Value of one pip.
	/// </summary>
	public decimal PipValue
	{
		get => _pipValue.Value;
		set => _pipValue.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
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
	/// Initializes a new instance of the <see cref="EnhancedBollingerBandsStrategy"/> class.
	/// </summary>
	public EnhancedBollingerBandsStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Period for Bollinger Bands", "Indicators")
			.SetCanOptimize(true);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Standard deviation multiplier", "Indicators")
			.SetCanOptimize(true);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long positions", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short positions", "General");

		_pipValue = Param(nameof(PipValue), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Value", "Value of one pip", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Pips", "Stop loss distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Pips", "Take profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevClose = _prevUpper = _prevLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerMultiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfitPips * PipValue, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPips * PipValue, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevClose.HasValue && _prevUpper.HasValue && _prevLower.HasValue)
		{
			var crossLower = _prevClose <= _prevLower && candle.ClosePrice > lower;
			var crossUpper = _prevClose >= _prevUpper && candle.ClosePrice < upper;

			if (EnableLong && crossLower && Position <= 0)
				BuyLimit(lower);

			if (EnableShort && crossUpper && Position >= 0)
				SellLimit(upper);
		}

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}

