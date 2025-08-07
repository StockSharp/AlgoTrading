namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Improvisando Strategy
/// </summary>
public class ImprovisandoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<decimal> _slPercent;

	private ExponentialMovingAverage _ema;
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _macdFast;
	private ExponentialMovingAverage _macdSlow;

	private decimal? _entryPrice;
	private decimal _trailingStopLong;
	private decimal _trailingStopShort;
	private decimal _prevMacd;
	private bool _prevBullishCandle;
	private bool _prevBearishCandle;
	
	// Хранение данных предыдущей свечи
	private decimal _prevClose;
	private decimal _prevOpen;

	public ImprovisandoStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_emaLength = Param(nameof(EmaLength), 10)
			.SetDisplay("EMA Length", "EMA period", "Moving Averages");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long entries", "Enable long positions", "Strategy");

		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short entries", "Enable short positions", "Strategy");

		_tpPercent = Param(nameof(TpPercent), 1.2m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Take Profit");

		_slPercent = Param(nameof(SlPercent), 1.8m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Stop Loss");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	public decimal TpPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
	}

	public decimal SlPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[] { (Security, CandleType) };

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevClose = default;
		_prevOpen = default;
		_prevBullishCandle = default;
		_prevBearishCandle = default;
		_prevMacd = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_macdFast = new ExponentialMovingAverage { Length = 12 };
		_macdSlow = new ExponentialMovingAverage { Length = 26 };

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ema, _rsi, _macdFast, _macdSlow, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema, System.Drawing.Color.Purple);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue rsiValue, IIndicatorValue macdFastValue, IIndicatorValue macdSlowValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_ema.IsFormed || !_rsi.IsFormed || !_macdFast.IsFormed || !_macdSlow.IsFormed)
		{
			// Store current candle data for next iteration
			_prevClose = candle.ClosePrice;
			_prevOpen = candle.OpenPrice;
			_prevBullishCandle = candle.ClosePrice > candle.OpenPrice;
			_prevBearishCandle = candle.ClosePrice < candle.OpenPrice;
			_prevMacd = macdFastValue.ToDecimal() - macdSlowValue.ToDecimal();
			return;
		}

		// Calculate MACD
		var macd = macdFastValue.ToDecimal() - macdSlowValue.ToDecimal();
		var emaPrice = emaValue.ToDecimal();
		var rsiPrice = rsiValue.ToDecimal();

		// Check candle patterns
		var currentBullish = candle.ClosePrice > candle.OpenPrice;
		var currentBearish = candle.ClosePrice < candle.OpenPrice;

		// Pattern: previous red candle and current green candle crosses above previous high
		var buyPattern = _prevBearishCandle && candle.ClosePrice > _prevOpen;
		
		// Pattern: previous green candle and current red candle crosses below previous low
		var sellPattern = _prevBullishCandle && candle.ClosePrice < _prevOpen;

		// Entry conditions
		var entryLong = buyPattern && 
						candle.ClosePrice > emaPrice && 
						_prevClose > emaPrice &&
						rsiPrice < 65 && 
						macd > _prevMacd;

		var entryShort = sellPattern && 
						 candle.ClosePrice < emaPrice && 
						 _prevClose < emaPrice &&
						 rsiPrice > 35 && 
						 macd < _prevMacd;

		// Calculate take profit and stop loss levels
		if (Position != 0 && _entryPrice.HasValue)
		{
			var longTP = _entryPrice.Value * (1 + TpPercent / 100);
			var shortTP = _entryPrice.Value * (1 - TpPercent / 100);
			var longStop = _entryPrice.Value * (1 - SlPercent / 100);
			var shortStop = _entryPrice.Value * (1 + SlPercent / 100);

			// Update trailing stops
			if (Position > 0)
			{
				var avgTP = (longTP + _entryPrice.Value) / 2;
				if (candle.ClosePrice > avgTP)
				{
					_trailingStopLong = Math.Max(_trailingStopLong, _entryPrice.Value * 1.002m);
				}
				else
				{
					_trailingStopLong = Math.Max(_trailingStopLong, longStop);
				}

				// Check exit conditions
				if (candle.ClosePrice >= longTP || candle.ClosePrice <= _trailingStopLong)
				{
					ClosePosition();
					_entryPrice = null;
					_trailingStopLong = 0;
				}
			}
			else if (Position < 0)
			{
				var avgTP = (shortTP + _entryPrice.Value) / 2;
				if (candle.ClosePrice < avgTP)
				{
					_trailingStopShort = Math.Min(_trailingStopShort == 0 ? decimal.MaxValue : _trailingStopShort, 
												  _entryPrice.Value * 0.998m);
				}
				else
				{
					_trailingStopShort = Math.Min(_trailingStopShort == 0 ? decimal.MaxValue : _trailingStopShort, 
												  shortStop);
				}

				// Check exit conditions
				if (candle.ClosePrice <= shortTP || candle.ClosePrice >= _trailingStopShort)
				{
					ClosePosition();
					_entryPrice = null;
					_trailingStopShort = 0;
				}
			}
		}

		// Execute new trades
		if (ShowLong && entryLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_trailingStopLong = _entryPrice.Value * (1 - SlPercent / 100);
		}
		else if (ShowShort && entryShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_trailingStopShort = _entryPrice.Value * (1 + SlPercent / 100);
		}

		// Update state for next candle
		_prevMacd = macd;
		_prevBullishCandle = currentBullish;
		_prevBearishCandle = currentBearish;
		_prevClose = candle.ClosePrice;
		_prevOpen = candle.OpenPrice;
	}
}