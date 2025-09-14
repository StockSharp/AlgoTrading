using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on relative change of price to its SMA.
/// </summary>
public class ImaExpertStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<decimal> _signalLevel;
	private readonly StrategyParam<decimal> _riskLevel;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousIma;

	/// <summary>
	/// Period of the SMA indicator.
	/// </summary>
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	/// <summary>
	/// Trailing step in ticks used for profit protection.
	/// </summary>
	public int TakeProfitTicks { get => _takeProfitTicks.Value; set => _takeProfitTicks.Value = value; }

	/// <summary>
	/// Stop loss size in ticks.
	/// </summary>
	public int StopLossTicks { get => _stopLossTicks.Value; set => _stopLossTicks.Value = value; }

	/// <summary>
	/// Threshold for signal generation.
	/// </summary>
	public decimal SignalLevel { get => _signalLevel.Value; set => _signalLevel.Value = value; }

	/// <summary>
	/// Risk level used for position sizing.
	/// </summary>
	public decimal RiskLevel { get => _riskLevel.Value; set => _riskLevel.Value = value; }

	/// <summary>
	/// Maximum allowed volume.
	/// </summary>
	public decimal MaxVolume { get => _maxVolume.Value; set => _maxVolume.Value = value; }

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ImaExpertStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Length of moving average", "Parameters");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 50)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Ticks", "Trailing step size", "Parameters");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Ticks", "Maximum adverse move before close", "Parameters");

		_signalLevel = Param(nameof(SignalLevel), 0.5m)
			.SetDisplay("Signal Level", "IMA change threshold", "Parameters");

		_riskLevel = Param(nameof(RiskLevel), 0.01m)
			.SetDisplay("Risk Level", "Fraction of equity used for a trade", "Parameters");

		_maxVolume = Param(nameof(MaxVolume), 1m)
			.SetDisplay("Max Volume", "Upper limit for order volume", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (smaValue == 0)
			return;

		var price = candle.ClosePrice;
		var ima = price / smaValue - 1m;

		if (_previousIma is null || _previousIma.Value == 0)
		{
			_previousIma = ima;
			return;
		}

		var k1 = (ima - _previousIma.Value) / Math.Abs(_previousIma.Value);
		_previousIma = ima;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			if (k1 >= SignalLevel)
			{
				BuyMarket(GetVolume());
			}
			else if (k1 <= -SignalLevel)
			{
				SellMarket(GetVolume());
			}
		}
		else if (Position > 0 && k1 <= -SignalLevel)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && k1 >= SignalLevel)
		{
			BuyMarket(-Position);
		}
	}

	private decimal GetVolume()
	{
		var portfolio = Portfolio;
		var tickCost = Security.PriceStepCost ?? 0m;
		var volumeStep = Security.VolumeStep ?? 1m;
		var volume = Volume;

		if (portfolio != null && tickCost > 0m && StopLossTicks > 0)
		{
			volume = portfolio.CurrentBalance * RiskLevel / StopLossTicks / tickCost;
			volume = Math.Floor(volume / volumeStep) * volumeStep;
			if (MaxVolume > 0m)
				volume = Math.Min(volume, MaxVolume);
			var maxVol = Security.MaxVolume;
			if (maxVol != null)
				volume = Math.Min(volume, maxVol.Value);
			if (volume < volumeStep)
				volume = volumeStep;
		}

		return volume;
	}
}

