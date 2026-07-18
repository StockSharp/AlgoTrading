import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class casino111_strategy(Strategy):
    """
    Casino111: RSI overbought/oversold reversal with ATR stops.
    """

    def __init__(self):
        super(casino111_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "Trend filter.", "Indicators")

        self._prev_rsi = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @rsi_length.setter
    def rsi_length(self, value):
        self._rsi_length.Value = value

    @property
    def atr_length(self):
        return self._atr_length.Value

    @atr_length.setter
    def atr_length(self, value):
        self._atr_length.Value = value

    @property
    def ema_length(self):
        return self._ema_length.Value

    @ema_length.setter
    def ema_length(self, value):
        self._ema_length.Value = value

    def OnReseted(self):
        super(casino111_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(casino111_strategy, self).OnStarted2(time)

        self._prev_rsi = 0.0
        self._entry_price = 0.0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        atr = AverageTrueRange()
        atr.Length = self.atr_length
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, atr, ema, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_val, atr_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_rsi == 0.0 or atr_val <= 0:
            self._prev_rsi = rsi_val
            return

        close = candle.ClosePrice

        if self.Position > 0:
            if (close >= self._entry_price + atr_val * 2.5
                    or close <= self._entry_price - atr_val * 1.5
                    or rsi_val > 70):
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if (close <= self._entry_price - atr_val * 2.5
                    or close >= self._entry_price + atr_val * 1.5
                    or rsi_val < 30):
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if rsi_val > 50 and self._prev_rsi <= 50 and close > ema_val:
                self._entry_price = float(close)
                self.BuyMarket()
            elif rsi_val < 50 and self._prev_rsi >= 50 and close < ema_val:
                self._entry_price = float(close)
                self.SellMarket()

        self._prev_rsi = rsi_val

    def CreateClone(self):
        return casino111_strategy()
