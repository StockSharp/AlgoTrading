import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WeightedMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class escort_trend_strategy(Strategy):

    def __init__(self):
        super(escort_trend_strategy, self).__init__()

        self._fast_wma_period = self.Param("FastWmaPeriod", 8) \
            .SetDisplay("Fast WMA", "Length of fast weighted MA", "General")
        self._slow_wma_period = self.Param("SlowWmaPeriod", 18) \
            .SetDisplay("Slow WMA", "Length of slow weighted MA", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI calculation period", "General")
        self._cci_threshold = self.Param("CciThreshold", 100.0) \
            .SetDisplay("CCI Threshold", "Threshold for CCI signal", "General")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def FastWmaPeriod(self):
        return self._fast_wma_period.Value

    @FastWmaPeriod.setter
    def FastWmaPeriod(self, value):
        self._fast_wma_period.Value = value

    @property
    def SlowWmaPeriod(self):
        return self._slow_wma_period.Value

    @SlowWmaPeriod.setter
    def SlowWmaPeriod(self, value):
        self._slow_wma_period.Value = value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def CciThreshold(self):
        return self._cci_threshold.Value

    @CciThreshold.setter
    def CciThreshold(self, value):
        self._cci_threshold.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

    @property
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(escort_trend_strategy, self).OnStarted(time)

        fast_wma = WeightedMovingAverage()
        fast_wma.Length = self.FastWmaPeriod
        slow_wma = WeightedMovingAverage()
        slow_wma.Length = self.SlowWmaPeriod
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast_wma, slow_wma, cci, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(self.TakeProfitPct, UnitTypes.Percent),
            stopLoss=Unit(self.StopLossPct, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, fast, slow, cci_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast)
        slow_val = float(slow)
        cci_val = float(cci_value)
        threshold = float(self.CciThreshold)

        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val and cci_val > threshold
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val and cci_val < -threshold

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def OnReseted(self):
        super(escort_trend_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def CreateClone(self):
        return escort_trend_strategy()
