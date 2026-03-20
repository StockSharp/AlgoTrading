import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class potential_entries_strategy(Strategy):
    def __init__(self):
        super(potential_entries_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

        self._candles = []
        self._candles_since_trade = 6

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(potential_entries_strategy, self).OnReseted()
        self._candles.clear()
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted(self, time):
        super(potential_entries_strategy, self).OnStarted(time)
        self._candles.clear()
        self._candles_since_trade = self.SignalCooldownCandles

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        self._candles.append(candle)
        if len(self._candles) > 5:
            self._candles.pop(0)

        if len(self._candles) >= 2:
            curr = self._candles[-1]
            prev = self._candles[-2]

            bullish = (float(prev.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) > float(curr.OpenPrice)
                and float(curr.ClosePrice) > float(prev.OpenPrice))

            bearish = (float(prev.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) > float(curr.ClosePrice)
                and float(curr.ClosePrice) < float(prev.OpenPrice))

            if bullish and float(rsi_value) < 50 and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif bearish and float(rsi_value) > 50 and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

    def CreateClone(self):
        return potential_entries_strategy()
