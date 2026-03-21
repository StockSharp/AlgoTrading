import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, RateOfChange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class long_and_short_with_multi_indicators_strategy(Strategy):
    """
    Long/Short with RSI, ROC and EMA trend filter.
    Long when price above MA + RSI not overbought + positive momentum.
    """

    def __init__(self):
        super(long_and_short_with_multi_indicators_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "Length of RSI", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "Overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._roc_length = self.Param("RocLength", 10) \
            .SetDisplay("ROC Length", "Length of ROC", "Indicators")
        self._ma_length = self.Param("MaLength", 20) \
            .SetDisplay("MA Length", "Length of MA", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 60) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(long_and_short_with_multi_indicators_strategy, self).OnReseted()
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(long_and_short_with_multi_indicators_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        roc = RateOfChange()
        roc.Length = self._roc_length.Value
        ma = ExponentialMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, roc, ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val, roc_val, ma_val):
        if candle.State != CandleStates.Finished:
            return

        self._bars_since_signal += 1

        if self._bars_since_signal < self._cooldown_bars.Value:
            return

        close = float(candle.ClosePrice)
        rsi = float(rsi_val)
        roc = float(roc_val)
        ma = float(ma_val)

        long_signal = close > ma and rsi < self._rsi_overbought.Value and roc > 0
        short_signal = close < ma and rsi > self._rsi_oversold.Value and roc < 0

        if long_signal and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_signal = 0
        elif short_signal and self.Position >= 0:
            self.SellMarket()
            self._bars_since_signal = 0

    def CreateClone(self):
        return long_and_short_with_multi_indicators_strategy()
