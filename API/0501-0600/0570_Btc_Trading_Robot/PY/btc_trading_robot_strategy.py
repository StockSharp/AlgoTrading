import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class btc_trading_robot_strategy(Strategy):
    def __init__(self):
        super(btc_trading_robot_strategy, self).__init__()
        self._fast_ema_period = self.Param("FastEmaPeriod", 120) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 450) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 3.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 5.0) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0
        self._entry_price = 0.0

    @property
    def fast_ema_period(self):
        return self._fast_ema_period.Value
    @property
    def slow_ema_period(self):
        return self._slow_ema_period.Value
    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value
    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(btc_trading_robot_strategy, self).OnReseted()
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(btc_trading_robot_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_ema_period
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_ema_value, slow_ema_value):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_fast_ema == 0 or self._prev_slow_ema == 0:
            self._prev_fast_ema = float(fast_ema_value)
            self._prev_slow_ema = float(slow_ema_value)
            return

        price = float(candle.ClosePrice)

        if self.Position > 0 and self._entry_price > 0:
            pnl_pct = (price - self._entry_price) / self._entry_price * 100.0
            if pnl_pct >= float(self.take_profit_percent) or pnl_pct <= -float(self.stop_loss_percent):
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0 and self._entry_price > 0:
            pnl_pct = (self._entry_price - price) / self._entry_price * 100.0
            if pnl_pct >= float(self.take_profit_percent) or pnl_pct <= -float(self.stop_loss_percent):
                self.BuyMarket()
                self._entry_price = 0.0

        if self._prev_fast_ema <= self._prev_slow_ema and fast_ema_value > slow_ema_value and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = price
        elif self._prev_fast_ema >= self._prev_slow_ema and fast_ema_value < slow_ema_value and self.Position >= 0:
            self.SellMarket()
            self._entry_price = price

        self._prev_fast_ema = float(fast_ema_value)
        self._prev_slow_ema = float(slow_ema_value)

    def CreateClone(self):
        return btc_trading_robot_strategy()
