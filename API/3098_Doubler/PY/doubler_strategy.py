import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class doubler_strategy(Strategy):
    def __init__(self):
        super(doubler_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 20) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._med_period = self.Param("MedPeriod", 50) \
            .SetDisplay("Medium Period", "Medium EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 200) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 150) \
            .SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 300) \
            .SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk")

        self._fast = None
        self._med = None
        self._slow = None
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def med_period(self):
        return self._med_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value

    @property
    def take_profit_points(self):
        return self._take_profit_points.Value

    def OnReseted(self):
        super(doubler_strategy, self).OnReseted()
        self._fast = None
        self._med = None
        self._slow = None
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(doubler_strategy, self).OnStarted(time)

        self._fast = ExponentialMovingAverage()
        self._fast.Length = self.fast_period
        self._med = ExponentialMovingAverage()
        self._med.Length = self.med_period
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self.slow_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._fast, self._med, self._slow, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, fast_value, med_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        med_val = float(med_value)
        slow_val = float(slow_value)

        if not self._fast.IsFormed or not self._med.IsFormed or not self._slow.IsFormed:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0

        # Check SL/TP
        if self.Position > 0 and self._entry_price > 0:
            if self.stop_loss_points > 0 and close <= self._entry_price - self.stop_loss_points * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                return
            if self.take_profit_points > 0 and close >= self._entry_price + self.take_profit_points * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self.stop_loss_points > 0 and close >= self._entry_price + self.stop_loss_points * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                return
            if self.take_profit_points > 0 and close <= self._entry_price - self.take_profit_points * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                return

        # Double confirmation: both fast and med above slow for long
        if fast_val > slow_val and med_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 100
        # Double confirmation: both fast and med below slow for short
        elif fast_val < slow_val and med_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 100

    def CreateClone(self):
        return doubler_strategy()
