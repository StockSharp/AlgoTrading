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
/// MACD pattern strategy inspired by the MetaTrader advisor "MacdPatternTraderv03".
/// </summary>
public class MacdPatternTraderV03Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _upperActivation;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<decimal> _lowerActivation;
	private readonly StrategyParam<int> _emaOneLength;
	private readonly StrategyParam<int> _emaTwoLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _emaFourLength;
	private readonly StrategyParam<decimal> _profitThreshold;

	private decimal? _previousMacd;
	private decimal? _olderMacd;

	private bool _isAboveUpperActivation;
	private bool _firstUpperDropConfirmed;
	private bool _secondUpperDropConfirmed;
	private bool _sellReady;
	private decimal _firstUpperPeak;
	private decimal _secondUpperPeak;

	private bool _isBelowLowerActivation;
	private bool _firstLowerRiseConfirmed;
	private bool _secondLowerRiseConfirmed;
	private bool _buyReady;
	private decimal _firstLowerTrough;
	private decimal _secondLowerTrough;

	private decimal? _emaTwoValue;
	private decimal? _smaValue;
	private decimal? _emaFourValue;

	private ICandleMessage _previousCandle;

	private int _longScaleStage;
	private int _shortScaleStage;
	private decimal _initialLongPosition;
	private decimal _initialShortPosition;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MacdPatternTraderV03Strategy()
	{

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 5)
		.SetDisplay("Fast EMA", "Fast period used inside MACD", "MACD");

		_slowEmaLength = Param(nameof(SlowEmaLength), 13)
		.SetDisplay("Slow EMA", "Slow period used inside MACD", "MACD");

		_upperThreshold = Param(nameof(UpperThreshold), 0.0045m)
		.SetDisplay("Upper Threshold", "Level that confirms bearish exhaustion", "MACD");

		_upperActivation = Param(nameof(UpperActivation), 0.0030m)
		.SetDisplay("Upper Activation", "Level that arms the bearish pattern", "MACD");

		_lowerThreshold = Param(nameof(LowerThreshold), -0.0045m)
		.SetDisplay("Lower Threshold", "Level that confirms bullish exhaustion", "MACD");

		_lowerActivation = Param(nameof(LowerActivation), -0.0030m)
		.SetDisplay("Lower Activation", "Level that arms the bullish pattern", "MACD");

		_emaOneLength = Param(nameof(EmaOneLength), 7)
		.SetDisplay("EMA #1", "Short EMA used for scaling out", "Management");

		_emaTwoLength = Param(nameof(EmaTwoLength), 21)
		.SetDisplay("EMA #2", "Second EMA used for scaling out", "Management");

		_smaLength = Param(nameof(SmaLength), 98)
		.SetDisplay("SMA", "Simple moving average used for scaling out", "Management");

		_emaFourLength = Param(nameof(EmaFourLength), 365)
		.SetDisplay("EMA #4", "Slow EMA used for scaling out", "Management");

		_profitThreshold = Param(nameof(ProfitThreshold), 5m)
		.SetDisplay("Profit Threshold", "Unrealized PnL required before scaling out", "Management");
	}


	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length inside MACD.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length inside MACD.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Upper threshold that marks MACD exhaustion for shorts.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Upper activation level that arms the short pattern.
	/// </summary>
	public decimal UpperActivation
	{
		get => _upperActivation.Value;
		set => _upperActivation.Value = value;
	}

	/// <summary>
	/// Lower threshold that marks MACD exhaustion for longs.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// Lower activation level that arms the long pattern.
	/// </summary>
	public decimal LowerActivation
	{
		get => _lowerActivation.Value;
		set => _lowerActivation.Value = value;
	}

	/// <summary>
	/// Short EMA used for position management.
	/// </summary>
	public int EmaOneLength
	{
		get => _emaOneLength.Value;
		set => _emaOneLength.Value = value;
	}

	/// <summary>
	/// Second EMA used for position management.
	/// </summary>
	public int EmaTwoLength
	{
		get => _emaTwoLength.Value;
		set => _emaTwoLength.Value = value;
	}

	/// <summary>
	/// SMA length used for position management.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA used for position management.
	/// </summary>
	public int EmaFourLength
	{
		get => _emaFourLength.Value;
		set => _emaFourLength.Value = value;
	}

	/// <summary>
	/// Minimum unrealized PnL before scaling out (in price units * volume).
	/// </summary>
	public decimal ProfitThreshold
	{
		get => _profitThreshold.Value;
		set => _profitThreshold.Value = value;
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

		var macd = new MovingAverageConvergenceDivergence
		{
			Fast = FastEmaLength,
			Slow = SlowEmaLength,
			Signal = 1
		};

		var emaOne = new ExponentialMovingAverage { Length = EmaOneLength };
		var emaTwo = new ExponentialMovingAverage { Length = EmaTwoLength };
		var sma = new SimpleMovingAverage { Length = SmaLength };
		var emaFour = new ExponentialMovingAverage { Length = EmaFourLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(emaOne, emaTwo, sma, emaFour, UpdateCachedIndicators)
		.BindEx(macd, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, emaOne);
			DrawIndicator(area, emaTwo);
			DrawIndicator(area, sma);
			DrawIndicator(area, emaFour);
			DrawOwnTrades(area);
		}
	}

	private void UpdateCachedIndicators(ICandleMessage candle, decimal emaOne, decimal emaTwo, decimal sma, decimal emaFour)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_emaTwoValue = emaTwo;
		_smaValue = sma;
		_emaFourValue = emaFour;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			CacheMacd(((MovingAverageConvergenceDivergenceValue)macdValue).Macd);
			_previousCandle = candle;
			return;
		}

		var macdData = (MovingAverageConvergenceDivergenceValue)macdValue;
		var macdMain = macdData.Macd;

		if (_previousMacd is null || _olderMacd is null)
		{
			CacheMacd(macdMain);
			_previousCandle = candle;
			return;
		}

		var macdPrev = _previousMacd.Value;
		var macdPrev2 = _olderMacd.Value;

		EvaluateSellPattern(macdMain, macdPrev, macdPrev2);
		EvaluateBuyPattern(macdMain, macdPrev, macdPrev2);
		ManageOpenPosition(candle);

		CacheMacd(macdMain);
		_previousCandle = candle;
	}

	private void EvaluateSellPattern(decimal macdCurrent, decimal macdPrevious, decimal macdPrevious2)
	{
		if (macdCurrent > UpperActivation)
		_isAboveUpperActivation = true;

		if (_isAboveUpperActivation && macdCurrent < macdPrevious && macdPrevious > macdPrevious2 && macdPrevious > _firstUpperPeak && !_firstUpperDropConfirmed)
		_firstUpperPeak = macdPrevious;

		if (_firstUpperPeak > 0m && macdCurrent < UpperThreshold)
		_firstUpperDropConfirmed = true;

		if (macdCurrent < UpperActivation)
		{
			ResetSellPattern();
			return;
		}

		if (_firstUpperDropConfirmed && macdCurrent > UpperThreshold && macdCurrent < macdPrevious && macdPrevious > macdPrevious2 && macdPrevious > _firstUpperPeak && macdPrevious > _secondUpperPeak && !_secondUpperDropConfirmed)
		_secondUpperPeak = macdPrevious;

		if (_secondUpperPeak > 0m && macdCurrent < UpperThreshold)
		_secondUpperDropConfirmed = true;

		if (_secondUpperDropConfirmed && macdCurrent < UpperThreshold && macdPrevious < UpperThreshold && macdPrevious2 < UpperThreshold && macdCurrent < macdPrevious && macdPrevious > macdPrevious2 && macdPrevious < _secondUpperPeak)
		_sellReady = true;

		if (!_sellReady)
		return;

		EnterShort();
	}

	private void EvaluateBuyPattern(decimal macdCurrent, decimal macdPrevious, decimal macdPrevious2)
	{
		if (macdCurrent < LowerActivation)
		_isBelowLowerActivation = true;

		if (_isBelowLowerActivation && macdCurrent > macdPrevious && macdPrevious < macdPrevious2 && macdPrevious < _firstLowerTrough && !_firstLowerRiseConfirmed)
		_firstLowerTrough = macdPrevious;

		if (_firstLowerTrough < 0m && macdCurrent > LowerThreshold)
		_firstLowerRiseConfirmed = true;

		if (macdCurrent > LowerActivation)
		{
			ResetBuyPattern();
			return;
		}

		if (_firstLowerRiseConfirmed && macdCurrent < LowerThreshold && macdCurrent > macdPrevious && macdPrevious < macdPrevious2 && macdPrevious < _firstLowerTrough && macdPrevious < _secondLowerTrough && !_secondLowerRiseConfirmed)
		_secondLowerTrough = macdPrevious;

		if (_secondLowerTrough < 0m && macdCurrent > LowerThreshold)
		_secondLowerRiseConfirmed = true;

		if (_secondLowerRiseConfirmed && macdCurrent > LowerThreshold && macdPrevious > LowerThreshold && macdPrevious2 > LowerThreshold && macdCurrent > macdPrevious && macdPrevious < macdPrevious2 && macdPrevious > _secondLowerTrough)
		_buyReady = true;

		if (!_buyReady)
		return;

		EnterLong();
	}

	private void EnterShort()
	{
		var currentPosition = Position;
		var flattenVolume = currentPosition > 0m ? currentPosition : 0m;
		if (flattenVolume > 0m)
		SellMarket(flattenVolume);

		var entryVolume = Volume + Math.Max(0m, Position);
		if (entryVolume <= 0m)
		{
			ResetSellPattern();
			_sellReady = false;
			return;
		}

		SellMarket(entryVolume);
		_initialShortPosition = Math.Abs(Position);
		_shortScaleStage = 0;
		_longScaleStage = 0;
		_sellReady = false;
		ResetSellPattern();
		ResetBuyPattern();
	}

	private void EnterLong()
	{
		var currentPosition = Position;
		var flattenVolume = currentPosition < 0m ? -currentPosition : 0m;
		if (flattenVolume > 0m)
		BuyMarket(flattenVolume);

		var entryVolume = Volume + Math.Max(0m, -Position);
		if (entryVolume <= 0m)
		{
			ResetBuyPattern();
			_buyReady = false;
			return;
		}

		BuyMarket(entryVolume);
		_initialLongPosition = Math.Max(0m, Position);
		_longScaleStage = 0;
		_shortScaleStage = 0;
		_buyReady = false;
		ResetBuyPattern();
		ResetSellPattern();
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			_longScaleStage = 0;
			_shortScaleStage = 0;
			_initialLongPosition = 0m;
			_initialShortPosition = 0m;
			return;
		}

		var previousCandle = _previousCandle;
		if (previousCandle is null)
		return;

		var profitThreshold = ProfitThreshold;
		if (profitThreshold <= 0m)
		return;

		var unrealized = GetUnrealizedPnL(candle);
		if (unrealized < profitThreshold)
		return;

		if (Position > 0m)
		{
			if (_emaTwoValue is decimal emaTwo && previousCandle.ClosePrice > emaTwo && _longScaleStage == 0)
			{
				var volume = Math.Min(Position, _initialLongPosition / 3m);
				if (volume > 0m)
				{
					SellMarket(volume);
					_longScaleStage = 1;
				}
			}

			if (_smaValue is decimal sma && _emaFourValue is decimal emaFour && previousCandle.HighPrice > (sma + emaFour) / 2m && _longScaleStage == 1)
			{
				var volume = Math.Min(Position, _initialLongPosition / 2m);
				if (volume > 0m)
				{
					SellMarket(volume);
					_longScaleStage = 2;
				}
			}
		}
		else if (Position < 0m)
		{
			var shortPosition = -Position;
			if (_emaTwoValue is decimal emaTwo && previousCandle.ClosePrice < emaTwo && _shortScaleStage == 0)
			{
				var volume = Math.Min(shortPosition, _initialShortPosition / 3m);
				if (volume > 0m)
				{
					BuyMarket(volume);
					_shortScaleStage = 1;
				}
			}

			if (_smaValue is decimal sma && _emaFourValue is decimal emaFour && previousCandle.LowPrice < (sma + emaFour) / 2m && _shortScaleStage == 1)
			{
				var volume = Math.Min(shortPosition, _initialShortPosition / 2m);
				if (volume > 0m)
				{
					BuyMarket(volume);
					_shortScaleStage = 2;
				}
			}
		}
	}

	private void CacheMacd(decimal macdValue)
	{
		_olderMacd = _previousMacd;
		_previousMacd = macdValue;
	}

	private decimal GetUnrealizedPnL(ICandleMessage candle)
	{
		if (Position == 0m)
		return 0m;

		var entryPrice = PositionAvgPrice;
		if (entryPrice == 0m)
		return 0m;

		var diff = candle.ClosePrice - entryPrice;
		return diff * Position;
	}

	private void ResetSellPattern()
	{
		_isAboveUpperActivation = false;
		_firstUpperDropConfirmed = false;
		_secondUpperDropConfirmed = false;
		_sellReady = false;
		_firstUpperPeak = 0m;
		_secondUpperPeak = 0m;
	}

	private void ResetBuyPattern()
	{
		_isBelowLowerActivation = false;
		_firstLowerRiseConfirmed = false;
		_secondLowerRiseConfirmed = false;
		_buyReady = false;
		_firstLowerTrough = 0m;
		_secondLowerTrough = 0m;
	}
}

