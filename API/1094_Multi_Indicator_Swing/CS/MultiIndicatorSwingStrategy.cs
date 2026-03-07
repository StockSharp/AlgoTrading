using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi indicator swing strategy using RSI and SMA for direction.
/// </summary>
public class MultiIndicatorSwingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _signalCooldownBars;

	private decimal? _prevClose;
	private decimal? _prevSma;
	private decimal? _prevRsi;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public MultiIndicatorSwingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_smaLength = Param(nameof(SmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period", "Indicators");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");
		_rsiOversold = Param(nameof(RsiOversold), 45m)
			.SetDisplay("RSI Oversold", "RSI oversold", "Indicators");
		_rsiOverbought = Param(nameof(RsiOverbought), 55m)
			.SetDisplay("RSI Overbought", "RSI overbought", "Indicators");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between new entries", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevClose = null;
		_prevSma = null;
		_prevRsi = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = SmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, rsi, (candle, smaVal, rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (_cooldownRemaining > 0)
					_cooldownRemaining--;

				var previousClose = _prevClose;
				var previousSma = _prevSma;
				var previousRsi = _prevRsi;

				if (previousClose.HasValue && previousSma.HasValue && previousRsi.HasValue)
				{
					var trendUp = candle.ClosePrice > smaVal && smaVal > previousSma.Value;
					var trendDown = candle.ClosePrice < smaVal && smaVal < previousSma.Value;
					var crossAboveSma = previousClose.Value <= previousSma.Value && candle.ClosePrice > smaVal;
					var crossBelowSma = previousClose.Value >= previousSma.Value && candle.ClosePrice < smaVal;
					var longEntry = trendUp && crossAboveSma && previousRsi.Value <= RsiOversold && rsiVal > RsiOversold;
					var shortEntry = trendDown && crossBelowSma && previousRsi.Value >= RsiOverbought && rsiVal < RsiOverbought;

					if (_cooldownRemaining == 0)
					{
						if (longEntry && Position <= 0)
						{
							var volume = Volume + Math.Abs(Position);
							BuyMarket(volume);
							_cooldownRemaining = SignalCooldownBars;
						}
						else if (shortEntry && Position >= 0)
						{
							var volume = Volume + Math.Abs(Position);
								SellMarket(volume);
							_cooldownRemaining = SignalCooldownBars;
						}
					}
				}

				_prevClose = candle.ClosePrice;
				_prevSma = smaVal;
				_prevRsi = rsiVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
