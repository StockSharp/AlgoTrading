using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Forex Fire EMA/MA/RSI Strategy.
/// Multi-timeframe strategy combining EMA, MA and RSI signals.
/// </summary>
public class ForexFireEmaMaRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _rsiSlowLength;
	private readonly StrategyParam<int> _rsiFastLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<bool> _useAtrExits;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _entryCandleType;
	private readonly StrategyParam<DataType> _confCandleType;

	private ExponentialMovingAverage _emaShortEntry;
	private ExponentialMovingAverage _emaLongEntry;
	private IIndicator _maEntry;
	private RelativeStrengthIndex _rsiSlowEntry;
	private RelativeStrengthIndex _rsiFastEntry;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _volumeSma;

	private ExponentialMovingAverage _emaShortConf;
	private ExponentialMovingAverage _emaLongConf;
	private IIndicator _maConf;
	private RelativeStrengthIndex _rsiSlowConf;
	private RelativeStrengthIndex _rsiFastConf;

	private decimal _emaShortConfVal;
	private decimal _emaLongConfVal;
	private decimal _maConfVal;
	private decimal _rsiSlowConfVal;
	private decimal _rsiFastConfVal;

	private decimal _prevEmaShort;
	private decimal _prevEmaLong;
	private decimal _prevPrice;
	private decimal _prevMa;

	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;
	private decimal _trailingStop;

	public ForexFireEmaMaRsiStrategy()
	{
		_emaShortLength = Param(nameof(EmaShortLength), 13)
			.SetDisplay("EMA Short Length", string.Empty, "General")
			.SetCanOptimize(true);

		_emaLongLength = Param(nameof(EmaLongLength), 62)
			.SetDisplay("EMA Long Length", string.Empty, "General")
			.SetCanOptimize(true);

		_maLength = Param(nameof(MaLength), 200)
			.SetDisplay("MA Length", string.Empty, "General")
			.SetCanOptimize(true);

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA Type", string.Empty, "General");

		_rsiSlowLength = Param(nameof(RsiSlowLength), 28)
			.SetDisplay("RSI Slow Length", string.Empty, "RSI")
			.SetCanOptimize(true);

		_rsiFastLength = Param(nameof(RsiFastLength), 7)
			.SetDisplay("RSI Fast Length", string.Empty, "RSI")
			.SetCanOptimize(true);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", string.Empty, "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", string.Empty, "RSI");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", string.Empty, "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss (%)", string.Empty, "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", string.Empty, "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit (%)", string.Empty, "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", string.Empty, "Risk");

		_trailingPercent = Param(nameof(TrailingPercent), 1.5m)
			.SetDisplay("Trailing Stop (%)", string.Empty, "Risk");

		_useAtrExits = Param(nameof(UseAtrExits), true)
			.SetDisplay("Use ATR Exits", string.Empty, "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", string.Empty, "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", string.Empty, "Risk")
			.SetCanOptimize(true);

		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Entry Candle", string.Empty, "Timeframes");

		_confCandleType = Param(nameof(ConfluenceCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Confluence Candle", string.Empty, "Timeframes");
	}

	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int EmaShortLength
	{
		get => _emaShortLength.Value;
		set => _emaShortLength.Value = value;
	}

	/// <summary>
	/// Long EMA length.
	/// </summary>
	public int EmaLongLength
	{
		get => _emaLongLength.Value;
		set => _emaLongLength.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Slow RSI length.
	/// </summary>
	public int RsiSlowLength
	{
		get => _rsiSlowLength.Value;
		set => _rsiSlowLength.Value = value;
	}

	/// <summary>
	/// Fast RSI length.
	/// </summary>
	public int RsiFastLength
	{
		get => _rsiFastLength.Value;
		set => _rsiFastLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Use stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Use take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Use trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop percentage.
	/// </summary>
	public decimal TrailingPercent
	{
		get => _trailingPercent.Value;
		set => _trailingPercent.Value = value;
	}

	/// <summary>
	/// Use ATR exits.
	/// </summary>
	public bool UseAtrExits
	{
		get => _useAtrExits.Value;
		set => _useAtrExits.Value = value;
	}

	/// <summary>
	/// ATR multiplier for exits.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Entry candle type.
	/// </summary>
	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	/// <summary>
	/// Confluence candle type.
	/// </summary>
	public DataType ConfluenceCandleType
	{
		get => _confCandleType.Value;
		set => _confCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, EntryCandleType), (Security, ConfluenceCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_emaShortConfVal = 0m;
		_emaLongConfVal = 0m;
		_maConfVal = 0m;
		_rsiSlowConfVal = 0m;
		_rsiFastConfVal = 0m;

		_prevEmaShort = 0m;
		_prevEmaLong = 0m;
		_prevPrice = 0m;
		_prevMa = 0m;

		_entryPrice = 0m;
		_stopLoss = 0m;
		_takeProfit = 0m;
		_trailingStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaShortEntry = new ExponentialMovingAverage { Length = EmaShortLength };
		_emaLongEntry = new ExponentialMovingAverage { Length = EmaLongLength };
		_maEntry = CreateMa(MaType, MaLength);
		_rsiSlowEntry = new RelativeStrengthIndex { Length = RsiSlowLength };
		_rsiFastEntry = new RelativeStrengthIndex { Length = RsiFastLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_volumeSma = new SimpleMovingAverage { Length = 20 };

		_emaShortConf = new ExponentialMovingAverage { Length = EmaShortLength };
		_emaLongConf = new ExponentialMovingAverage { Length = EmaLongLength };
		_maConf = CreateMa(MaType, MaLength);
		_rsiSlowConf = new RelativeStrengthIndex { Length = RsiSlowLength };
		_rsiFastConf = new RelativeStrengthIndex { Length = RsiFastLength };

		var entrySub = SubscribeCandles(EntryCandleType);
		entrySub
			.Bind(_emaShortEntry, _emaLongEntry, _maEntry, _rsiSlowEntry, _rsiFastEntry, _atr, ProcessEntry)
			.Start();

		var confSub = SubscribeCandles(ConfluenceCandleType);
		confSub
			.Bind(_emaShortConf, _emaLongConf, _maConf, _rsiSlowConf, _rsiFastConf, ProcessConfluence)
			.Start();

		StartProtection();
	}

	private void ProcessConfluence(ICandleMessage candle, decimal emaShort, decimal emaLong, decimal ma, decimal rsiSlow, decimal rsiFast)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_emaShortConfVal = emaShort;
		_emaLongConfVal = emaLong;
		_maConfVal = ma;
		_rsiSlowConfVal = rsiSlow;
		_rsiFastConfVal = rsiFast;
	}

	private void ProcessEntry(ICandleMessage candle, decimal emaShort, decimal emaLong, decimal ma, decimal rsiSlow, decimal rsiFast, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeAvg = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		if (!_volumeSma.IsFormed)
			return;

		var volumeIncreasing = candle.TotalVolume > volumeAvg;

		var longEntryCond = emaShort > emaLong && candle.ClosePrice > ma && rsiFast > rsiSlow && rsiFast > 50m && volumeIncreasing;
		var shortEntryCond = emaShort < emaLong && candle.ClosePrice < ma && rsiFast < rsiSlow && rsiFast < 50m && volumeIncreasing;

		var longConfluence = _emaShortConfVal > _emaLongConfVal && candle.ClosePrice > _maConfVal && _rsiSlowConfVal > 40m && _rsiFastConfVal > _rsiSlowConfVal;
		var shortConfluence = _emaShortConfVal < _emaLongConfVal && candle.ClosePrice < _maConfVal && _rsiSlowConfVal < 60m && _rsiFastConfVal < _rsiSlowConfVal;

		var emaCrossover = _prevEmaShort <= _prevEmaLong && emaShort > emaLong;
		var emaCrossunder = _prevEmaShort >= _prevEmaLong && emaShort < emaLong;
		var priceCrossover = _prevPrice <= _prevMa && candle.ClosePrice > ma;
		var priceCrossunder = _prevPrice >= _prevMa && candle.ClosePrice < ma;

		var longCondition = longEntryCond && longConfluence && (emaCrossover || priceCrossover);
		var shortCondition = shortEntryCond && shortConfluence && (emaCrossunder || priceCrossunder);

		var longExitTechnical = emaShort < emaLong || rsiFast > RsiOverbought;
		var shortExitTechnical = emaShort > emaLong || rsiFast < RsiOversold;

		if (longCondition && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			if (UseStopLoss)
				_stopLoss = _entryPrice * (1 - StopLossPercent / 100m);
			if (UseTakeProfit)
				_takeProfit = _entryPrice * (1 + TakeProfitPercent / 100m);
			if (UseTrailingStop)
				_trailingStop = _entryPrice * (1 - TrailingPercent / 100m);

			BuyMarket();
		}
		else if (shortCondition && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			if (UseStopLoss)
				_stopLoss = _entryPrice * (1 + StopLossPercent / 100m);
			if (UseTakeProfit)
				_takeProfit = _entryPrice * (1 - TakeProfitPercent / 100m);
			if (UseTrailingStop)
				_trailingStop = _entryPrice * (1 + TrailingPercent / 100m);

			SellMarket();
		}

		if (Position > 0)
		{
			if (UseTrailingStop && candle.ClosePrice > _entryPrice)
				_trailingStop = Math.Max(_trailingStop, candle.ClosePrice * (1 - TrailingPercent / 100m));

			var atrStop = candle.ClosePrice - atrValue * AtrMultiplier;

			if ((UseStopLoss && candle.LowPrice <= _stopLoss) ||
				(UseTakeProfit && candle.HighPrice >= _takeProfit) ||
				(UseTrailingStop && candle.LowPrice <= _trailingStop) ||
				(UseAtrExits && candle.LowPrice <= atrStop) ||
				longExitTechnical)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			if (UseTrailingStop && candle.ClosePrice < _entryPrice)
				_trailingStop = Math.Min(_trailingStop, candle.ClosePrice * (1 + TrailingPercent / 100m));

			var atrStop = candle.ClosePrice + atrValue * AtrMultiplier;

			if ((UseStopLoss && candle.HighPrice >= _stopLoss) ||
				(UseTakeProfit && candle.LowPrice <= _takeProfit) ||
				(UseTrailingStop && candle.HighPrice >= _trailingStop) ||
				(UseAtrExits && candle.HighPrice >= atrStop) ||
				shortExitTechnical)
			{
				BuyMarket();
			}
		}

		_prevEmaShort = emaShort;
		_prevEmaLong = emaLong;
		_prevPrice = candle.ClosePrice;
		_prevMa = ma;
	}

	private static IIndicator CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Available moving average types.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Weighted moving average.</summary>
		Weighted,
		/// <summary>Volume weighted moving average.</summary>
		VolumeWeighted
	}
}
