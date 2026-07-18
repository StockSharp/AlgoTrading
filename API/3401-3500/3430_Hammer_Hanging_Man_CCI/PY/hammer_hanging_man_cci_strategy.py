import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class hammer_hanging_man_cci_strategy(Strategy):
    def __init__(self):
        super(hammer_hanging_man_cci_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._cci_period = self.Param("CciPeriod", 14)
        self._cci_level = self.Param("CciLevel", 100.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

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
    def CciLevel(self):
        return self._cci_level.Value

    @CciLevel.setter
    def CciLevel(self, value):
        self._cci_level.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(hammer_hanging_man_cci_strategy, self).OnReseted()
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted2(self, time):
        super(hammer_hanging_man_cci_strategy, self).OnStarted2(time)
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

        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        rng = float(candle.HighPrice) - float(candle.LowPrice)
        if rng <= 0 or body <= 0:
            return

        upper_shadow = float(candle.HighPrice) - max(float(candle.OpenPrice), float(candle.ClosePrice))
        lower_shadow = min(float(candle.OpenPrice), float(candle.ClosePrice)) - float(candle.LowPrice)

        is_hammer = lower_shadow > body * 2.5 and upper_shadow < body * 0.5
        is_hanging_man = upper_shadow > body * 2.5 and lower_shadow < body * 0.5

        if is_hammer and cci_val < -self.CciLevel and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif is_hanging_man and cci_val > self.CciLevel and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.SellMarket()
            self._candles_since_trade = 0

    def CreateClone(self):
        return hammer_hanging_man_cci_strategy()
