import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class trendless_ag_hist_strategy(Strategy):

    def __init__(self):
        super(trendless_ag_hist_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 7) \
            .SetDisplay("Fast Length", "Period of the first smoothing", "Parameters")
        self._slow_length = self.Param("SlowLength", 5) \
            .SetDisplay("Slow Length", "Period of the second smoothing", "Parameters")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Loss limit in price units", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Profit target in price units", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(12))) \
            .SetDisplay("Candle Type", "Working candle timeframe", "General")

        self._fast_ema = None
        self._slow_ema = None
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._initialized = False
        self._entry_price = 0.0
        self._is_long = False
        self._bars_since_trade = 0

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _compute_trendless(self, candle):
        price = float(candle.ClosePrice)

        fast_result = self._fast_ema.Process(
            DecimalIndicatorValue(self._fast_ema, price, candle.OpenTime, True))
        fast_val = price if fast_result.IsEmpty else float(fast_result)

        diff = price - fast_val

        slow_result = self._slow_ema.Process(
            DecimalIndicatorValue(self._slow_ema, diff, candle.OpenTime, True))
        slow_val = diff if slow_result.IsEmpty else float(slow_result)

        return slow_val

    def _check_risk(self, price):
        if self._is_long and self.Position > 0:
            if price <= self._entry_price - self.StopLoss or price >= self._entry_price + self.TakeProfit:
                self.SellMarket(self.Position)
        elif not self._is_long and self.Position < 0:
            if price >= self._entry_price + self.StopLoss or price <= self._entry_price - self.TakeProfit:
                self.BuyMarket(abs(self.Position))

    def OnStarted(self, time):
        super(trendless_ag_hist_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastLength
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        value = self._compute_trendless(candle)

        if not self._initialized:
            self._prev2 = self._prev1
            self._prev1 = value
            self._initialized = True
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev2 = self._prev1
            self._prev1 = value
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        if (self._bars_since_trade >= self.CooldownBars
                and self._prev1 < self._prev2
                and value > self._prev1
                and self._prev1 < 0.0):
            if self.Position <= 0:
                self._entry_price = float(candle.ClosePrice)
                self._is_long = True
                self.BuyMarket(self.Volume + abs(self.Position))
                self._bars_since_trade = 0
        elif (self._bars_since_trade >= self.CooldownBars
              and self._prev1 > self._prev2
              and value < self._prev1
              and self._prev1 > 0.0):
            if self.Position >= 0:
                self._entry_price = float(candle.ClosePrice)
                self._is_long = False
                self.SellMarket(self.Volume + abs(self.Position))
                self._bars_since_trade = 0

        self._prev2 = self._prev1
        self._prev1 = value

        if self.Position != 0 and self._entry_price != 0.0:
            self._check_risk(float(candle.ClosePrice))

    def OnReseted(self):
        super(trendless_ag_hist_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._initialized = False
        self._entry_price = 0.0
        self._is_long = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return trendless_ag_hist_strategy()
