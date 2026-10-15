import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_macd_position_averaging_v2_strategy(Strategy):
    def __init__(self):
        super(ma_macd_position_averaging_v2_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 15) \
            .SetDisplay("Fast Period", "Fast WMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 100) \
            .SetDisplay("Slow Period", "Slow WMA period", "Indicator")
        self._ema_period = self.Param("EmaPeriod", 200) \
            .SetDisplay("EMA Period", "EMA trend filter period", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Take Profit", "Take-profit in price steps", "Risk")

        self._fast = None
        self._slow = None
        self._ema = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value

    @property
    def take_profit_points(self):
        return self._take_profit_points.Value

    def OnReseted(self):
        super(ma_macd_position_averaging_v2_strategy, self).OnReseted()
        self._fast = None
        self._slow = None
        self._ema = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(ma_macd_position_averaging_v2_strategy, self).OnStarted2(time)

        self._fast = WeightedMovingAverage()
        self._fast.Length = self.fast_period
        self._slow = WeightedMovingAverage()
        self._slow.Length = self.slow_period
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._fast, self._slow, self._ema, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, fast_value, slow_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        ema_val = float(ema_value)

        if not self._fast.IsFormed or not self._slow.IsFormed or not self._ema.IsFormed:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0

        # Check SL/TP
        if self.Position > 0 and self._entry_price > 0:
            if self.stop_loss_points > 0 and close <= self._entry_price - self.stop_loss_points * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if self.take_profit_points > 0 and close >= self._entry_price + self.take_profit_points * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self.stop_loss_points > 0 and close >= self._entry_price + self.stop_loss_points * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if self.take_profit_points > 0 and close <= self._entry_price - self.take_profit_points * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 80
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return

        # WMA crossover with EMA trend filter
        if self._prev_fast <= self._prev_slow and fast_val > slow_val and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 80
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 80

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return ma_macd_position_averaging_v2_strategy()
