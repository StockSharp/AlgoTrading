import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rsi_rftl_strategy(Strategy):
    def __init__(self):
        super(rsi_rftl_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Length of the RSI oscillator", "Indicator")
        self._ema_period = self.Param("EmaPeriod", 44) \
            .SetDisplay("EMA Period", "Length of the trend EMA filter", "Indicator")
        self._overbought = self.Param("Overbought", 75.0) \
            .SetDisplay("Overbought", "RSI overbought level", "Levels")
        self._oversold = self.Param("Oversold", 25.0) \
            .SetDisplay("Oversold", "RSI oversold level", "Levels")

        self._rsi = None
        self._ema = None
        self._prev_rsi = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def overbought(self):
        return self._overbought.Value

    @property
    def oversold(self):
        return self._oversold.Value

    def OnReseted(self):
        super(rsi_rftl_strategy, self).OnReseted()
        self._rsi = None
        self._ema = None
        self._prev_rsi = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(rsi_rftl_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._rsi, self._ema, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        ema_val = float(ema_value)

        if not self._rsi.IsFormed or not self._ema.IsFormed:
            self._prev_rsi = rsi_val
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rsi_val
            return

        close = float(candle.ClosePrice)
        trend_up = close > ema_val
        trend_down = close < ema_val
        ob = float(self.overbought)
        os_level = float(self.oversold)

        # Buy: RSI crosses above oversold + uptrend
        if self._prev_rsi < os_level and rsi_val >= os_level and trend_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 10

        # Sell: RSI crosses below overbought + downtrend
        elif self._prev_rsi > ob and rsi_val <= ob and trend_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 10

        # Exit long on strong overbought
        if self.Position > 0 and rsi_val > 80.0:
            self.SellMarket()
            self._entry_price = 0.0
            self._cooldown = 10
        # Exit short on strong oversold
        elif self.Position < 0 and rsi_val < 20.0:
            self.BuyMarket()
            self._entry_price = 0.0
            self._cooldown = 10

        self._prev_rsi = rsi_val

    def CreateClone(self):
        return rsi_rftl_strategy()
