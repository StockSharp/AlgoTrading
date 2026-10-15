import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class color_metro_wpr_strategy(Strategy):
    def __init__(self):
        super(color_metro_wpr_strategy, self).__init__()
        self._wpr_period = self.Param("WprPeriod", 7) \
            .SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators")
        self._fast_step = self.Param("FastStep", 5) \
            .SetDisplay("Fast Step", "Step size for fast line", "Indicators")
        self._slow_step = self.Param("SlowStep", 15) \
            .SetDisplay("Slow Step", "Step size for slow line", "Indicators")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0) \
            .SetDisplay("Take Profit (%)", "Take profit as percentage", "Risk parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss (%)", "Stop loss as percentage", "Risk parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._wpr = None
        self._f_min_prev = 999999.0
        self._f_max_prev = -999999.0
        self._s_min_prev = 999999.0
        self._s_max_prev = -999999.0
        self._f_trend = 0
        self._s_trend = 0
        self._prev_m_plus = 0.0
        self._prev_m_minus = 0.0
        self._curr_m_plus = 0.0
        self._curr_m_minus = 0.0
        self._is_first_value = True

    @property
    def wpr_period(self):
        return self._wpr_period.Value

    @property
    def fast_step(self):
        return self._fast_step.Value

    @property
    def slow_step(self):
        return self._slow_step.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_metro_wpr_strategy, self).OnReseted()
        self._f_min_prev = 999999.0
        self._f_max_prev = -999999.0
        self._s_min_prev = 999999.0
        self._s_max_prev = -999999.0
        self._f_trend = 0
        self._s_trend = 0
        self._prev_m_plus = 0.0
        self._prev_m_minus = 0.0
        self._curr_m_plus = 0.0
        self._curr_m_minus = 0.0
        self._is_first_value = True
        self._wpr = None

    def OnStarted2(self, time):
        super(color_metro_wpr_strategy, self).OnStarted2(time)
        self._wpr = WilliamsR()
        self._wpr.Length = self.wpr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._wpr, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._wpr)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(float(self.take_profit_percent), UnitTypes.Percent),
            stopLoss=Unit(float(self.stop_loss_percent), UnitTypes.Percent))

    def process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        wpr = float(wpr_value) + 100.0
        fs = float(self.fast_step)
        ss = float(self.slow_step)

        fmax0 = wpr + 2.0 * fs
        fmin0 = wpr - 2.0 * fs

        if wpr > self._f_max_prev:
            self._f_trend = 1
        if wpr < self._f_min_prev:
            self._f_trend = -1

        if self._f_trend > 0 and fmin0 < self._f_min_prev:
            fmin0 = self._f_min_prev
        if self._f_trend < 0 and fmax0 > self._f_max_prev:
            fmax0 = self._f_max_prev

        smax0 = wpr + 2.0 * ss
        smin0 = wpr - 2.0 * ss

        if wpr > self._s_max_prev:
            self._s_trend = 1
        if wpr < self._s_min_prev:
            self._s_trend = -1

        if self._s_trend > 0 and smin0 < self._s_min_prev:
            smin0 = self._s_min_prev
        if self._s_trend < 0 and smax0 > self._s_max_prev:
            smax0 = self._s_max_prev

        m_plus = fmin0 + fs if self._f_trend > 0 else fmax0 - fs
        m_minus = smin0 + ss if self._s_trend > 0 else smax0 - ss

        self._f_min_prev = fmin0
        self._f_max_prev = fmax0
        self._s_min_prev = smin0
        self._s_max_prev = smax0

        if self._is_first_value:
            self._prev_m_plus = m_plus
            self._prev_m_minus = m_minus
            self._curr_m_plus = m_plus
            self._curr_m_minus = m_minus
            self._is_first_value = False
            return

        self._prev_m_plus = self._curr_m_plus
        self._prev_m_minus = self._curr_m_minus
        self._curr_m_plus = m_plus
        self._curr_m_minus = m_minus

        prev_fast_above_slow = self._prev_m_plus > self._prev_m_minus
        prev_fast_below_slow = self._prev_m_plus < self._prev_m_minus

        if prev_fast_above_slow:
            if self.Position < 0:
                self.BuyMarket()
            if self._curr_m_plus <= self._curr_m_minus and self.Position <= 0:
                self.BuyMarket()
        elif prev_fast_below_slow:
            if self.Position > 0:
                self.SellMarket()
            if self._curr_m_plus >= self._curr_m_minus and self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return color_metro_wpr_strategy()
