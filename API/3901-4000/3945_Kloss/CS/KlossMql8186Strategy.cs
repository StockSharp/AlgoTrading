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
/// Strategy converted from the MetaTrader 4 "Kloss" expert advisor (MQL/8186).
/// Implements the original combination of CCI and Stochastic oscillators with a price shift filter.
/// </summary>
public class KlossMql8186Strategy : Strategy
{
        private readonly StrategyParam<int> _cciPeriod;
        private readonly StrategyParam<decimal> _cciThreshold;
        private readonly StrategyParam<int> _stochasticKPeriod;
        private readonly StrategyParam<int> _stochasticDPeriod;
        private readonly StrategyParam<int> _stochasticSmooth;
        private readonly StrategyParam<decimal> _stochasticOversold;
        private readonly StrategyParam<decimal> _stochasticOverbought;
        private readonly StrategyParam<decimal> _stopLossPoints;
        private readonly StrategyParam<decimal> _takeProfitPoints;
        private readonly StrategyParam<decimal> _fixedVolume;
        private readonly StrategyParam<decimal> _riskPercent;
        private readonly StrategyParam<decimal> _maxVolume;
        private readonly StrategyParam<DataType> _candleType;

        private CommodityChannelIndex _cci = null!;
        private StochasticOscillator _stochastic = null!;

        private decimal? _previousOpen;
        private decimal? _previousClose;
        private readonly decimal?[] _typicalHistory = new decimal?[5];

        /// <summary>
        /// Initializes a new instance of the <see cref="KlossMql8186Strategy"/> class.
        /// </summary>
        public KlossMql8186Strategy()
        {
                _cciPeriod = Param(nameof(CciPeriod), 10)
                        .SetGreaterThanZero()
                        .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
                        
                        .SetOptimize(5, 40, 5);

                _cciThreshold = Param(nameof(CciThreshold), 150m)
                        .SetGreaterThanZero()
                        .SetDisplay("CCI Threshold", "Absolute CCI level that triggers entries", "Indicators")
                        
                        .SetOptimize(80m, 200m, 10m);

                _stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
                        .SetGreaterThanZero()
                        .SetDisplay("Stochastic %K", "Period of the %K line", "Indicators")
                        
                        .SetOptimize(3, 15, 1);

                _stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
                        .SetGreaterThanZero()
                        .SetDisplay("Stochastic %D", "SMA length of the %D line", "Indicators")

                        .SetOptimize(1, 10, 1);

                _stochasticSmooth = Param(nameof(StochasticSmooth), 3)
                        .SetGreaterThanZero()
                        .SetDisplay("Stochastic Smoothing", "Smoothing applied to the %K calculation", "Indicators")
                        
                        .SetOptimize(1, 10, 1);

                _stochasticOversold = Param(nameof(StochasticOversold), 45m)
                        .SetNotNegative()
                        .SetDisplay("Stochastic Oversold", "Threshold under which %K confirms a long signal", "Signals")
                        
                        .SetOptimize(10m, 40m, 5m);

                _stochasticOverbought = Param(nameof(StochasticOverbought), 55m)
                        .SetNotNegative()
                        .SetDisplay("Stochastic Overbought", "Threshold above which %K confirms a short signal", "Signals")
                        
                        .SetOptimize(60m, 90m, 5m);

                _stopLossPoints = Param(nameof(StopLossPoints), 48m)
                        .SetNotNegative()
                        .SetDisplay("Stop Loss (pts)", "Stop loss distance expressed in price points", "Risk");

                _takeProfitPoints = Param(nameof(TakeProfitPoints), 152m)
                        .SetNotNegative()
                        .SetDisplay("Take Profit (pts)", "Take profit distance expressed in price points", "Risk");

                _fixedVolume = Param(nameof(FixedVolume), 0m)
                        .SetNotNegative()
                        .SetDisplay("Fixed Volume", "If greater than zero this volume will be used for orders", "Trading");

                _riskPercent = Param(nameof(RiskPercent), 0.2m)
                        .SetNotNegative()
                        .SetDisplay("Risk Percent", "Fraction of portfolio value used when Fixed Volume is zero", "Trading");

                _maxVolume = Param(nameof(MaxVolume), 5m)
                        .SetGreaterThanZero()
                        .SetDisplay("Max Volume", "Upper limit for the trade volume", "Trading");

                _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                        .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General");
        }

        /// <summary>
        /// CCI lookback period.
        /// </summary>
        public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

        /// <summary>
        /// Absolute CCI level that triggers entries.
        /// </summary>
        public decimal CciThreshold { get => _cciThreshold.Value; set => _cciThreshold.Value = value; }

        /// <summary>
        /// Stochastic %K period.
        /// </summary>
        public int StochasticKPeriod { get => _stochasticKPeriod.Value; set => _stochasticKPeriod.Value = value; }

        /// <summary>
        /// Stochastic %D period.
        /// </summary>
        public int StochasticDPeriod { get => _stochasticDPeriod.Value; set => _stochasticDPeriod.Value = value; }

        /// <summary>
        /// Stochastic smoothing factor.
        /// </summary>
        public int StochasticSmooth { get => _stochasticSmooth.Value; set => _stochasticSmooth.Value = value; }

        /// <summary>
        /// Stochastic oversold threshold.
        /// </summary>
        public decimal StochasticOversold { get => _stochasticOversold.Value; set => _stochasticOversold.Value = value; }

        /// <summary>
        /// Stochastic overbought threshold.
        /// </summary>
        public decimal StochasticOverbought { get => _stochasticOverbought.Value; set => _stochasticOverbought.Value = value; }

        /// <summary>
        /// Stop loss distance in points.
        /// </summary>
        public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

        /// <summary>
        /// Take profit distance in points.
        /// </summary>
        public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

        /// <summary>
        /// Fixed trading volume.
        /// </summary>
        public decimal FixedVolume { get => _fixedVolume.Value; set => _fixedVolume.Value = value; }

        /// <summary>
        /// Fraction of the portfolio value converted into volume when <see cref="FixedVolume"/> is zero.
        /// </summary>
        public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

        /// <summary>
        /// Maximum volume allowed per trade.
        /// </summary>
        public decimal MaxVolume { get => _maxVolume.Value; set => _maxVolume.Value = value; }

        /// <summary>
        /// Candle type used for indicator calculations.
        /// </summary>
        public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

        /// <inheritdoc />
        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
                return [(Security, CandleType)];
        }

        /// <inheritdoc />
        protected override void OnReseted()
        {
                base.OnReseted();

                _previousOpen = null;
                _previousClose = null;
                Array.Clear(_typicalHistory, 0, _typicalHistory.Length);
        }

        /// <inheritdoc />
        protected override void OnStarted2(DateTime time)
        {
                base.OnStarted2(time);

                _cci = new CommodityChannelIndex { Length = CciPeriod };
                _stochastic = new StochasticOscillator();
                _stochastic.K.Length = StochasticKPeriod;
                _stochastic.D.Length = StochasticDPeriod;

                var subscription = SubscribeCandles(CandleType);

                subscription
                        .Bind(ProcessCandle)
                        .Start();

                // Configure automatic position protection for stop loss and take profit.
                StartProtection(
                        takeProfit: CreatePriceUnit(TakeProfitPoints),
                        stopLoss: CreatePriceUnit(StopLossPoints));
        }

        private void ProcessCandle(ICandleMessage candle)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                var cciResult = _cci.Process(candle).ToNullableDecimal();
                var stochResult = _stochastic.Process(candle);

                UpdateHistory(candle);

                if (cciResult is null)
                        return;

                if (!_stochastic.IsFormed)
                        return;

                var stochValue = (StochasticOscillatorValue)stochResult;
                if (stochValue.K is not decimal stochMain)
                        return;

                if (!IsFormedAndOnlineAndAllowTrading())
                        return;

                var cci = cciResult.Value;

                if (_previousOpen is decimal prevOpen &&
                        _previousClose is decimal prevClose &&
                        _typicalHistory[4] is decimal shiftedTypical)
                {
                        var buySignal = cci <= -CciThreshold && stochMain < StochasticOversold && prevOpen > shiftedTypical;
                        var sellSignal = cci >= CciThreshold && stochMain > StochasticOverbought && prevClose < shiftedTypical;

                        if (buySignal && Position <= 0)
                                BuyMarket();
                        else if (sellSignal && Position >= 0)
                                SellMarket();
                }
        }

        private void UpdateHistory(ICandleMessage candle)
        {
                // Shift stored typical prices to keep five previous values available.
                for (var i = _typicalHistory.Length - 1; i > 0; i--)
                        _typicalHistory[i] = _typicalHistory[i - 1];

                _typicalHistory[0] = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

                // Store previous candle prices for the next iteration.
                _previousOpen = candle.OpenPrice;
                _previousClose = candle.ClosePrice;
        }

        private Unit CreatePriceUnit(decimal points)
        {
                if (points <= 0)
                        return new Unit(0m, UnitTypes.Absolute);

                if (Security?.PriceStep is decimal priceStep && priceStep > 0)
                        return new Unit(points * priceStep, UnitTypes.Absolute);

                return new Unit(points, UnitTypes.Absolute);
        }
}

