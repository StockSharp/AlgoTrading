import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class supertrend_rsi_strategy(Strategy):
    """
    Supertrend RSI strategy.
    A close above the Supertrend line with RSI below RsiOversold goes long and a close below it with RSI above RsiOverbought goes short,
    reversing an opposite position. The Supertrend line is the trailing stop: a long closes when Supertrend flips down and a short
    when it flips up.
    """

    def __init__(self):
        super(supertrend_rsi_strategy, self).__init__()
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetGreaterThanZero().SetDisplay("Supertrend Period", "ATR period of Supertrend", "Supertrend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Supertrend")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 40.0).SetDisplay("RSI Oversold", "RSI level for longs", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 60.0).SetDisplay("RSI Overbought", "RSI level for shorts", "RSI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(supertrend_rsi_strategy, self).OnStarted2(time)

        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(supertrend, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, supertrend_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not supertrend_value.IsFormed or not rsi_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        line = supertrend_value.Value
        is_up_trend = supertrend_value.IsUpTrend
        rsi = rsi_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if close > line and rsi < Decimal(self._rsi_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < line and rsi > Decimal(self._rsi_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and not is_up_trend:
            self.SellMarket(self.Position)
        elif self.Position < 0 and is_up_trend:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return supertrend_rsi_strategy()
