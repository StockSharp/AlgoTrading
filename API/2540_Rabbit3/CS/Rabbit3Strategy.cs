namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class Rabbit3Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciBuyLevel;
	private readonly StrategyParam<decimal> _cciSellLevel;
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _williamsOversold;
	private readonly StrategyParam<decimal> _williamsOverbought;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;

	private WilliamsR _williams;
	private CommodityChannelIndex _cci;
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	
	// Store last Williams %R value to require two-bar confirmation.
	private decimal _previousWilliams;
	private bool _hasPrevWilliams;
	// Track whether the boosted volume should be used for the next order.
	private bool _useBoost;
	// Remember realized PnL to measure the delta after each closed trade.
	private decimal _lastRealizedPnL;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public decimal CciBuyLevel
	{
		get => _cciBuyLevel.Value;
		set => _cciBuyLevel.Value = value;
	}

	public decimal CciSellLevel
	{
		get => _cciSellLevel.Value;
		set => _cciSellLevel.Value = value;
	}

	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	public decimal WilliamsOversold
	{
		get => _williamsOversold.Value;
		set => _williamsOversold.Value = value;
	}

	public decimal WilliamsOverbought
	{
		get => _williamsOverbought.Value;
		set => _williamsOverbought.Value = value;
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

	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	public decimal ProfitThreshold
	{
		get => _profitThreshold.Value;
		set => _profitThreshold.Value = value;
	}

	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public Rabbit3Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signals", "General");

		_cciPeriod = Param(nameof(CciPeriod), 15)
			.SetDisplay("CCI Period", "Commodity Channel Index length", "Indicators")
			.SetGreaterThanZero();

		_cciBuyLevel = Param(nameof(CciBuyLevel), -80m)
			.SetDisplay("CCI Buy Level", "CCI threshold to allow long entries", "Signals");

		_cciSellLevel = Param(nameof(CciSellLevel), 80m)
			.SetDisplay("CCI Sell Level", "CCI threshold to allow short entries", "Signals");

		_williamsPeriod = Param(nameof(WilliamsPeriod), 62)
			.SetDisplay("Williams %R Period", "Williams %R lookback", "Indicators")
			.SetGreaterThanZero();

		_williamsOversold = Param(nameof(WilliamsOversold), -80m)
			.SetDisplay("Williams Oversold", "Oversold threshold for confirmation", "Signals");

		_williamsOverbought = Param(nameof(WilliamsOverbought), -20m)
			.SetDisplay("Williams Overbought", "Overbought threshold for confirmation", "Signals");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 17)
			.SetDisplay("Fast EMA Period", "Fast EMA plotted for context", "Indicators")
			.SetGreaterThanZero();

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 30)
			.SetDisplay("Slow EMA Period", "Slow EMA plotted for context", "Indicators")
			.SetGreaterThanZero();

		_maxPositions = Param(nameof(MaxPositions), 2)
			.SetDisplay("Max Positions", "Maximum stacked entries per direction", "Risk")
			.SetGreaterThanZero();

		_profitThreshold = Param(nameof(ProfitThreshold), 4m)
			.SetDisplay("Profit Threshold", "Realized profit that boosts volume", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
			.SetDisplay("Base Volume", "Initial trade volume", "Risk")
			.SetGreaterThanZero();

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.6m)
			.SetDisplay("Volume Multiplier", "Factor applied after profitable trades", "Risk")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 45)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in adjusted points", "Risk")
			.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 110)
			.SetDisplay("Take Profit (pips)", "Target distance in adjusted points", "Risk")
			.SetGreaterThanZero();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousWilliams = 0m;
		_hasPrevWilliams = false;
		_useBoost = false;
		_lastRealizedPnL = 0m;
	}

        protected override void OnStarted(DateTimeOffset time)
        {
                base.OnStarted(time);

                // Reset dynamic sizing state and expose the starting volume to UI.
                _useBoost = false;
                Volume = BaseVolume;
                _lastRealizedPnL = PnL;

                // Initialize indicators that will be bound to the candle feed.
                _williams = new WilliamsR { Length = WilliamsPeriod };
                _cci = new CommodityChannelIndex { Length = CciPeriod };
                _fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
                _slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

                // Subscribe to candles and bind indicators plus the processing callback.
                var subscription = SubscribeCandles(CandleType);
                subscription.Bind(_williams, _cci, _fastEma, _slowEma, ProcessCandle).Start();

                var area = CreateChartArea();
                if (area != null)
                {
                        DrawCandles(area, subscription);
                        DrawIndicator(area, _williams);
                        DrawIndicator(area, _cci);
                        DrawIndicator(area, _fastEma);
                        DrawIndicator(area, _slowEma);
                        DrawOwnTrades(area);
                }

                var point = GetAdjustedPoint();
                var takeDistance = TakeProfitPips * point;
                var stopDistance = StopLossPips * point;

                // Register protective orders using MetaTrader-like pip distances.
                StartProtection(
                        takeProfit: new Unit(takeDistance, UnitTypes.Point),
                        stopLoss: new Unit(stopDistance, UnitTypes.Point));
        }

        private void ProcessCandle(ICandleMessage candle, decimal williamsValue, decimal cciValue, decimal fastEmaValue, decimal slowEmaValue)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                // Update the current volume based on realized profit or loss.
                UpdateVolumeIfNeeded();

                if (!_williams.IsFormed || !_cci.IsFormed)
                {
                        _previousWilliams = williamsValue;
                        return;
                }

                if (!_hasPrevWilliams)
                {
                        _previousWilliams = williamsValue;
                        _hasPrevWilliams = true;
                        return;
                }

                if (!IsFormedAndOnlineAndAllowTrading())
                        return;

                if (williamsValue == 0m)
                        williamsValue = -1m;

                if (_previousWilliams == 0m)
                        _previousWilliams = -1m;

                // Require Williams %R confirmation on two consecutive closed candles and CCI agreement.
                var longSignal = williamsValue < WilliamsOversold
                        && _previousWilliams < WilliamsOversold
                        && cciValue < CciBuyLevel
                        && CanEnterLong();

                var shortSignal = williamsValue > WilliamsOverbought
                        && _previousWilliams > WilliamsOverbought
                        && cciValue > CciSellLevel
                        && CanEnterShort();

                if (longSignal)
                {
                        // Stack another long position using the dynamically selected volume.
                        var volume = GetTradeVolume();
                        BuyMarket(volume);
                        LogInfo($"Enter long at {candle.ClosePrice:F4} with volume {volume}");
                }
                else if (shortSignal)
                {
                        // Stack another short position using the dynamically selected volume.
                        var volume = GetTradeVolume();
                        SellMarket(volume);
                        LogInfo($"Enter short at {candle.ClosePrice:F4} with volume {volume}");
                }

                _previousWilliams = williamsValue;
        }

        private void UpdateVolumeIfNeeded()
        {
                var realizedPnL = PnL;

                if (realizedPnL != _lastRealizedPnL)
                {
                        var delta = realizedPnL - _lastRealizedPnL;
                        // Boost the next order after reaching the desired profit, otherwise revert to base volume.
                        _useBoost = delta > ProfitThreshold;
                        _lastRealizedPnL = realizedPnL;
                }

                Volume = GetTradeVolume();
        }

        private bool CanEnterLong()
        {
                if (Position < 0m)
                        return false;

                // Limit the number of stacked long entries according to MaxPositions.
                var tradeVolume = GetTradeVolume();
                var targetVolume = Position + tradeVolume;
                var maxVolume = MaxPositions * tradeVolume;
                return targetVolume <= maxVolume + GetVolumeTolerance();
        }

        private bool CanEnterShort()
        {
                if (Position > 0m)
                        return false;

                // Limit stacked shorts in the same way as longs.
                var tradeVolume = GetTradeVolume();
                var targetVolume = Math.Abs(Position - tradeVolume);
                var maxVolume = MaxPositions * tradeVolume;
                return targetVolume <= maxVolume + GetVolumeTolerance();
        }

	private decimal GetTradeVolume()
	{
		var multiplier = _useBoost ? VolumeMultiplier : 1m;
		return BaseVolume * multiplier;
	}

	private decimal GetAdjustedPoint()
	{
		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;
		return step * adjust;
	}

	private decimal GetVolumeTolerance()
	{
		var step = Security?.VolumeStep;
		if (step == null || step == 0m)
			return 0.00000001m;
		return step.Value / 2m;
	}
}
