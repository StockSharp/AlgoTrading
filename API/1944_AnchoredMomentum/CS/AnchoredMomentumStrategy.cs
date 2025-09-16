using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Anchored Momentum indicator.
/// Calculates the ratio between EMA and SMA to detect trend strength.
/// Opens long positions when momentum rises above the upper level and
/// opens short positions when momentum falls below the lower level.
/// </summary>
public class AnchoredMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyEnabled;
	private readonly StrategyParam<bool> _sellEnabled;

	private decimal _previousMomentum;
	private bool _isFirstValue;

	/// <summary>
	/// Period for the simple moving average.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the exponential moving average.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Upper threshold for the momentum.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for the momentum.
	/// </summary>
	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
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
	/// Allow opening long positions.
	/// </summary>
	public bool BuyEnabled
	{
		get => _buyEnabled.Value;
		set => _buyEnabled.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellEnabled
	{
		get => _sellEnabled.Value;
		set => _sellEnabled.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AnchoredMomentumStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Period of the simple moving average", "Indicator");

		_emaPeriod = Param(nameof(EmaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period of the exponential moving average", "Indicator");

		_upLevel = Param(nameof(UpLevel), 0.025m)
			.SetDisplay("Upper Level", "Upper threshold for momentum", "Indicator");

		_downLevel = Param(nameof(DownLevel), -0.025m)
			.SetDisplay("Lower Level", "Lower threshold for momentum", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used by the strategy", "General");

		_buyEnabled = Param(nameof(BuyEnabled), true)
			.SetDisplay("Enable Buy", "Allow opening long positions", "Trading");

		_sellEnabled = Param(nameof(SellEnabled), true)
			.SetDisplay("Enable Sell", "Allow opening short positions", "Trading");
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
		_previousMomentum = 0m;
		_isFirstValue = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SMA { Length = SmaPeriod };
		var ema = new EMA { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var momentum = smaValue == 0m ? 0m : 100m * (emaValue / smaValue - 1m);

		if (_isFirstValue)
		{
			_previousMomentum = momentum;
			_isFirstValue = false;
			return;
		}

		if (_previousMomentum <= UpLevel && momentum > UpLevel)
		{
			if (SellEnabled && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (BuyEnabled && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_previousMomentum >= DownLevel && momentum < DownLevel)
		{
			if (BuyEnabled && Position > 0)
				SellMarket(Math.Abs(Position));

			if (SellEnabled && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_previousMomentum = momentum;
	}
}
