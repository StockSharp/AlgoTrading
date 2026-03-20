import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class five_min_rsi_qualified_strategy(Strategy):
    """5-minute RSI qualified strategy.
    Counts consecutive candles in RSI extreme zones.
    Buys after sustained oversold, sells after sustained overbought."""

    def __init__(self):
        super(five_min_rsi_qualified_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._qualification_length = self.Param("QualificationLength", 3) \
            .SetDisplay("Qual Length", "Consecutive candles in extreme zone", "Indicators")
        self._upper_threshold = self.Param("UpperThreshold", 65.0) \
            .SetDisplay("Upper", "RSI overbought threshold", "Indicators")
        self._lower_threshold = self.Param("LowerThreshold", 35.0) \
            .SetDisplay("Lower", "RSI oversold threshold", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._overbought_count = 0
        self._oversold_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def QualificationLength(self):
        return self._qualification_length.Value

    @property
    def UpperThreshold(self):
        return self._upper_threshold.Value

    @property
    def LowerThreshold(self):
        return self._lower_threshold.Value

    def OnReseted(self):
        super(five_min_rsi_qualified_strategy, self).OnReseted()
        self._overbought_count = 0
        self._oversold_count = 0

    def OnStarted(self, time):
        super(five_min_rsi_qualified_strategy, self).OnStarted(time)

        self._overbought_count = 0
        self._oversold_count = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        upper = float(self.UpperThreshold)
        lower = float(self.LowerThreshold)
        qual = int(self.QualificationLength)

        # Track consecutive overbought candles
        if rsi_val >= upper:
            self._overbought_count += 1
        else:
            self._overbought_count = 0

        # Track consecutive oversold candles
        if rsi_val <= lower:
            self._oversold_count += 1
        else:
            self._oversold_count = 0

        # After qualified oversold period, buy (contrarian)
        if self._oversold_count >= qual and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._oversold_count = 0
        # After qualified overbought period, sell (contrarian)
        elif self._overbought_count >= qual and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._overbought_count = 0

    def CreateClone(self):
        return five_min_rsi_qualified_strategy()
