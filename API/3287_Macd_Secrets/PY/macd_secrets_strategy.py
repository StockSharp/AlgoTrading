import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class macd_secrets_strategy(Strategy):
    """
    MACD Secrets: EMA crossover with manual SL/TP via price steps.
    """

    def __init__(self):
        super(macd_secrets_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 50).SetDisplay("Slow Period", "Slow EMA period", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200).SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 400).SetDisplay("Take Profit", "Take-profit in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_secrets_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(macd_secrets_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if self.Position > 0 and self._entry_price > 0:
            if self._stop_loss_points.Value > 0 and close <= self._entry_price - self._stop_loss_points.Value * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                self._prev_fast = fast
                self._prev_slow = slow
                return
            if self._take_profit_points.Value > 0 and close >= self._entry_price + self._take_profit_points.Value * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                self._prev_fast = fast
                self._prev_slow = slow
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self._stop_loss_points.Value > 0 and close >= self._entry_price + self._stop_loss_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                self._prev_fast = fast
                self._prev_slow = slow
                return
            if self._take_profit_points.Value > 0 and close <= self._entry_price - self._take_profit_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                self._prev_fast = fast
                self._prev_slow = slow
                return
        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 100
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 100
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return macd_secrets_strategy()
