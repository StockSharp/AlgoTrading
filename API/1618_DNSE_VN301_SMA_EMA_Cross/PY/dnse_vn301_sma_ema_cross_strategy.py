import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class dnse_vn301_sma_ema_cross_strategy(Strategy):
    def __init__(self):
        super(dnse_vn301_sma_ema_cross_strategy, self).__init__()
        self._session_close_hour = self.Param("SessionCloseHour", 14) \
            .SetDisplay("Close Hour", "Session close hour", "General")
        self._session_close_minute = self.Param("SessionCloseMinute", 30) \
            .SetDisplay("Close Minute", "Session close minute", "General")
        self._minutes_before_close = self.Param("MinutesBeforeClose", 5) \
            .SetDisplay("Minutes Before Close", "Exit minutes before close", "General")
        self._max_loss_percent = self.Param("MaxLossPercent", 2.0) \
            .SetDisplay("Max Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._prev_ema15 = 0.0
        self._prev_sma60 = 0.0

    @property
    def session_close_hour(self):
        return self._session_close_hour.Value

    @property
    def session_close_minute(self):
        return self._session_close_minute.Value

    @property
    def minutes_before_close(self):
        return self._minutes_before_close.Value

    @property
    def max_loss_percent(self):
        return self._max_loss_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dnse_vn301_sma_ema_cross_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_ema15 = 0.0
        self._prev_sma60 = 0.0

    def OnStarted(self, time):
        super(dnse_vn301_sma_ema_cross_strategy, self).OnStarted(time)
        ema15 = ExponentialMovingAverage()
        ema15.Length = 15
        sma60 = SimpleMovingAverage()
        sma60.Length = 60
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema15, sma60, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema15, sma60):
        if candle.State != CandleStates.Finished:
            return
        cross_up = ema15 > sma60 and self._prev_ema15 <= self._prev_sma60
        cross_down = ema15 < sma60 and self._prev_ema15 >= self._prev_sma60
        self._prev_ema15 = ema15
        self._prev_sma60 = sma60
        if cross_up and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
        elif cross_down and self.Position >= 0:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
        if self.Position > 0:
            if cross_down or candle.ClosePrice <= self._entry_price * (1 - self.max_loss_percent / 100):
                self.SellMarket()
        elif self.Position < 0:
            if cross_up or candle.ClosePrice >= self._entry_price * (1 + self.max_loss_percent / 100):
                self.BuyMarket()

    def close_position(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

    def CreateClone(self):
        return dnse_vn301_sma_ema_cross_strategy()
