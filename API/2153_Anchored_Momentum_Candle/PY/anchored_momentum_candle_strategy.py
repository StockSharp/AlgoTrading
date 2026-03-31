import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class anchored_momentum_candle_strategy(Strategy):
    def __init__(self):
        super(anchored_momentum_candle_strategy, self).__init__()
        self._mom_period = self.Param("MomPeriod", 8) \
            .SetDisplay("Momentum Period", "SMA length", "Parameters")
        self._smooth_period = self.Param("SmoothPeriod", 6) \
            .SetDisplay("Smooth Period", "EMA length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Working timeframe", "General")
        self._open_queue = []
        self._close_queue = []
        self._sum_open = 0.0
        self._sum_close = 0.0
        self._ema_open = 0.0
        self._ema_close = 0.0
        self._ema_init = False
        self._prev_color = None

    @property
    def mom_period(self):
        return self._mom_period.Value

    @property
    def smooth_period(self):
        return self._smooth_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(anchored_momentum_candle_strategy, self).OnReseted()
        self._open_queue = []
        self._close_queue = []
        self._sum_open = 0.0
        self._sum_close = 0.0
        self._ema_open = 0.0
        self._ema_close = 0.0
        self._ema_init = False
        self._prev_color = None

    def OnStarted2(self, time):
        super(anchored_momentum_candle_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        open_p = float(candle.OpenPrice)
        close_p = float(candle.ClosePrice)
        mp = int(self.mom_period)
        sp = int(self.smooth_period)
        self._sum_open += open_p
        self._open_queue.append(open_p)
        if len(self._open_queue) > mp:
            self._sum_open -= self._open_queue.pop(0)
        self._sum_close += close_p
        self._close_queue.append(close_p)
        if len(self._close_queue) > mp:
            self._sum_close -= self._close_queue.pop(0)
        k = 2.0 / (sp + 1)
        if not self._ema_init:
            self._ema_open = open_p
            self._ema_close = close_p
            self._ema_init = True
        else:
            self._ema_open = k * open_p + (1.0 - k) * self._ema_open
            self._ema_close = k * close_p + (1.0 - k) * self._ema_close
        if len(self._open_queue) < mp:
            return
        sma_open = self._sum_open / mp
        sma_close = self._sum_close / mp
        open_mom = 100.0 * (self._ema_open / sma_open - 1.0) if sma_open != 0 else 0.0
        close_mom = 100.0 * (self._ema_close / sma_close - 1.0) if sma_close != 0 else 0.0
        if open_mom < close_mom:
            color = 2.0
        elif open_mom > close_mom:
            color = 0.0
        else:
            color = 1.0
        if self._prev_color is None:
            self._prev_color = color
            return
        if color == 2.0 and self._prev_color != 2.0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif color == 0.0 and self._prev_color != 0.0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_color = color

    def CreateClone(self):
        return anchored_momentum_candle_strategy()
