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
/// Risk management strategy converted from the MetaTrader "RiskManager" expert advisor.
/// Manages exposure using CCI, ATR based targets, and portfolio level equity protections.
/// </summary>
public class LayeredRiskProtectorStrategy : Strategy
{
        private sealed class PositionRecord
        {
                public Sides Side;
                public decimal Volume;
                public decimal EntryPrice;
                public decimal StopPrice;
                public decimal TakePrice;
        }

        private readonly StrategyParam<bool> _allowLong;
        private readonly StrategyParam<bool> _allowShort;
        private readonly StrategyParam<decimal> _maxVolume;
        private readonly StrategyParam<int> _layers;
        private readonly StrategyParam<int> _cciLength;
        private readonly StrategyParam<decimal> _cciLevel;
        private readonly StrategyParam<decimal> _stopMultiple;
        private readonly StrategyParam<decimal> _takeMultiple;
        private readonly StrategyParam<decimal> _closeProfitBuffer;
        private readonly StrategyParam<decimal> _manualCapital;
        private readonly StrategyParam<decimal> _riskLimit;
        private readonly StrategyParam<decimal> _profitTarget;
        private readonly StrategyParam<bool> _multiPairTrading;
        private readonly StrategyParam<decimal> _hedgeLevel;
        private readonly StrategyParam<decimal> _hedgeRatio;
        private readonly StrategyParam<bool> _closeAtBreakEven;
        private readonly StrategyParam<bool> _hardClose;
        private readonly StrategyParam<DataType> _candleType;

        private readonly List<PositionRecord> _positions = new();
        private readonly Queue<decimal> _volumeHistory = new();
        private AverageTrueRange _atr = null!;
        private CommodityChannelIndex _cci = null!;

        private decimal _initialCapital;
        private decimal _equityDrawdownLimit;
        private decimal _closeProfitTarget;
        private decimal _riskBaseline;
        private decimal _closeEquityTarget;
        private decimal _breakEvenCapital;
        private decimal _previousClose;
        private decimal _previousVolume;
        private decimal _perOrderVolume;
        private bool _isHedging;
        private bool _stopTrading;
        private bool _breakEvenRecorded;
        private int _hedgeBaseOrders;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayeredRiskProtectorStrategy"/> class.
        /// </summary>
        public LayeredRiskProtectorStrategy()
        {
                _allowLong = Param(nameof(AllowLong), false)
                        .SetDisplay("Allow Long", "Enable long side trading", "Trading");

                _allowShort = Param(nameof(AllowShort), false)
                        .SetDisplay("Allow Short", "Enable short side trading", "Trading");

                _maxVolume = Param(nameof(MaxVolume), 5m)
                        .SetGreaterThanZero()
                        .SetDisplay("Max Volume", "Total volume distributed across layers", "Orders");

                _layers = Param(nameof(Layers), 100)
                        .SetGreaterThanZero()
                        .SetDisplay("Layers", "Maximum order layers", "Orders");

                _cciLength = Param(nameof(CciLength), 75)
                        .SetGreaterThanZero()
                        .SetDisplay("CCI Length", "CCI indicator period", "Indicators");

                _cciLevel = Param(nameof(CciLevel), 100m)
                        .SetGreaterThanZero()
                        .SetDisplay("CCI Level", "CCI threshold for entries", "Indicators");

                _stopMultiple = Param(nameof(StopLossMultiple), 200m)
                        .SetGreaterThanZero()
                        .SetDisplay("Stop Multiple", "ATR multiplier for stop loss", "Risk");

                _takeMultiple = Param(nameof(TakeProfitMultiple), 5m)
                        .SetGreaterThanZero()
                        .SetDisplay("Take Profit Multiple", "ATR multiplier for take profit", "Risk");

                _closeProfitBuffer = Param(nameof(CloseProfitBuffer), 50m)
                        .SetDisplay("Close Equity Buffer", "Equity buffer added when recycling trading", "Risk");

                _manualCapital = Param(nameof(ManualCapital), 0m)
                        .SetDisplay("Manual Capital", "Override initial capital (0 = portfolio value)", "Risk");

                _riskLimit = Param(nameof(RiskLimit), 400m)
                        .SetGreaterThanZero()
                        .SetDisplay("Risk Limit", "Maximum equity drawdown allowed", "Risk");

                _profitTarget = Param(nameof(ProfitTarget), 300m)
                        .SetDisplay("Profit Target", "Equity target that locks profits", "Risk");

                _multiPairTrading = Param(nameof(MultiPairTrading), true)
                        .SetDisplay("Multi Pair Trading", "Disable internal hedging when true", "Hedging");

                _hedgeLevel = Param(nameof(HedgeLevel), 70m)
                        .SetGreaterThanZero()
                        .SetDisplay("Hedge Level", "Health percentage that starts hedging", "Hedging");

                _hedgeRatio = Param(nameof(HedgeRatio), 0.75m)
                        .SetGreaterThanZero()
                        .SetDisplay("Hedge Ratio", "Additional order ratio opened while hedging", "Hedging");

                _closeAtBreakEven = Param(nameof(CloseAtBreakEven), false)
                        .SetDisplay("Close At Break Even", "Close once equity reaches break even", "Risk");

                _hardClose = Param(nameof(HardClose), false)
                        .SetDisplay("Hard Close", "Force positions to close immediately", "Risk");

                _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
                        .SetDisplay("Candle Type", "Primary candle series", "General");
        }

        /// <summary>
        /// Enable long side trading.
        /// </summary>
        public bool AllowLong
        {
                get => _allowLong.Value;
                set => _allowLong.Value = value;
        }

        /// <summary>
        /// Enable short side trading.
        /// </summary>
        public bool AllowShort
        {
                get => _allowShort.Value;
                set => _allowShort.Value = value;
        }

        /// <summary>
        /// Maximum total volume distributed across layers.
        /// </summary>
        public decimal MaxVolume
        {
                get => _maxVolume.Value;
                set => _maxVolume.Value = value;
        }

        /// <summary>
        /// Number of order layers.
        /// </summary>
        public int Layers
        {
                get => _layers.Value;
                set => _layers.Value = value;
        }

        /// <summary>
        /// CCI indicator period length.
        /// </summary>
        public int CciLength
        {
                get => _cciLength.Value;
                set => _cciLength.Value = value;
        }

        /// <summary>
        /// CCI threshold level used for entries.
        /// </summary>
        public decimal CciLevel
        {
                get => _cciLevel.Value;
                set => _cciLevel.Value = value;
        }

        /// <summary>
        /// ATR multiplier for stop loss calculation.
        /// </summary>
        public decimal StopLossMultiple
        {
                get => _stopMultiple.Value;
                set => _stopMultiple.Value = value;
        }

        /// <summary>
        /// ATR multiplier for take profit calculation.
        /// </summary>
        public decimal TakeProfitMultiple
        {
                get => _takeMultiple.Value;
                set => _takeMultiple.Value = value;
        }

        /// <summary>
        /// Equity buffer used when resetting the close equity target.
        /// </summary>
        public decimal CloseProfitBuffer
        {
                get => _closeProfitBuffer.Value;
                set => _closeProfitBuffer.Value = value;
        }

        /// <summary>
        /// Optional manual capital override.
        /// </summary>
        public decimal ManualCapital
        {
                get => _manualCapital.Value;
                set => _manualCapital.Value = value;
        }

        /// <summary>
        /// Maximum equity drawdown allowed before stopping trading.
        /// </summary>
        public decimal RiskLimit
        {
                get => _riskLimit.Value;
                set => _riskLimit.Value = value;
        }

        /// <summary>
        /// Profit target that pauses trading when reached.
        /// </summary>
        public decimal ProfitTarget
        {
                get => _profitTarget.Value;
                set => _profitTarget.Value = value;
        }

        /// <summary>
        /// When true the strategy assumes multi pair trading and skips internal hedging.
        /// </summary>
        public bool MultiPairTrading
        {
                get => _multiPairTrading.Value;
                set => _multiPairTrading.Value = value;
        }

        /// <summary>
        /// Health percentage that enables hedging.
        /// </summary>
        public decimal HedgeLevel
        {
                get => _hedgeLevel.Value;
                set => _hedgeLevel.Value = value;
        }

        /// <summary>
        /// Ratio of additional orders opened during hedging mode.
        /// </summary>
        public decimal HedgeRatio
        {
                get => _hedgeRatio.Value;
                set => _hedgeRatio.Value = value;
        }

        /// <summary>
        /// Enables closing at break even equity.
        /// </summary>
        public bool CloseAtBreakEven
        {
                get => _closeAtBreakEven.Value;
                set => _closeAtBreakEven.Value = value;
        }

        /// <summary>
        /// When true all positions are closed and trading is paused.
        /// </summary>
        public bool HardClose
        {
                get => _hardClose.Value;
                set => _hardClose.Value = value;
        }

        /// <summary>
        /// Candle type used for calculations.
        /// </summary>
        public DataType CandleType
        {
                get => _candleType.Value;
                set => _candleType.Value = value;
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

                _positions.Clear();
                _volumeHistory.Clear();
                _initialCapital = 0m;
                _equityDrawdownLimit = 0m;
                _closeProfitTarget = 0m;
                _riskBaseline = 0m;
                _closeEquityTarget = 0m;
                _breakEvenCapital = 0m;
                _previousClose = 0m;
                _previousVolume = 0m;
                _perOrderVolume = 0m;
                _isHedging = false;
                _stopTrading = false;
                _breakEvenRecorded = false;
                _hedgeBaseOrders = 0;
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
                base.OnStarted(time);

                var portfolio = Portfolio ?? throw new InvalidOperationException("Portfolio must be assigned.");
                var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
                var balance = portfolio.CurrentBalance ?? portfolio.BeginValue ?? equity;

                _initialCapital = ManualCapital > 0m ? ManualCapital : balance;
                _equityDrawdownLimit = _initialCapital - RiskLimit;
                _closeProfitTarget = _initialCapital + ProfitTarget;
                _riskBaseline = RiskLimit;
                _closeEquityTarget = balance + CloseProfitBuffer;
                _breakEvenCapital = 0m;
                _perOrderVolume = Layers > 0 ? MaxVolume / Layers : 0m;

                // Prepare indicators for ATR and CCI calculations.
                _atr = new AverageTrueRange { Length = 14 };
                _cci = new CommodityChannelIndex { Length = CciLength };

                var subscription = SubscribeCandles(CandleType);
                subscription
                        .Bind(_atr, _cci, ProcessCandle)
                        .Start();
        }

        private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal cciValue)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                // Push the previous candle volume into the moving window.
                if (_previousVolume > 0m)
                        EnqueueVolume(_previousVolume);

                var averageVolume = CalculateAverageVolume();
                var isActive = averageVolume > _previousVolume && averageVolume > 0m;

                ApplyPositionTargets(candle);
                ApplyRiskManagement();

                if (_stopTrading)
                {
                        _previousClose = candle.ClosePrice;
                        _previousVolume = candle.TotalVolume;
                        return;
                }

                var indicatorsReady = _atr.IsFormed && _cci.IsFormed;
                var canTrade = IsFormedAndOnlineAndAllowTrading() && _perOrderVolume > 0m && indicatorsReady;
                if (!canTrade)
                {
                        _previousClose = candle.ClosePrice;
                        _previousVolume = candle.TotalVolume;
                        return;
                }

                var ordersTotal = _positions.Count;
                var sizeOn = Layers > 0 ? ordersTotal / (decimal)Layers : 0m;
                var health = CalculateHealth();

                if (!_isHedging && isActive)
                {
                        if (AllowShort && sizeOn * 100m < health && candle.ClosePrice > _previousClose && cciValue > CciLevel)
                                OpenPosition(Sides.Sell, candle.ClosePrice, atrValue);

                        if (AllowLong && sizeOn * 100m < health && candle.ClosePrice < _previousClose && cciValue < -CciLevel)
                                OpenPosition(Sides.Buy, candle.ClosePrice, atrValue);
                }

                if (_isHedging && !_stopTrading)
                        MaintainHedge(candle.ClosePrice, atrValue);

                _previousClose = candle.ClosePrice;
                _previousVolume = candle.TotalVolume;
        }

        private void ApplyPositionTargets(ICandleMessage candle)
        {
                var high = candle.HighPrice;
                var low = candle.LowPrice;

                for (var i = _positions.Count - 1; i >= 0; i--)
                {
                        var record = _positions[i];
                        var shouldClose = false;

                        if (record.Side == Sides.Buy)
                        {
                                if (low <= record.StopPrice)
                                        shouldClose = true;
                                else if (high >= record.TakePrice)
                                        shouldClose = true;
                        }
                        else
                        {
                                if (high >= record.StopPrice)
                                        shouldClose = true;
                                else if (low <= record.TakePrice)
                                        shouldClose = true;
                        }

                        if (!shouldClose)
                                continue;

                        CloseRecord(record);
                        _positions.RemoveAt(i);
                }
        }

        private void ApplyRiskManagement()
        {
                var portfolio = Portfolio;
                if (portfolio == null)
                        return;

                var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
                var balance = portfolio.CurrentBalance ?? portfolio.BeginValue ?? equity;

                if (RiskLimit > 0m && equity <= _equityDrawdownLimit)
                {
                        CloseAllPositions();
                        _stopTrading = true;
                        return;
                }

                if (CloseAtBreakEven && !_breakEvenRecorded)
                {
                        _breakEvenCapital = balance - CloseProfitBuffer;
                        _breakEvenRecorded = true;
                }

                if (CloseAtBreakEven && _breakEvenRecorded && equity >= _breakEvenCapital)
                {
                        CloseAllPositions();
                        _stopTrading = true;
                        return;
                }

                if (ProfitTarget > 0m && equity >= _closeProfitTarget)
                {
                        CloseAllPositions();
                        _stopTrading = true;
                        return;
                }

                if (equity >= _closeEquityTarget)
                {
                        CloseAllPositions();
                        _stopTrading = true;

                        if (_positions.Count == 0)
                        {
                                _closeEquityTarget = equity + CloseProfitBuffer;
                                _isHedging = false;
                                _stopTrading = false;
                        }

                        return;
                }

                if (HardClose)
                {
                        CloseAllPositions();
                        _stopTrading = true;
                        return;
                }

                var health = CalculateHealth();

                if (!MultiPairTrading && !_isHedging && health < HedgeLevel)
                {
                        _isHedging = true;
                        _hedgeBaseOrders = _positions.Count;
                        _closeEquityTarget = _initialCapital;
                }

                if (_isHedging && health > HedgeLevel)
                        _isHedging = false;
        }

        private void MaintainHedge(decimal price, decimal atrValue)
        {
                var targetOrders = (int)Math.Ceiling(_hedgeBaseOrders * (1m + HedgeRatio));
                var currentOrders = _positions.Count;

                if (currentOrders >= targetOrders)
                        return;

                if (AllowLong)
                        OpenPosition(Sides.Sell, price, atrValue);

                if (AllowShort)
                        OpenPosition(Sides.Buy, price, atrValue);
        }

        private void OpenPosition(Sides side, decimal price, decimal atrValue)
        {
                if (_perOrderVolume <= 0m)
                        return;

                if (atrValue <= 0m)
                        return;

                var stopDistance = atrValue * StopLossMultiple;
                var takeDistance = atrValue * TakeProfitMultiple;

                var record = new PositionRecord
                {
                        Side = side,
                        Volume = _perOrderVolume,
                        EntryPrice = price,
                        StopPrice = side == Sides.Buy ? price - stopDistance : price + stopDistance,
                        TakePrice = side == Sides.Buy ? price + takeDistance : price - takeDistance,
                };

                _positions.Add(record);

                if (side == Sides.Buy)
                        BuyMarket(record.Volume);
                else
                        SellMarket(record.Volume);
        }

        private void CloseRecord(PositionRecord record)
        {
                if (record.Side == Sides.Buy)
                        SellMarket(record.Volume);
                else
                        BuyMarket(record.Volume);
        }

        private void CloseAllPositions()
        {
                decimal longVolume = 0m;
                decimal shortVolume = 0m;

                for (var i = 0; i < _positions.Count; i++)
                {
                        var position = _positions[i];
                        if (position.Side == Sides.Buy)
                                longVolume += position.Volume;
                        else
                                shortVolume += position.Volume;
                }

                if (longVolume > 0m)
                        SellMarket(longVolume);

                if (shortVolume > 0m)
                        BuyMarket(shortVolume);

                _positions.Clear();
        }

        private void EnqueueVolume(decimal volume)
        {
                const int maxCount = 50;
                _volumeHistory.Enqueue(volume);
                while (_volumeHistory.Count > maxCount)
                        _volumeHistory.Dequeue();
        }

        private decimal CalculateAverageVolume()
        {
                if (_volumeHistory.Count == 0)
                        return 0m;

                decimal sum = 0m;
                foreach (var volume in _volumeHistory)
                        sum += volume;

                return sum / _volumeHistory.Count;
        }

        private decimal CalculateHealth()
        {
                var portfolio = Portfolio;
                if (portfolio == null)
                        return 0m;

                var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
                var currentRisk = equity - _equityDrawdownLimit;

                if (_riskBaseline <= 0m)
                        return 0m;

                return currentRisk / _riskBaseline * 100m;
        }
}

