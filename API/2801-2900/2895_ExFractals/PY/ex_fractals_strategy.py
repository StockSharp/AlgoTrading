import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from collections import deque


class ex_fractals_strategy(Strategy):
    """Fractal breakout strategy that averages recent fractal levels
    and filters entries with average body momentum (ExVol)."""

    def __init__(self):
        super(ex_fractals_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ex_period = self.Param("ExPeriod", 15) \
            .SetDisplay("ExVol Period", "Average body lookback", "Indicators")

        self._body_queue = deque()
        self._body_sum = 0.0
        self._h1 = self._h2 = self._h3 = self._h4 = self._h5 = 0.0
        self._l1 = self._l2 = self._l3 = self._l4 = self._l5 = 0.0
        self._candle_count = 0

        self._up_fractal1 = None
        self._up_fractal2 = None
        self._up_count1 = 0
        self._up_count2 = 0

        self._down_fractal1 = None
        self._down_fractal2 = None
        self._down_count1 = 0
        self._down_count2 = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def ExPeriod(self):
        return self._ex_period.Value

    def OnReseted(self):
        super(ex_fractals_strategy, self).OnReseted()
        self._body_queue = deque()
        self._body_sum = 0.0
        self._h1 = self._h2 = self._h3 = self._h4 = self._h5 = 0.0
        self._l1 = self._l2 = self._l3 = self._l4 = self._l5 = 0.0
        self._candle_count = 0
        self._up_fractal1 = None
        self._up_fractal2 = None
        self._up_count1 = 0
        self._up_count2 = 0
        self._down_fractal1 = None
        self._down_fractal2 = None
        self._down_count1 = 0
        self._down_count2 = 0

    def OnStarted2(self, time):
        super(ex_fractals_strategy, self).OnStarted2(time)
        self._body_queue = deque()
        self._body_sum = 0.0
        self._h1 = self._h2 = self._h3 = self._h4 = self._h5 = 0.0
        self._l1 = self._l2 = self._l3 = self._l4 = self._l5 = 0.0
        self._candle_count = 0
        self._up_fractal1 = None
        self._up_fractal2 = None
        self._up_count1 = 0
        self._up_count2 = 0
        self._down_fractal1 = None
        self._down_fractal2 = None
        self._down_count1 = 0
        self._down_count2 = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        self._h1 = self._h2
        self._h2 = self._h3
        self._h3 = self._h4
        self._h4 = self._h5
        self._h5 = float(candle.HighPrice)

        self._l1 = self._l2
        self._l2 = self._l3
        self._l3 = self._l4
        self._l4 = self._l5
        self._l5 = float(candle.LowPrice)

        if candle.State != CandleStates.Finished:
            return

        self._candle_count += 1

        if self._candle_count >= 5:
            if (self._h3 > self._h1 and self._h3 > self._h2
                    and self._h3 > self._h4 and self._h3 > self._h5):
                self._register_up_fractal(self._h3)

            if (self._l3 < self._l1 and self._l3 < self._l2
                    and self._l3 < self._l4 and self._l3 < self._l5):
                self._register_down_fractal(self._l3)

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0001
        if step <= 0:
            step = 0.0001

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        body = (close - open_p) / step
        self._body_queue.append(body)
        self._body_sum += body
        if len(self._body_queue) > self.ExPeriod:
            self._body_sum -= self._body_queue.popleft()

        ex_vol = None
        if len(self._body_queue) >= self.ExPeriod:
            ex_vol = self._body_sum / self.ExPeriod

        upper_level = self._get_upper_level()
        lower_level = self._get_lower_level()

        if (ex_vol is not None and upper_level is not None
                and close > upper_level and ex_vol < 0 and self.Position <= 0):
            self.BuyMarket()
        elif (ex_vol is not None and lower_level is not None
              and close < lower_level and ex_vol > 0 and self.Position >= 0):
            self.SellMarket()

    def _register_up_fractal(self, price):
        if self._up_fractal1 is None:
            self._up_fractal1 = price
            self._up_count1 = self._candle_count
            return
        if self._up_count1 == self._candle_count:
            self._up_fractal1 = price
            return
        if self._up_fractal2 is None:
            self._up_fractal2 = price
            self._up_count2 = self._candle_count
            return
        if self._up_count2 == self._candle_count:
            self._up_fractal2 = price
            return
        self._up_fractal1 = self._up_fractal2
        self._up_count1 = self._up_count2
        self._up_fractal2 = price
        self._up_count2 = self._candle_count

    def _register_down_fractal(self, price):
        if self._down_fractal1 is None:
            self._down_fractal1 = price
            self._down_count1 = self._candle_count
            return
        if self._down_count1 == self._candle_count:
            self._down_fractal1 = price
            return
        if self._down_fractal2 is None:
            self._down_fractal2 = price
            self._down_count2 = self._candle_count
            return
        if self._down_count2 == self._candle_count:
            self._down_fractal2 = price
            return
        self._down_fractal1 = self._down_fractal2
        self._down_count1 = self._down_count2
        self._down_fractal2 = price
        self._down_count2 = self._candle_count

    def _get_upper_level(self):
        if (self._up_fractal1 is not None and self._up_fractal2 is not None
                and self._up_count1 != self._up_count2):
            return (self._up_fractal1 + self._up_fractal2) / 2.0
        return None

    def _get_lower_level(self):
        if (self._down_fractal1 is not None and self._down_fractal2 is not None
                and self._down_count1 != self._down_count2):
            return (self._down_fractal1 + self._down_fractal2) / 2.0
        return None

    def CreateClone(self):
        return ex_fractals_strategy()
