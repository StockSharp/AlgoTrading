import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class ema_wpr_retracement_strategy(Strategy):
    def __init__(self):
        super(ema_wpr_retracement_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA period for trend", "Trend")
        self._wpr_period = self.Param("WprPeriod", 14) \
            .SetDisplay("WPR Period", "Williams %R period", "Signals")
        self._wpr_retracement = self.Param("WprRetracement", TimeSpan.FromHours(4)) \
            .SetDisplay("WPR Retracement", "Retracement needed for next trade", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._can_buy = True
        self._can_sell = True
        self._prev_ema = 0.0
        self._has_prev_ema = False

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
        super(ema_wpr_retracement_strategy, self).OnReseted()
        self._can_buy = True
        self._can_sell = True
        self._prev_ema = 0.0
        self._has_prev_ema = False

    def OnStarted(self, time):
        super(ema_wpr_retracement_strategy, self).OnStarted(time)
        wpr = WilliamsR()
        wpr.Length = self.wpr_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wpr, ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, wpr, ema):
        if candle.State != CandleStates.Finished:
            return
        ema_up = self._has_prev_ema and ema > self._prev_ema
        ema_down = self._has_prev_ema and ema < self._prev_ema
        # Retracement gating: after a buy at oversold, require WPR to retrace above threshold before next buy
        if wpr > -100 + self.wpr_retracement:
            self._can_buy = True
        if wpr < -self.wpr_retracement:
            self._can_sell = True
        # Oversold buy with uptrend
        if wpr <= -80 and self._can_buy and ema_up and self.Position <= 0:
            self.BuyMarket()
            self._can_buy = False
        # Overbought sell with downtrend
        elif wpr >= -20 and self._can_sell and ema_down and self.Position >= 0:
            self.SellMarket()
            self._can_sell = False
        self._prev_ema = ema
        self._has_prev_ema = True

    def CreateClone(self):
        return ema_wpr_retracement_strategy()
