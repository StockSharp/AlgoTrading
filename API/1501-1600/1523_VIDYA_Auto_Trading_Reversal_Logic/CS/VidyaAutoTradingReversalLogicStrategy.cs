using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VIDYA Auto-Trading strategy based on reversal logic.
/// Uses Variable Index Dynamic Average with ATR bands and reverses on band breakouts.
/// </summary>
public class VidyaAutoTradingReversalLogicStrategy : Strategy
{
	private readonly StrategyParam<int> _vidyaLength;
	private readonly StrategyParam<int> _vidyaMomentum;
	private readonly StrategyParam<decimal> _bandDistance;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _vidya;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;
	private int _cooldown;

	public int VidyaLength { get => _vidyaLength.Value; set => _vidyaLength.Value = value; }
	public int VidyaMomentum { get => _vidyaMomentum.Value; set => _vidyaMomentum.Value = value; }
	public decimal BandDistance { get => _bandDistance.Value; set => _bandDistance.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VidyaAutoTradingReversalLogicStrategy()
	{
		_vidyaLength = Param(nameof(VidyaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("VIDYA Length", "Length of VIDYA", "General");

		_vidyaMomentum = Param(nameof(VidyaMomentum), 20)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Length for momentum", "General");

		_bandDistance = Param(nameof(BandDistance), 3m)
			.SetDisplay("Band Distance", "ATR multiplier for bands", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_vidya = null;
		_prevUpper = 0;
		_prevLower = 0;
		_prevClose = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cmo = new ChandeMomentumOscillator { Length = VidyaMomentum };
		var atr = new AverageTrueRange { Length = 14 };

		_vidya = null;
		_prevUpper = 0;
		_prevLower = 0;
		_prevClose = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cmo, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cmoValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldown > 0)
			_cooldown--;

		var alpha = 2m / (VidyaLength + 1);
		var absCmo = Math.Abs(cmoValue);
		var prev = _vidya ?? candle.ClosePrice;
		_vidya = alpha * absCmo / 100m * candle.ClosePrice + (1 - alpha * absCmo / 100m) * prev;

		var upper = _vidya.Value + atrValue * BandDistance;
		var lower = _vidya.Value - atrValue * BandDistance;

		if (_prevClose != 0 && _cooldown <= 0)
		{
			var trendCrossUp = _prevClose <= _prevUpper && candle.ClosePrice > upper;
			var trendCrossDown = _prevClose >= _prevLower && candle.ClosePrice < lower;

			if (trendCrossUp && Position <= 0)
			{
				BuyMarket();
				_cooldown = 25;
			}
			else if (trendCrossDown && Position >= 0)
			{
				SellMarket();
				_cooldown = 25;
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}
}
