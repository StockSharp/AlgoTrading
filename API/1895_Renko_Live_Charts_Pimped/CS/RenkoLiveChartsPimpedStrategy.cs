using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko strategy with optional ATR-based brick size.
/// Simulates renko bricks from regular candles and trades on direction changes.
/// When ATR mode is enabled, the brick size is derived from ATR of a larger timeframe.
/// </summary>
public class RenkoLiveChartsPimpedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _boxSize;
	private readonly StrategyParam<bool> _calculateBestBoxSize;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<bool> _useAtrMa;
	private readonly StrategyParam<int> _atrMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrMa;
	private decimal _renkoPrice;
	private int _prevDirection;
	private decimal _dynamicBoxSize;

	/// <summary>
	/// Renko brick size in price units.
	/// </summary>
	public decimal BoxSize { get => _boxSize.Value; set => _boxSize.Value = value; }

	/// <summary>
	/// Calculate brick size from ATR.
	/// </summary>
	public bool CalculateBestBoxSize { get => _calculateBestBoxSize.Value; set => _calculateBestBoxSize.Value = value; }

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Apply moving average to ATR.
	/// </summary>
	public bool UseAtrMa { get => _useAtrMa.Value; set => _useAtrMa.Value = value; }

	/// <summary>
	/// Moving average length for ATR.
	/// </summary>
	public int AtrMaPeriod { get => _atrMaPeriod.Value; set => _atrMaPeriod.Value = value; }

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes <see cref="RenkoLiveChartsPimpedStrategy"/>.
	/// </summary>
	public RenkoLiveChartsPimpedStrategy()
	{
		_boxSize = Param(nameof(BoxSize), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Box Size", "Renko brick size", "Renko");

		_calculateBestBoxSize = Param(nameof(CalculateBestBoxSize), false)
			.SetDisplay("Use ATR Box", "Calculate brick size from ATR", "Renko");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Renko");

		_useAtrMa = Param(nameof(UseAtrMa), false)
			.SetDisplay("Smooth ATR", "Apply moving average on ATR", "Renko");

		_atrMaPeriod = Param(nameof(AtrMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR MA Period", "Moving average length for ATR", "Renko");

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
		_renkoPrice = 0m;
		_prevDirection = 0;
		_dynamicBoxSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_dynamicBoxSize = BoxSize;

		if (CalculateBestBoxSize)
		{
			_atr = new AverageTrueRange { Length = AtrPeriod };
			_atrMa = new SimpleMovingAverage { Length = AtrMaPeriod };
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(null, null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// update dynamic box size from ATR if enabled
		if (CalculateBestBoxSize && _atr != null)
		{
			var atrResult = _atr.Process(candle);
			if (atrResult.IsFormed)
			{
				var atrVal = atrResult.ToDecimal();
				if (UseAtrMa && _atrMa != null)
				{
					var maResult = _atrMa.Process(atrVal, candle.OpenTime, true);
					if (maResult.IsFormed)
						_dynamicBoxSize = maResult.ToDecimal();
				}
				else
				{
					_dynamicBoxSize = atrVal;
				}
			}
		}

		var close = candle.ClosePrice;
		var size = _dynamicBoxSize;

		if (size <= 0)
			return;

		if (_renkoPrice == 0m)
		{
			_renkoPrice = close;
			return;
		}

		var diff = close - _renkoPrice;
		if (Math.Abs(diff) < size)
			return;

		var direction = Math.Sign(diff);
		_renkoPrice += direction * size;

		// trade on direction change
		if (_prevDirection != 0 && direction != _prevDirection)
		{
			if (direction > 0 && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (direction < 0 && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}
		else if (_prevDirection == 0)
		{
			// first brick - enter in its direction
			if (direction > 0)
				BuyMarket();
			else if (direction < 0)
				SellMarket();
		}

		_prevDirection = direction;
	}
}
