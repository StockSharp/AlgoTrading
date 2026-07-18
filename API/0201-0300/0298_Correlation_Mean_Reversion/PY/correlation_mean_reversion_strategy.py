import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from System import Decimal, ValueTuple
from StockSharp.Algo.Indicators import Correlation, SimpleMovingAverage, StandardDeviation, PairIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from indicator_extensions import *

class correlation_mean_reversion_strategy(Strategy):
    """
    Mean-reversion strategy that uses rolling inter-market correlation as a regime filter.
    Trades the primary security when a low-correlation regime coincides with short-term
    divergence versus the secondary security.
    """

    def __init__(self):
        super(correlation_mean_reversion_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Second Security Id", "Identifier of the secondary security", "General")

        self._correlation_period = self.Param("CorrelationPeriod", 20) \
            .SetDisplay("Correlation Period", "Rolling period for the correlation indicator", "Indicators")

        self._lookback_period = self.Param("LookbackPeriod", 30) \
            .SetDisplay("Lookback Period", "Lookback period for correlation statistics", "Indicators")

        self._deviation_threshold = self.Param("DeviationThreshold", 1.1) \
            .SetDisplay("Deviation Threshold", "Absolute Z-score required for entry", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.15) \
            .SetDisplay("Exit Threshold", "Z-score threshold used for exit", "Signals")

        self._divergence_threshold = self.Param("DivergenceThreshold", 0.003) \
            .SetDisplay("Divergence Threshold", "Minimum one-bar divergence between the two instruments", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 120) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle series for both instruments", "General")

        self._security2 = None
        self._correlation = None
        self._correlation_sma = None
        self._correlation_std_dev = None
        self._latest_price1 = Decimal(0)
        self._latest_price2 = Decimal(0)
        self._previous_price1 = Decimal(0)
        self._previous_price2 = Decimal(0)
        self._primary_updated = False
        self._secondary_updated = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(correlation_mean_reversion_strategy, self).OnReseted()
        self._security2 = None
        self._correlation = None
        self._correlation_sma = None
        self._correlation_std_dev = None
        self._latest_price1 = Decimal(0)
        self._latest_price2 = Decimal(0)
        self._previous_price1 = Decimal(0)
        self._previous_price2 = Decimal(0)
        self._primary_updated = False
        self._secondary_updated = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(correlation_mean_reversion_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Secondary security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._security2 = s

        self._correlation = Correlation()
        self._correlation.Length = int(self._correlation_period.Value)
        self._correlation_sma = SimpleMovingAverage()
        self._correlation_sma.Length = int(self._lookback_period.Value)
        self._correlation_std_dev = StandardDeviation()
        self._correlation_std_dev.Length = int(self._lookback_period.Value)
        self._cooldown = 0

        primary_subscription = self.SubscribeCandles(self.candle_type, False, self.Security)
        secondary_subscription = self.SubscribeCandles(self.candle_type, False, self._security2)

        primary_subscription.Bind(self._process_primary_candle).Start()
        secondary_subscription.Bind(self._process_secondary_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_subscription)
            self.DrawCandles(area, secondary_subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_percent.Value, UnitTypes.Percent), False)

    def _process_primary_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_price1 = candle.ClosePrice
        self._primary_updated = True
        self._try_process_pair(candle.OpenTime)

    def _process_secondary_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_price2 = candle.ClosePrice
        self._secondary_updated = True
        self._try_process_pair(candle.OpenTime)

    def _try_process_pair(self, time):
        if not self._primary_updated or not self._secondary_updated:
            return

        self._primary_updated = False
        self._secondary_updated = False

        # Convert DateTimeOffset to DateTime if needed
        try:
            time = time.UtcDateTime
        except:
            pass

        if self._latest_price1 <= 0 or self._latest_price2 <= 0:
            return

        if self._previous_price1 <= 0 or self._previous_price2 <= 0:
            self._previous_price1 = self._latest_price1
            self._previous_price2 = self._latest_price2
            return

        pair_val = ValueTuple[Decimal, Decimal](self._latest_price1, self._latest_price2)
        pair_input = PairIndicatorValue[Decimal](self._correlation, pair_val, time)
        pair_input.IsFinal = True
        corr_result = self._correlation.Process(pair_input)
        correlation_value = float(corr_result)

        avg_result = process_float(self._correlation_sma, Decimal(correlation_value), time, True)
        average_correlation = float(avg_result)

        std_result = process_float(self._correlation_std_dev, Decimal(correlation_value), time, True)
        std_correlation = float(std_result)

        primary_return = float(self._latest_price1 - self._previous_price1) / float(self._previous_price1)
        secondary_return = float(self._latest_price2 - self._previous_price2) / float(self._previous_price2)
        divergence = primary_return - secondary_return

        self._previous_price1 = self._latest_price1
        self._previous_price2 = self._latest_price2

        if not self._correlation.IsFormed or not self._correlation_sma.IsFormed or not self._correlation_std_dev.IsFormed:
            return

        from StockSharp.Algo import ProcessStates as PS
        if self.ProcessState != PS.Started:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if std_correlation <= 0:
            return

        z_score = (correlation_value - average_correlation) / std_correlation
        dev_threshold = float(self._deviation_threshold.Value)
        is_low_correlation = z_score <= -dev_threshold

        exit_threshold = float(self._exit_threshold.Value)
        div_threshold = float(self._divergence_threshold.Value)
        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if not is_low_correlation:
                return

            if divergence <= -div_threshold:
                self.BuyMarket()
                self._cooldown = cd
            elif divergence >= div_threshold:
                self.SellMarket()
                self._cooldown = cd
            return

        correlation_recovered = z_score >= -exit_threshold

        if self.Position > 0 and (correlation_recovered or divergence >= -div_threshold * 0.5):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = cd
        elif self.Position < 0 and (correlation_recovered or divergence <= div_threshold * 0.5):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = cd

    def CreateClone(self):
        return correlation_mean_reversion_strategy()
