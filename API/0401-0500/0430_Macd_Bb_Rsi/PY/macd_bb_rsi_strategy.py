import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

RSI_OVERSOLD = 30.0
RSI_OVERBOUGHT = 70.0


class macd_bb_rsi_strategy(Strategy):
    """
    MACD + Bollinger Bands + RSI strategy.
    Buys a pullback when the MACD line is positive while the close is below the lower Bollinger band and RSI is under 30,
    and sells when the MACD line is negative while the close is above the upper band and RSI is over 70. The opposite
    signal reverses the position.
    """

    def __init__(self):
        super(macd_bb_rsi_strategy, self).__init__()
        self._macd_fast_length = self.Param("MacdFastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "MACD fast EMA period", "MACD")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "MACD slow EMA period", "MACD")
        self._macd_signal_length = self.Param("MacdSignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "MACD signal line period", "MACD")
        self._bb_length = self.Param("BBLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._rsi_length = self.Param("RSILength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        super(macd_bb_rsi_strategy, self).OnStarted2(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast_length.Value
        macd.Macd.LongMa.Length = self._macd_slow_length.Value
        macd.SignalMa.Length = self._macd_signal_length.Value
        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_multiplier.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, bollinger, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, macd_value, bollinger_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed or not bollinger_value.IsFormed or not rsi_value.IsFormed:
            return

        if macd_value.Macd is None:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd_line = float(macd_value.Macd)
        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)
        rsi = float(rsi_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)

        long_signal = macd_line > 0 and close < lower and rsi < RSI_OVERSOLD
        short_signal = macd_line < 0 and close > upper and rsi > RSI_OVERBOUGHT

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return macd_bb_rsi_strategy()
