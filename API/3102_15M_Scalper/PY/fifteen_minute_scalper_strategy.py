import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class fifteen_minute_scalper_strategy(Strategy):
    """
    15-minute scalper using WMA crossover with SL/TP and cooldown.
    Buys when fast WMA crosses above slow WMA, sells on reverse.
    """

    def __init__(self):
        super(fifteen_minute_scalper_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 20) \
            .SetDisplay("Fast Period", "Fast WMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 85) \
            .SetDisplay("Slow Period", "Slow WMA period", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Take Profit", "Take-profit in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Trading timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fifteen_minute_scalper_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(fifteen_minute_scalper_strategy, self).OnStarted(time)

        fast = WeightedMovingAverage()
        fast.Length = self._fast_period.Value
        slow = WeightedMovingAverage()
        slow.Length = self._slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = float(fast_val)
            self._prev_slow = float(slow_val)
            return

        fast_val = float(fast_val)
        slow_val = float(slow_val)

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = float(candle.ClosePrice)
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        sl_pts = self._stop_loss_points.Value
        tp_pts = self._take_profit_points.Value

        if self.Position > 0 and self._entry_price > 0:
            if sl_pts > 0 and close <= self._entry_price - sl_pts * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if tp_pts > 0 and close >= self._entry_price + tp_pts * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
        elif self.Position < 0 and self._entry_price > 0:
            if sl_pts > 0 and close >= self._entry_price + sl_pts * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if tp_pts > 0 and close <= self._entry_price - tp_pts * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return

        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 80
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 80

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return fifteen_minute_scalper_strategy()
