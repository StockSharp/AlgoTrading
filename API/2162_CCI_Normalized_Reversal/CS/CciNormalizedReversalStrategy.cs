using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI Normalized Reversal Strategy.
/// Enters positions after the indicator leaves extreme zones.
/// </summary>
public class CciNormalizedReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _middleLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevColor = 2;
	private int _prevPrevColor = 2;

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Upper CCI threshold.
	/// </summary>
	public int HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Middle CCI threshold.
	/// </summary>
	public int MiddleLevel
	{
		get => _middleLevel.Value;
		set => _middleLevel.Value = value;
	}

	/// <summary>
	/// Lower CCI threshold.
	/// </summary>
	public int LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CciNormalizedReversalStrategy"/>.
	/// </summary>
	public CciNormalizedReversalStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 10)
			.SetDisplay("CCI Period", "Lookback period for CCI", "General")
			.SetRange(5, 50)
			.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), 100)
			.SetDisplay("High Level", "Upper CCI threshold", "General")
			.SetRange(50, 200)
			.SetCanOptimize(true);

		_middleLevel = Param(nameof(MiddleLevel), 0)
			.SetDisplay("Middle Level", "Middle CCI threshold", "General")
			.SetRange(-50, 50)
			.SetCanOptimize(true);

		_lowLevel = Param(nameof(LowLevel), -100)
			.SetDisplay("Low Level", "Lower CCI threshold", "General")
			.SetRange(-200, -50)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
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

		_prevColor = 2;
		_prevPrevColor = 2;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(useMarketOrders: true);

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(cci, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var color = GetColorIndex(cciValue);

		// Close short when CCI rises above middle level
		if (_prevColor < 2 && Position < 0)
			BuyMarket();

		// Close long when CCI falls below middle level
		if (_prevColor > 2 && Position > 0)
			SellMarket();

		// Open long after leaving high zone
		if (_prevPrevColor == 0 && _prevColor > 0 && Position <= 0)
			BuyMarket();

		// Open short after leaving low zone
		if (_prevPrevColor == 4 && _prevColor < 4 && Position >= 0)
			SellMarket();

		_prevPrevColor = _prevColor;
		_prevColor = color;
	}

	private int GetColorIndex(decimal cci)
	{
		if (cci > MiddleLevel)
			return cci > HighLevel ? 0 : 1;
		if (cci < MiddleLevel)
			return cci < LowLevel ? 4 : 3;
		return 2;
	}
}

