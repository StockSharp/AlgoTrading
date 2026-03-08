using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku cloud strategy.
/// Buys when Kijun crosses down into cloud, sells on opposite.
/// </summary>
public class Exp3XmaIshimokuStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevKijun;
	private decimal? _prevUpper;
	private decimal? _prevLower;

	public int TenkanPeriod { get => _tenkanPeriod.Value; set => _tenkanPeriod.Value = value; }
	public int KijunPeriod { get => _kijunPeriod.Value; set => _kijunPeriod.Value = value; }
	public int SenkouSpanPeriod { get => _senkouSpanPeriod.Value; set => _senkouSpanPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Exp3XmaIshimokuStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku");

		_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Senkou B Period", "Senkou Span B period", "Ichimoku");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevKijun = null;
		_prevUpper = null;
		_prevLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ichimoku = new Ichimoku();
		ichimoku.Tenkan.Length = TenkanPeriod;
		ichimoku.Kijun.Length = KijunPeriod;
		ichimoku.SenkouB.Length = SenkouSpanPeriod;

		SubscribeCandles(CandleType)
			.BindEx(ichimoku, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ich = (IIchimokuValue)ichimokuValue;

		if (ich.Kijun is not decimal kijun ||
			ich.SenkouA is not decimal senkouA ||
			ich.SenkouB is not decimal senkouB)
			return;

		var upper = Math.Max(senkouA, senkouB);
		var lower = Math.Min(senkouA, senkouB);

		if (_prevKijun is null)
		{
			_prevKijun = kijun;
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		// Buy when Kijun crosses down into cloud
		var crossDown = _prevKijun > _prevUpper && kijun <= upper;
		// Sell when Kijun crosses up out of cloud
		var crossUp = _prevKijun < _prevLower && kijun >= lower;

		if (crossDown && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossUp && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevKijun = kijun;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
