using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AI Volume Strategy - trades volume spikes in trend direction.
/// Uses EMA for trend and volume EMA for spike detection.
/// </summary>
public class AiVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _priceEmaLength;
	private readonly StrategyParam<int> _volumeEmaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _exitBars;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _volumeSma;
	private int _barsInPosition;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int PriceEmaLength { get => _priceEmaLength.Value; set => _priceEmaLength.Value = value; }
	public int VolumeEmaLength { get => _volumeEmaLength.Value; set => _volumeEmaLength.Value = value; }
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
	public int ExitBars { get => _exitBars.Value; set => _exitBars.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AiVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_priceEmaLength = Param(nameof(PriceEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Price EMA Length", "Length for price EMA", "Parameters");

		_volumeEmaLength = Param(nameof(VolumeEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume EMA Length", "Length for volume EMA", "Parameters");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.0m)
			.SetDisplay("Volume Multiplier", "Multiplier for volume spike detection", "Parameters");

		_exitBars = Param(nameof(ExitBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Exit Bars", "Exit position after this many bars", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_volumeSma = null;
		_barsInPosition = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var priceEma = new ExponentialMovingAverage { Length = PriceEmaLength };
		_volumeSma = new SimpleMovingAverage { Length = VolumeEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(priceEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, priceEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal priceEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeResult = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.ServerTime));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Time-based exit
		if (Position != 0)
		{
			_barsInPosition++;
			if (_barsInPosition >= ExitBars)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));
				_barsInPosition = 0;
				_cooldownRemaining = CooldownBars;
				return;
			}
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var avgVolume = _volumeSma.IsFormed ? volumeResult.ToDecimal() : 0m;
		var volumeSpike = avgVolume > 0 && candle.TotalVolume > avgVolume * VolumeMultiplier;
		// If no volume data, use price action only
		var useVolumeFilter = avgVolume > 0;

		var trendUp = candle.ClosePrice > priceEmaValue;
		var trendDown = candle.ClosePrice < priceEmaValue;
		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		var longOk = trendUp && isBullish && (!useVolumeFilter || volumeSpike);
		var shortOk = trendDown && isBearish && (!useVolumeFilter || volumeSpike);

		if (longOk && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_barsInPosition = 0;
			_cooldownRemaining = CooldownBars;
		}
		else if (shortOk && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_barsInPosition = 0;
			_cooldownRemaining = CooldownBars;
		}
	}
}
