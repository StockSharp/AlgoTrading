import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, CommodityChannelIndex, ExponentialMovingAverage, AverageTrueRange

class auto_rxdv167_strategy(Strategy):
    def __init__(self):
        super(auto_rxdv167_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._cci_length = self.Param("CciLength", 14) \
            .SetDisplay("CCI Length", "CCI period.", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend filter.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for stops.", "Indicators")

        self._prev_rsi = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def CciLength(self):
        return self._cci_length.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted2(self, time):
        super(auto_rxdv167_strategy, self).OnStarted2(time)

        self._prev_rsi = 0.0
        self._entry_price = 0.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciLength
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._cci, self._ema, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_val, cci_val, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_val)
        cv = float(cci_val)
        ev = float(ema_val)
        av = float(atr_val)

        if self._prev_rsi == 0 or av <= 0:
            self._prev_rsi = rv
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if close <= self._entry_price - av * 2.0 or close >= self._entry_price + av * 3.0 or (rv > 70 and cv > 100):
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close >= self._entry_price + av * 2.0 or close <= self._entry_price - av * 3.0 or (rv < 30 and cv < -100):
                self.BuyMarket()
                self._entry_price = 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return

        if self.Position == 0:
            if rv > 50 and cv > 0 and close > ev and self._prev_rsi <= 50:
                self._entry_price = close
                self.BuyMarket()
            elif rv < 50 and cv < 0 and close < ev and self._prev_rsi >= 50:
                self._entry_price = close
                self.SellMarket()

        self._prev_rsi = rv

    def OnReseted(self):
        super(auto_rxdv167_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return auto_rxdv167_strategy()
