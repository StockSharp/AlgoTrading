import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ea_objprop_chart_id_strategy(Strategy):
    def __init__(self):
        super(ea_objprop_chart_id_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._period = self.Param("Period", 50)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_std_dev = 0.0
        self._prev_range = 0.0
        self._candles_since_trade = 4
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(ea_objprop_chart_id_strategy, self).OnReseted()
        self._prev_std_dev = 0.0
        self._prev_range = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

    def OnStarted2(self, time):
        super(ea_objprop_chart_id_strategy, self).OnStarted2(time)
        self._prev_std_dev = 0.0
        self._prev_range = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _process_candle(self, candle, std_dev_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        std_val = float(std_dev_value)
        range_val = high - low

        if range_val <= 0:
            return

        if self._has_prev and std_val > 0 and self._prev_range > 0:
            expanding = range_val > self._prev_range * 1.2
            bullish = close > open_price
            bearish = close < open_price

            if expanding and bullish and close > std_val and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif expanding and bearish and close < std_val and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_std_dev = std_val
        self._prev_range = range_val
        self._has_prev = True

    def CreateClone(self):
        return ea_objprop_chart_id_strategy()
