import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class lego_ea_strategy(Strategy):
    """
    Lego EA: SMA crossover with SL/TP in price steps and cooldown.
    """

    def __init__(self):
        super(lego_ea_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Fast Period", "Fast SMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 67) \
            .SetDisplay("Slow Period", "Slow SMA period", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Take Profit", "Take-profit in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lego_ea_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(lego_ea_strategy, self).OnStarted2(time)

        fast = SimpleMovingAverage()
        fast.Length = self._fast_period.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        slow = float(slow_val)

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return

        close = float(candle.ClosePrice)
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        sl = self._stop_loss_points.Value
        tp = self._take_profit_points.Value

        if self.Position > 0 and self._entry_price > 0:
            if sl > 0 and close <= self._entry_price - sl * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = fast
                self._prev_slow = slow
                return
            if tp > 0 and close >= self._entry_price + tp * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = fast
                self._prev_slow = slow
                return
        elif self.Position < 0 and self._entry_price > 0:
            if sl > 0 and close >= self._entry_price + sl * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = fast
                self._prev_slow = slow
                return
            if tp > 0 and close <= self._entry_price - tp * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = fast
                self._prev_slow = slow
                return

        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 80
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 80

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return lego_ea_strategy()
