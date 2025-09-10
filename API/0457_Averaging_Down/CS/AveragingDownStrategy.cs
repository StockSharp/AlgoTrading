using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Averaging Down strategy with ATR-based bands and step-scaled DCA entries.
/// </summary>
public class AveragingDownStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<decimal> _baseDeviation;
	private readonly StrategyParam<decimal> _stepScale;
	private readonly StrategyParam<decimal> _dcaMultiplier;
	private readonly StrategyParam<int> _maxDcaLevels;
	private readonly StrategyParam<decimal> _initialVolume;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;

	private decimal _entryPrice;
	private decimal _avgPrice;
	private decimal _positionSize;
	private int _dcaCount;
	private int _positionDir; // 1 for long, -1 for short

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR band multiplier.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TpPercent { get => _tpPercent.Value; set => _tpPercent.Value = value; }

	/// <summary>
	/// Base deviation percent for first DCA.
	/// </summary>
	public decimal BaseDeviation { get => _baseDeviation.Value; set => _baseDeviation.Value = value; }

	/// <summary>
	/// Multiplier for deviation at each DCA level.
	/// </summary>
	public decimal StepScale { get => _stepScale.Value; set => _stepScale.Value = value; }

	/// <summary>
	/// Volume multiplier for each DCA.
	/// </summary>
	public decimal DcaMultiplier { get => _dcaMultiplier.Value; set => _dcaMultiplier.Value = value; }

	/// <summary>
	/// Maximum number of DCA levels.
	/// </summary>
	public int MaxDcaLevels { get => _maxDcaLevels.Value; set => _maxDcaLevels.Value = value; }

	/// <summary>
	/// Initial order volume.
	/// </summary>
	public decimal InitialVolume { get => _initialVolume.Value; set => _initialVolume.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public AveragingDownStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_atrLength = Param(nameof(AtrLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 10);

		_atrMultiplier = Param(nameof(AtrMultiplier), 8m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Mult", "ATR band multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4m, 10m, 2m);

		_tpPercent = Param(nameof(TpPercent), 3m)
			.SetDisplay("TP %", "Take profit percent", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(2m, 5m, 1m);

		_baseDeviation = Param(nameof(BaseDeviation), 3m)
			.SetDisplay("Base Deviation %", "Percent move for first DCA", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(2m, 5m, 1m);

		_stepScale = Param(nameof(StepScale), 1.5m)
			.SetDisplay("Step Scale", "Multiplier for next deviation", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1.2m, 2m, 0.2m);

		_dcaMultiplier = Param(nameof(DcaMultiplier), 1.2m)
			.SetDisplay("DCA Size Multiplier", "Size multiplier for each DCA", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1m, 2m, 0.2m);

		_maxDcaLevels = Param(nameof(MaxDcaLevels), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max DCA Levels", "Maximum number of averaging entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_initialVolume = Param(nameof(InitialVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Initial order volume", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);
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

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_atr.IsFormed)
			return;

		var price = candle.ClosePrice;
		var upperBand = emaValue + atrValue * AtrMultiplier;
		var lowerBand = emaValue - atrValue * AtrMultiplier;

		if (_positionDir == 0)
		{
			if (price > upperBand)
			{
				_positionDir = 1;
				_entryPrice = price;
				_avgPrice = price;
				_dcaCount = 0;
				_positionSize = InitialVolume;
				BuyMarket(_positionSize);
			}
			else if (price < lowerBand)
			{
				_positionDir = -1;
				_entryPrice = price;
				_avgPrice = price;
				_dcaCount = 0;
				_positionSize = InitialVolume;
				SellMarket(_positionSize);
			}
			return;
		}

		var tpPrice = _positionDir == 1
			? _avgPrice * (1 + TpPercent / 100m)
			: _avgPrice * (1 - TpPercent / 100m);

		if ((_positionDir == 1 && price >= tpPrice) ||
			(_positionDir == -1 && price <= tpPrice))
		{
			if (_positionDir == 1)
				SellMarket(Math.Abs(Position));
			else
				BuyMarket(Math.Abs(Position));

			ResetState();
			return;
		}

		if (_dcaCount >= MaxDcaLevels)
			return;

		var deviation = BaseDeviation;
		for (var i = 0; i < _dcaCount; i++)
			deviation *= StepScale;

		var dcaPrice = _positionDir == 1
			? _entryPrice * (1 - deviation / 100m)
			: _entryPrice * (1 + deviation / 100m);

		if ((_positionDir == 1 && price <= dcaPrice) ||
			(_positionDir == -1 && price >= dcaPrice))
		{
			var vol = InitialVolume * (decimal)Math.Pow((double)DcaMultiplier, _dcaCount + 1);

			if (_positionDir == 1)
				BuyMarket(vol);
			else
				SellMarket(vol);

			_avgPrice = ((_avgPrice * _positionSize) + price * vol) / (_positionSize + vol);
			_positionSize += vol;
			_dcaCount++;
		}
	}

	private void ResetState()
	{
		_positionDir = 0;
		_entryPrice = 0m;
		_avgPrice = 0m;
		_positionSize = 0m;
		_dcaCount = 0;
	}
}
