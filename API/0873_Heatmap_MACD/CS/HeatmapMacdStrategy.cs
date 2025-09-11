using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD heatmap strategy using five timeframes.
/// Opens long when all MACD histograms cross above zero.
/// Opens short when all MACD histograms cross below zero.
/// Optionally closes on opposite signal.
/// </summary>
public class HeatmapMacdStrategy : Strategy
{
        private readonly StrategyParam<int> _fastLength;
        private readonly StrategyParam<int> _slowLength;
        private readonly StrategyParam<int> _signalLength;
        private readonly StrategyParam<DataType> _timeFrame1;
        private readonly StrategyParam<DataType> _timeFrame2;
        private readonly StrategyParam<DataType> _timeFrame3;
        private readonly StrategyParam<DataType> _timeFrame4;
        private readonly StrategyParam<DataType> _timeFrame5;
        private readonly StrategyParam<bool> _closeOnOpposite;

        private decimal _hist1, _hist2, _hist3, _hist4, _hist5;
        private bool _ready1, _ready2, _ready3, _ready4, _ready5;
        private int _prevBullCount, _prevBearCount;

        /// <summary>
        /// Fast EMA length for MACD.
        /// </summary>
        public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

        /// <summary>
        /// Slow EMA length for MACD.
        /// </summary>
        public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

        /// <summary>
        /// Signal MA length for MACD.
        /// </summary>
        public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

        /// <summary>
        /// First timeframe.
        /// </summary>
        public DataType TimeFrame1 { get => _timeFrame1.Value; set => _timeFrame1.Value = value; }

        /// <summary>
        /// Second timeframe.
        /// </summary>
        public DataType TimeFrame2 { get => _timeFrame2.Value; set => _timeFrame2.Value = value; }

        /// <summary>
        /// Third timeframe.
        /// </summary>
        public DataType TimeFrame3 { get => _timeFrame3.Value; set => _timeFrame3.Value = value; }

        /// <summary>
        /// Fourth timeframe.
        /// </summary>
        public DataType TimeFrame4 { get => _timeFrame4.Value; set => _timeFrame4.Value = value; }

        /// <summary>
        /// Fifth timeframe.
        /// </summary>
        public DataType TimeFrame5 { get => _timeFrame5.Value; set => _timeFrame5.Value = value; }

        /// <summary>
        /// Close position when not all MACDs align.
        /// </summary>
        public bool CloseOnOpposite { get => _closeOnOpposite.Value; set => _closeOnOpposite.Value = value; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public HeatmapMacdStrategy()
        {
                _fastLength = Param(nameof(FastLength), 9)
                        .SetGreaterThanZero()
                        .SetDisplay("Fast Length", "Fast EMA length", "MACD")
                        .SetCanOptimize(true);

                _slowLength = Param(nameof(SlowLength), 26)
                        .SetGreaterThanZero()
                        .SetDisplay("Slow Length", "Slow EMA length", "MACD")
                        .SetCanOptimize(true);

                _signalLength = Param(nameof(SignalLength), 9)
                        .SetGreaterThanZero()
                        .SetDisplay("Signal Length", "Signal MA length", "MACD")
                        .SetCanOptimize(true);

                _timeFrame1 = Param(nameof(TimeFrame1), TimeSpan.FromMinutes(60).TimeFrame())
                        .SetDisplay("TimeFrame1", "First timeframe", "Timeframes");
                _timeFrame2 = Param(nameof(TimeFrame2), TimeSpan.FromMinutes(120).TimeFrame())
                        .SetDisplay("TimeFrame2", "Second timeframe", "Timeframes");
                _timeFrame3 = Param(nameof(TimeFrame3), TimeSpan.FromMinutes(240).TimeFrame())
                        .SetDisplay("TimeFrame3", "Third timeframe", "Timeframes");
                _timeFrame4 = Param(nameof(TimeFrame4), TimeSpan.FromMinutes(240).TimeFrame())
                        .SetDisplay("TimeFrame4", "Fourth timeframe", "Timeframes");
                _timeFrame5 = Param(nameof(TimeFrame5), TimeSpan.FromMinutes(480).TimeFrame())
                        .SetDisplay("TimeFrame5", "Fifth timeframe", "Timeframes");

                _closeOnOpposite = Param(nameof(CloseOnOpposite), false)
                        .SetDisplay("Close On Opposite", "Close position if not all MACDs align", "General");
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
                return [(Security, TimeFrame1), (Security, TimeFrame2), (Security, TimeFrame3), (Security, TimeFrame4), (Security, TimeFrame5)];
        }

        protected override void OnReseted()
        {
                base.OnReseted();
                _hist1 = _hist2 = _hist3 = _hist4 = _hist5 = 0m;
                _ready1 = _ready2 = _ready3 = _ready4 = _ready5 = false;
                _prevBullCount = 0;
                _prevBearCount = 0;
        }

        protected override void OnStarted(DateTimeOffset time)
        {
                base.OnStarted(time);

                var macd1 = CreateMacd();
                var macd2 = CreateMacd();
                var macd3 = CreateMacd();
                var macd4 = CreateMacd();
                var macd5 = CreateMacd();

                var sub1 = SubscribeCandles(TimeFrame1);
                var sub2 = SubscribeCandles(TimeFrame2);
                var sub3 = SubscribeCandles(TimeFrame3);
                var sub4 = SubscribeCandles(TimeFrame4);
                var sub5 = SubscribeCandles(TimeFrame5);

                sub1.BindEx(macd1, ProcessTf1).Start();
                sub2.BindEx(macd2, ProcessTf2).Start();
                sub3.BindEx(macd3, ProcessTf3).Start();
                sub4.BindEx(macd4, ProcessTf4).Start();
                sub5.BindEx(macd5, ProcessTf5).Start();

                var area = CreateChartArea();
                if (area != null)
                {
                        DrawCandles(area, sub1);
                        DrawIndicator(area, macd1);
                        DrawOwnTrades(area);
                }

                StartProtection();
        }

        private void ProcessTf1(ICandleMessage candle, IIndicatorValue value)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                var v = (MovingAverageConvergenceDivergenceSignalValue)value;
                if (v.Macd is not decimal macd || v.Signal is not decimal signal)
                        return;

                _hist1 = macd - signal;
                _ready1 = true;
                EvaluateSignals();
        }

        private void ProcessTf2(ICandleMessage candle, IIndicatorValue value)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                var v = (MovingAverageConvergenceDivergenceSignalValue)value;
                if (v.Macd is not decimal macd || v.Signal is not decimal signal)
                        return;

                _hist2 = macd - signal;
                _ready2 = true;
                EvaluateSignals();
        }

        private void ProcessTf3(ICandleMessage candle, IIndicatorValue value)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                var v = (MovingAverageConvergenceDivergenceSignalValue)value;
                if (v.Macd is not decimal macd || v.Signal is not decimal signal)
                        return;

                _hist3 = macd - signal;
                _ready3 = true;
                EvaluateSignals();
        }

        private void ProcessTf4(ICandleMessage candle, IIndicatorValue value)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                var v = (MovingAverageConvergenceDivergenceSignalValue)value;
                if (v.Macd is not decimal macd || v.Signal is not decimal signal)
                        return;

                _hist4 = macd - signal;
                _ready4 = true;
                EvaluateSignals();
        }

        private void ProcessTf5(ICandleMessage candle, IIndicatorValue value)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                var v = (MovingAverageConvergenceDivergenceSignalValue)value;
                if (v.Macd is not decimal macd || v.Signal is not decimal signal)
                        return;

                _hist5 = macd - signal;
                _ready5 = true;
                EvaluateSignals();
        }

        private void EvaluateSignals()
        {
                if (!(_ready1 && _ready2 && _ready3 && _ready4 && _ready5))
                        return;

                var bullCount = (_hist1 > 0m ? 1 : 0) + (_hist2 > 0m ? 1 : 0) + (_hist3 > 0m ? 1 : 0) + (_hist4 > 0m ? 1 : 0) + (_hist5 > 0m ? 1 : 0);
                var bearCount = (_hist1 < 0m ? 1 : 0) + (_hist2 < 0m ? 1 : 0) + (_hist3 < 0m ? 1 : 0) + (_hist4 < 0m ? 1 : 0) + (_hist5 < 0m ? 1 : 0);

                if (!IsFormedAndOnlineAndAllowTrading())
                {
                        _prevBullCount = bullCount;
                        _prevBearCount = bearCount;
                        return;
                }

                var bull = bullCount == 5 && _prevBullCount < 5;
                var bear = bearCount == 5 && _prevBearCount < 5;

                if (bull && Position <= 0)
                        BuyMarket(Volume + Math.Abs(Position));
                else if (bear && Position >= 0)
                        SellMarket(Volume + Math.Abs(Position));

                if (CloseOnOpposite)
                {
                        if (Position > 0 && _prevBullCount == 5 && bullCount < 5)
                                SellMarket(Math.Abs(Position));
                        else if (Position < 0 && _prevBearCount == 5 && bearCount < 5)
                                BuyMarket(Math.Abs(Position));
                }

                _prevBullCount = bullCount;
                _prevBearCount = bearCount;
        }

        private MovingAverageConvergenceDivergenceSignal CreateMacd()
        {
                return new()
                {
                        Macd =
                        {
                                ShortMa = new ExponentialMovingAverage { Length = FastLength },
                                LongMa = new ExponentialMovingAverage { Length = SlowLength }
                        },
                        SignalMa = new ExponentialMovingAverage { Length = SignalLength }
                };
        }
}
