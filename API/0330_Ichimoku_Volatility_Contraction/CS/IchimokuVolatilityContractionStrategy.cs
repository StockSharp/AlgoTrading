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
/// Ichimoku with Volatility Contraction strategy.
/// Enters positions when Ichimoku signals a trend and volatility is contracting.
/// </summary>
public class IchimokuVolatilityContractionStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _deviationFactor;
	private readonly StrategyParam<DataType> _candleType;
	private static readonly object _sync = new();
	
	private decimal _avgAtr;
	private decimal _atrStdDev;
	private int _processedCandles;

	/// <summary>
	/// Tenkan-sen (Conversion Line) period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen (Base Line) period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B (Leading Span B) period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for volatility calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Deviation factor for volatility contraction detection.
	/// </summary>
	public decimal DeviationFactor
	{
		get => _deviationFactor.Value;
		set => _deviationFactor.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public IchimokuVolatilityContractionStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Period for Tenkan-sen (Conversion Line)", "Ichimoku Settings")
			
			.SetOptimize(7, 11, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Period for Kijun-sen (Base Line)", "Ichimoku Settings")
			
			.SetOptimize(20, 30, 2);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Period for Senkou Span B (Leading Span B)", "Ichimoku Settings")
			
			.SetOptimize(40, 60, 4);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for Average True Range calculation", "Volatility Settings")
			
			.SetOptimize(10, 20, 2);

		_deviationFactor = Param(nameof(DeviationFactor), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Factor", "Factor multiplied by standard deviation to detect volatility contraction", "Volatility Settings")
			
			.SetOptimize(1.5m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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

		_avgAtr = 0;
		_atrStdDev = 0;
		_processedCandles = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create Ichimoku indicator
		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		// Create ATR indicator for volatility measurement
		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(candle => ProcessCandle(candle, ichimoku, atr))
			.Start();

		// Start position protection
		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, Ichimoku ichimoku, AverageTrueRange atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		lock (_sync)
		{
			var ichimokuValue = ichimoku.Process(new CandleIndicatorValue(ichimoku, candle) { IsFinal = true });
			var atrValue = atr.Process(new CandleIndicatorValue(atr, candle) { IsFinal = true });
			if (!ichimokuValue.IsFinal || !atrValue.IsFinal || !ichimoku.IsFormed || !atr.IsFormed)
				return;

			var currentAtr = atrValue.ToDecimal();
			_processedCandles++;

			if (_processedCandles == 1)
			{
				_avgAtr = currentAtr;
				_atrStdDev = 0m;
			}
			else
			{
				var alpha = 2.0m / (AtrPeriod + 1);
				var oldAvg = _avgAtr;
				_avgAtr = alpha * currentAtr + (1 - alpha) * _avgAtr;
				var atrDev = Math.Abs(currentAtr - oldAvg);
				_atrStdDev = alpha * atrDev + (1 - alpha) * _atrStdDev;
			}

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (ichimokuValue is not IchimokuValue ichimokuTyped ||
				ichimokuTyped.Tenkan is not decimal tenkan ||
				ichimokuTyped.Kijun is not decimal kijun ||
				ichimokuTyped.SenkouA is not decimal senkouA ||
				ichimokuTyped.SenkouB is not decimal senkouB)
			{
				return;
			}

			var upperKumo = Math.Max(senkouA, senkouB);
			var lowerKumo = Math.Min(senkouA, senkouB);
			var isVolatilityContraction = currentAtr <= _avgAtr;

			if (isVolatilityContraction)
			{
				if (candle.ClosePrice > upperKumo && tenkan > kijun && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
				else if (candle.ClosePrice < lowerKumo && tenkan < kijun && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}

			if (Position > 0 && candle.ClosePrice < lowerKumo)
				SellMarket(Position);
			else if (Position < 0 && candle.ClosePrice > upperKumo)
				BuyMarket(-Position);
		}
	}
}
