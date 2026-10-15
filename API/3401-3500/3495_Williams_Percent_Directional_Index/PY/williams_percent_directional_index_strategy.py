import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class williams_percent_directional_index_strategy(Strategy):
    def __init__(self):
        super(williams_percent_directional_index_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._period = self.Param("Period", 14)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_wr = 0.0
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
        super(williams_percent_directional_index_strategy, self).OnReseted()
        self._prev_wr = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

    def OnStarted2(self, time):
        super(williams_percent_directional_index_strategy, self).OnStarted2(time)
        self._prev_wr = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, wr_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        wr_val = float(wr_value)

        if self._has_prev:
            if self._prev_wr < 35 and wr_val >= 35 and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif self._prev_wr > 65 and wr_val <= 65 and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_wr = wr_val
        self._has_prev = True

    def CreateClone(self):
        return williams_percent_directional_index_strategy()
