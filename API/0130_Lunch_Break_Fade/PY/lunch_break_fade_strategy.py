import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class lunch_break_fade_strategy(Strategy):
    """
    Fades the prior trend during lunch break (11-14h), exits on MA cross.
    """

    def __init__(self):
        super(lunch_break_fade_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 30).SetDisplay("Cooldown", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lunch_break_fade_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(lunch_break_fade_strategy, self).OnStarted(time)
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        hour = candle.OpenTime.Hour
        ma = float(ma_val)
        if self._prev_close == 0:
            self._prev_close = close
            return
        if self._prev_prev_close == 0:
            self._prev_prev_close = self._prev_close
            self._prev_close = close
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_prev_close = self._prev_close
            self._prev_close = close
            return
        is_lunch = hour >= 11 and hour <= 14
        if is_lunch:
            prior_up = self._prev_close > self._prev_prev_close
            prior_down = self._prev_close < self._prev_prev_close
            bearish = close < open_p
            bullish = close > open_p
            if prior_up and bearish and self.Position == 0:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
            elif prior_down and bullish and self.Position == 0:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
        if self.Position > 0 and close < ma:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and close > ma:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        self._prev_prev_close = self._prev_close
        self._prev_close = close

    def CreateClone(self):
        return lunch_break_fade_strategy()
