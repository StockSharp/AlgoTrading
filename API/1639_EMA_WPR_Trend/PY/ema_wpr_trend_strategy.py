import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class ema_wpr_trend_strategy(Strategy):
    def __init__(self):
        super(ema_wpr_trend_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "Period for EMA", "Indicators")
        self._wpr_period = self.Param("WprPeriod", 14) \
            .SetDisplay("WPR Period", "%R length", "Indicators")
        self._wpr_retracement = self.Param("WprRetracement", 30) \
            .SetDisplay("WPR Retracement", "Retracement for next trade", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._buy_allowed = False
        self._sell_allowed = False
        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._has_prev_ema = False
        self._trend_counter = 0

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def wpr_period(self):
        return self._wpr_period.Value

    @property
    def wpr_retracement(self):
        return self._wpr_retracement.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_wpr_trend_strategy, self).OnReseted()
        self._buy_allowed = False
        self._sell_allowed = False
        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._has_prev_ema = False
        self._trend_counter = 0

    def OnStarted(self, time):
        super(ema_wpr_trend_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        wpr = WilliamsR()
        wpr.Length = self.wpr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, wpr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_value, wpr_value):
        if candle.State != CandleStates.Finished:
            return
        # Update trend
        if self._has_prev_ema:
            if ema_value > self._prev_ema:
                self._trend_counter = min(self._trend_counter + 1, 1)
            elif ema_value < self._prev_ema:
                self._trend_counter = max(self._trend_counter - 1, -1)
            else:
                self._trend_counter = 0
        self._prev_ema = ema_value
        self._has_prev_ema = True
        price = candle.ClosePrice
        # Exit: WPR at opposite extreme
        if self.Position > 0 and wpr_value >= -20:
            self.SellMarket()
            self._entry_price = 0
        elif self.Position < 0 and wpr_value <= -80:
            self.BuyMarket()
            self._entry_price = 0
        # Retracement flags
        if wpr_value > -100 + self.wpr_retracement:
            self._buy_allowed = True
        if wpr_value < 0 - self.wpr_retracement:
            self._sell_allowed = True
        trend_up = self._trend_counter >= 1
        trend_down = self._trend_counter <= -1
        if self.Position <= 0 and self._buy_allowed and wpr_value <= -80 and trend_up:
            self.BuyMarket()
            self._entry_price = price
            self._buy_allowed = False
        elif self.Position >= 0 and self._sell_allowed and wpr_value >= -20 and trend_down:
            self.SellMarket()
            self._entry_price = price
            self._sell_allowed = False

    def CreateClone(self):
        return ema_wpr_trend_strategy()
