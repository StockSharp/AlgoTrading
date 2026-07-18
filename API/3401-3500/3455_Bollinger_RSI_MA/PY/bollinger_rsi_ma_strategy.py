import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class bollinger_rsi_ma_strategy(Strategy):
    def __init__(self):
        super(bollinger_rsi_ma_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._bb_period = self.Param("BbPeriod", 20)
        self._band_percent = self.Param("BandPercent", 0.01)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

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
    def BbPeriod(self):
        return self._bb_period.Value

    @BbPeriod.setter
    def BbPeriod(self, value):
        self._bb_period.Value = value

    @property
    def BandPercent(self):
        return self._band_percent.Value

    @BandPercent.setter
    def BandPercent(self, value):
        self._band_percent.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(bollinger_rsi_ma_strategy, self).OnReseted()
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted2(self, time):
        super(bollinger_rsi_ma_strategy, self).OnStarted2(time)
        self._candles_since_trade = self.SignalCooldownCandles

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        ma = SimpleMovingAverage()
        ma.Length = self.BbPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, ma, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        close = float(candle.ClosePrice)
        rsi_val = float(rsi_value)
        ma_val = float(ma_value)

        upper = ma_val * (1.0 + self.BandPercent)
        lower = ma_val * (1.0 - self.BandPercent)

        if close < lower and rsi_val < 35 and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif close > upper and rsi_val > 65 and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.SellMarket()
            self._candles_since_trade = 0

    def CreateClone(self):
        return bollinger_rsi_ma_strategy()
