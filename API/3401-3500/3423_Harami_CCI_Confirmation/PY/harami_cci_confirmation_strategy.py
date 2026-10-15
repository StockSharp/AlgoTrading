import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class harami_cci_confirmation_strategy(Strategy):
    def __init__(self):
        super(harami_cci_confirmation_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._cci_period = self.Param("CciPeriod", 14)
        self._entry_level = self.Param("EntryLevel", 0.0)
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
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def EntryLevel(self):
        return self._entry_level.Value

    @EntryLevel.setter
    def EntryLevel(self, value):
        self._entry_level.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(harami_cci_confirmation_strategy, self).OnReseted()
        self._candles.clear()
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted2(self, time):
        super(harami_cci_confirmation_strategy, self).OnStarted2(time)
        self._candles.clear()
        self._candles_since_trade = self.SignalCooldownCandles

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, self._process_candle).Start()

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        cci_val = float(cci_value)

        self._candles.append(candle)
        if len(self._candles) > 5:
            self._candles.pop(0)

        if len(self._candles) >= 2:
            curr = self._candles[-1]
            prev = self._candles[-2]

            bullish_harami = (float(prev.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) > float(curr.OpenPrice)
                and float(curr.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) < float(prev.OpenPrice))

            bearish_harami = (float(prev.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) > float(curr.ClosePrice)
                and float(curr.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) < float(prev.ClosePrice))

            if bullish_harami and cci_val < -self.EntryLevel and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif bearish_harami and cci_val > self.EntryLevel and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

    def CreateClone(self):
        return harami_cci_confirmation_strategy()
