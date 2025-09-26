namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class NatusekoProtrader4HStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _trendEmaPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _macdSmaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiEntryLevel;
	private readonly StrategyParam<decimal> _rsiTakeProfitLong;
	private readonly StrategyParam<decimal> _rsiTakeProfitShort;
	private readonly StrategyParam<decimal> _distanceThresholdPoints;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<bool> _useSarStopLoss;
	private readonly StrategyParam<bool> _useTrendStopLoss;
	private readonly StrategyParam<int> _stopOffsetPoints;
	private readonly StrategyParam<bool> _useSarTakeProfit;
	private readonly StrategyParam<bool> _useRsiTakeProfit;
	private readonly StrategyParam<decimal> _minimumProfitPoints;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private ExponentialMovingAverage _trendEma;
	private MovingAverageConvergenceDivergence? _macd;
	private BollingerBands? _macdBands;
	private SimpleMovingAverage _macdSma;
	private RelativeStrengthIndex? _rsi;
	private ParabolicSar? _parabolicSar;

	private bool _waitingForLongEntry;
	private bool _waitingForShortEntry;
	private bool _longPartialExecuted;
	private bool _shortPartialExecuted;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	public NatusekoProtrader4HStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Default order size used for entries.", "Trading");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA period", "Length of the fast EMA filter.", "Indicator");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA period", "Length of the slower EMA filter.", "Indicator");

		_trendEmaPeriod = Param(nameof(TrendEmaPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA period", "Length of the trend EMA used for filters and stop loss.", "Indicator");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("MACD fast period", "Fast EMA length inside the MACD indicator.", "Indicator");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("MACD slow period", "Slow EMA length inside the MACD indicator.", "Indicator");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("MACD signal period", "Signal moving average length for MACD.", "Indicator");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger period", "Number of MACD samples used for the Bollinger Bands.", "Indicator");

		_bollingerWidth = Param(nameof(BollingerWidth), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger width", "Standard deviation multiplier for the MACD Bollinger Bands.", "Indicator");

		_macdSmaPeriod = Param(nameof(MacdSmaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("MACD SMA period", "Length of the moving average applied to the MACD line.", "Indicator");

		_rsiPeriod = Param(nameof(RsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI period", "Length of the RSI filter.", "Indicator");

		_rsiEntryLevel = Param(nameof(RsiEntryLevel), 50m)
			.SetDisplay("RSI neutral level", "Central RSI threshold used for both entry and exit rules.", "Trading");

		_rsiTakeProfitLong = Param(nameof(RsiTakeProfitLong), 65m)
			.SetDisplay("RSI take profit long", "RSI value that triggers a partial exit for long positions.", "Trading");

		_rsiTakeProfitShort = Param(nameof(RsiTakeProfitShort), 35m)
			.SetDisplay("RSI take profit short", "RSI value that triggers a partial exit for short positions.", "Trading");

		_distanceThresholdPoints = Param(nameof(DistanceThresholdPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Distance threshold", "Maximum distance in points between price and the trend EMA before delaying the entry.", "Trading");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR step", "Acceleration step of the Parabolic SAR indicator.", "Indicator");

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR maximum", "Maximum acceleration of the Parabolic SAR indicator.", "Indicator");

		_useSarStopLoss = Param(nameof(UseSarStopLoss), false)
			.SetDisplay("Use SAR stop loss", "Whether the Parabolic SAR defines the protective stop price.", "Risk");

		_useTrendStopLoss = Param(nameof(UseTrendStopLoss), true)
			.SetDisplay("Use trend stop loss", "Whether the trend EMA defines the protective stop price.", "Risk");

		_stopOffsetPoints = Param(nameof(StopOffsetPoints), 0)
			.SetNotNegative()
			.SetDisplay("Stop offset", "Additional point offset added to the protective stop.", "Risk");

		_useSarTakeProfit = Param(nameof(UseSarTakeProfit), true)
			.SetDisplay("Use SAR take profit", "Enable partial exits when price crosses the Parabolic SAR.", "Risk");

		_useRsiTakeProfit = Param(nameof(UseRsiTakeProfit), true)
			.SetDisplay("Use RSI take profit", "Enable partial exits driven by RSI thresholds.", "Risk");

		_minimumProfitPoints = Param(nameof(MinimumProfitPoints), 5m)
			.SetNotNegative()
			.SetDisplay("Minimum profit", "Minimum profit in points required before take-profit rules activate.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	public int TrendEmaPeriod
	{
		get => _trendEmaPeriod.Value;
		set => _trendEmaPeriod.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	public int MacdSmaPeriod
	{
		get => _macdSmaPeriod.Value;
		set => _macdSmaPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiEntryLevel
	{
		get => _rsiEntryLevel.Value;
		set => _rsiEntryLevel.Value = value;
	}

	public decimal RsiTakeProfitLong
	{
		get => _rsiTakeProfitLong.Value;
		set => _rsiTakeProfitLong.Value = value;
	}

	public decimal RsiTakeProfitShort
	{
		get => _rsiTakeProfitShort.Value;
		set => _rsiTakeProfitShort.Value = value;
	}

	public decimal DistanceThresholdPoints
	{
		get => _distanceThresholdPoints.Value;
		set => _distanceThresholdPoints.Value = value;
	}

	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	public bool UseSarStopLoss
	{
		get => _useSarStopLoss.Value;
		set => _useSarStopLoss.Value = value;
	}

	public bool UseTrendStopLoss
	{
		get => _useTrendStopLoss.Value;
		set => _useTrendStopLoss.Value = value;
	}

	public int StopOffsetPoints
	{
		get => _stopOffsetPoints.Value;
		set => _stopOffsetPoints.Value = value;
	}

	public bool UseSarTakeProfit
	{
		get => _useSarTakeProfit.Value;
		set => _useSarTakeProfit.Value = value;
	}

	public bool UseRsiTakeProfit
	{
		get => _useRsiTakeProfit.Value;
		set => _useRsiTakeProfit.Value = value;
	}

	public decimal MinimumProfitPoints
	{
		get => _minimumProfitPoints.Value;
		set => _minimumProfitPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_waitingForLongEntry = false;
		_waitingForShortEntry = false;
		_longPartialExecuted = false;
		_shortPartialExecuted = false;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		_trendEma = new ExponentialMovingAverage { Length = TrendEmaPeriod };

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		_macdBands = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};

		_macdSma = new SimpleMovingAverage { Length = MacdSmaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaximum
		};

		_waitingForLongEntry = false;
		_waitingForShortEntry = false;
		_longPartialExecuted = false;
		_shortPartialExecuted = false;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _trendEma);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}

		var macdArea = CreateChartArea("MACD");
		if (macdArea != null)
		{
			DrawIndicator(macdArea, _macd);
			DrawIndicator(macdArea, _macdBands);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_fastEma == null || _slowEma == null || _trendEma == null || _macd == null || _macdBands == null ||
				_macdSma == null || _rsi == null || _parabolicSar == null)
		{
			return;
		}

		var fastEmaValue = _fastEma.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
		var slowEmaValue = _slowEma.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
		var trendEmaValue = _trendEma.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
		var rsiValue = _rsi.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

		var sarRaw = _parabolicSar.Process(new CandleIndicatorValue(_parabolicSar, candle));
		if (!sarRaw.IsFinal)
			return;
		var sarValue = sarRaw.ToDecimal();

		var macdRaw = _macd.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!macdRaw.IsFinal || macdRaw is not MovingAverageConvergenceDivergenceValue macdValue ||
				macdValue.Macd is not decimal macdLine)
		{
			return;
		}

		var macdSmaRaw = _macdSma.Process(macdLine, candle.OpenTime, true);
		if (!macdSmaRaw.IsFinal)
			return;
		var macdSmaValue = macdSmaRaw.ToDecimal();

		var bandsRaw = _macdBands.Process(macdLine, candle.OpenTime, true);
		if (!bandsRaw.IsFinal || bandsRaw is not BollingerBandsValue bandsValue ||
				bandsValue.MovingAverage is not decimal macdMiddle)
		{
			return;
		}

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_trendEma.IsFormed || !_macd.IsFormed || !_macdSma.IsFormed ||
				!_macdBands.IsFormed || !_rsi.IsFormed || !_parabolicSar.IsFormed)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;
		var stopOffset = StopOffsetPoints * priceStep;

		if (Position > 0)
		{
			var updatedStop = CalculateStopPrice(true, sarValue, trendEmaValue, stopOffset);
			if (updatedStop.HasValue)
				_longStopPrice = updatedStop;
		}
		else if (Position < 0)
		{
			var updatedStop = CalculateStopPrice(false, sarValue, trendEmaValue, stopOffset);
			if (updatedStop.HasValue)
				_shortStopPrice = updatedStop;
		}

		ManageOpenPositions(candle, rsiValue, sarValue, priceStep);

		if (Position > 0)
			_waitingForLongEntry = false;
		if (Position < 0)
			_waitingForShortEntry = false;

		TryEnterLong(candle, fastEmaValue, slowEmaValue, trendEmaValue, rsiValue, macdLine, macdSmaValue, macdMiddle, sarValue, priceStep);
		TryEnterShort(candle, fastEmaValue, slowEmaValue, trendEmaValue, rsiValue, macdLine, macdSmaValue, macdMiddle, sarValue, priceStep);
	}

	private void ManageOpenPositions(ICandleMessage candle, decimal rsiValue, decimal sarValue, decimal priceStep)
	{
		var profitThreshold = MinimumProfitPoints * priceStep;

		if (Position > 0)
		{
			var volume = Math.Abs(Position);
			if (volume > 0m)
			{
				if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
				{
					SellMarket(volume);
					ResetLongState();
				}
				else
				{
					var entry = _longEntryPrice ?? candle.ClosePrice;
					_longEntryPrice ??= entry;

					var profit = candle.ClosePrice - entry;

					if ((profitThreshold <= 0m || profit >= profitThreshold) && !_longPartialExecuted)
					{
						if (UseRsiTakeProfit && rsiValue >= RsiTakeProfitLong)
						{
							CloseHalfLong(volume);
						}
						else if (UseSarTakeProfit && sarValue >= candle.ClosePrice)
						{
							CloseHalfLong(volume);
						}
					}

					if ((profitThreshold <= 0m || profit >= profitThreshold) && rsiValue <= RsiEntryLevel)
					{
						SellMarket(volume);
						ResetLongState();
					}
				}
			}
		}
		else
		{
			ResetLongState();
		}

		if (Position < 0)
		{
			var volume = Math.Abs(Position);
			if (volume > 0m)
			{
				if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
				{
					BuyMarket(volume);
					ResetShortState();
				}
				else
				{
					var entry = _shortEntryPrice ?? candle.ClosePrice;
					_shortEntryPrice ??= entry;

					var profit = entry - candle.ClosePrice;

					if ((profitThreshold <= 0m || profit >= profitThreshold) && !_shortPartialExecuted)
					{
						if (UseRsiTakeProfit && rsiValue <= RsiTakeProfitShort)
						{
							CloseHalfShort(volume);
						}
						else if (UseSarTakeProfit && sarValue <= candle.ClosePrice)
						{
							CloseHalfShort(volume);
						}
					}

					if ((profitThreshold <= 0m || profit >= profitThreshold) && rsiValue >= RsiEntryLevel)
					{
						BuyMarket(volume);
						ResetShortState();
					}
				}
			}
		}
		else
		{
			ResetShortState();
		}
	}

	private void CloseHalfLong(decimal volume)
	{
		var half = volume / 2m;
		if (half <= 0m)
			return;

		SellMarket(half);
		_longPartialExecuted = true;
	}

	private void CloseHalfShort(decimal volume)
	{
		var half = volume / 2m;
		if (half <= 0m)
			return;

		BuyMarket(half);
		_shortPartialExecuted = true;
	}

	private void TryEnterLong(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal trendEma, decimal rsi, decimal macdLine,
		decimal macdSma, decimal macdMiddle, decimal sar, decimal priceStep)
	{
		if (Position > 0)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var upperShadow = Math.Abs(candle.HighPrice - candle.ClosePrice);
		var lowerShadow = Math.Abs(candle.OpenPrice - candle.LowPrice);

		var baseCondition = fastEma > slowEma && fastEma > trendEma && rsi > RsiEntryLevel && rsi < RsiTakeProfitLong &&
			macdLine > macdSma && macdLine > macdMiddle && macdSma > macdMiddle && body > 0m && body > lowerShadow && body > upperShadow &&
			sar < candle.ClosePrice;

		if (baseCondition)
		{
			var distanceLimit = DistanceThresholdPoints * priceStep;
			var distance = candle.ClosePrice - trendEma;

			if (distanceLimit > 0m && distance >= distanceLimit)
			{
				_waitingForLongEntry = true;
			}
			else
			{
				OpenLong(candle, sar, trendEma, priceStep);
			}
		}
		else if (_waitingForLongEntry && candle.LowPrice <= fastEma && rsi < RsiTakeProfitLong && sar < candle.ClosePrice)
		{
			OpenLong(candle, sar, trendEma, priceStep);
			_waitingForLongEntry = false;
		}
	}

	private void TryEnterShort(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal trendEma, decimal rsi, decimal macdLine,
		decimal macdSma, decimal macdMiddle, decimal sar, decimal priceStep)
	{
		if (Position < 0)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var upperShadow = Math.Abs(candle.HighPrice - candle.ClosePrice);
		var lowerShadow = Math.Abs(candle.OpenPrice - candle.LowPrice);

		var baseCondition = fastEma < slowEma && fastEma < trendEma && rsi < RsiEntryLevel && rsi > RsiTakeProfitShort &&
			macdLine < macdSma && macdLine < macdMiddle && macdSma < macdMiddle && body > 0m && body > lowerShadow && body > upperShadow &&
			sar > candle.ClosePrice;

		if (baseCondition)
		{
			var distanceLimit = DistanceThresholdPoints * priceStep;
			var distance = trendEma - candle.ClosePrice;

			if (distanceLimit > 0m && distance >= distanceLimit)
			{
				_waitingForShortEntry = true;
			}
			else
			{
				OpenShort(candle, sar, trendEma, priceStep);
			}
		}
		else if (_waitingForShortEntry && candle.HighPrice >= fastEma && rsi > RsiTakeProfitShort && sar > candle.ClosePrice)
		{
			OpenShort(candle, sar, trendEma, priceStep);
			_waitingForShortEntry = false;
		}
	}

	private void OpenLong(ICandleMessage candle, decimal sar, decimal trendEma, decimal priceStep)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		if (Position < 0m)
			BuyMarket(Math.Abs(Position));

		BuyMarket(volume);

		_longEntryPrice = candle.ClosePrice;
		_longPartialExecuted = false;

		_longStopPrice = CalculateStopPrice(true, sar, trendEma, StopOffsetPoints * priceStep);
	}

	private void OpenShort(ICandleMessage candle, decimal sar, decimal trendEma, decimal priceStep)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		if (Position > 0m)
			SellMarket(Math.Abs(Position));

		SellMarket(volume);

		_shortEntryPrice = candle.ClosePrice;
		_shortPartialExecuted = false;

		_shortStopPrice = CalculateStopPrice(false, sar, trendEma, StopOffsetPoints * priceStep);
	}

	private decimal? CalculateStopPrice(bool isLong, decimal sar, decimal trendEma, decimal offset)
	{
		decimal? stop = null;

		if (UseSarStopLoss)
			stop = isLong ? sar - offset : sar + offset;

		if (UseTrendStopLoss)
			stop = isLong ? trendEma - offset : trendEma + offset;

		return stop;
	}

	private void ResetLongState()
	{
		_longPartialExecuted = false;
		_longStopPrice = null;
		_longEntryPrice = null;
	}

	private void ResetShortState()
	{
		_shortPartialExecuted = false;
		_shortStopPrice = null;
		_shortEntryPrice = null;
	}
}
