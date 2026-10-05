import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class honest_volatility_grid_strategy(Strategy):
    """
    Honest Volatility Grid strategy.
    Keltner levels are built as EMA + level * Multiplier * ATR, with the ATR over the same EmaPeriod. A long opens when the close
    reaches the LEntry1Level band and a short when it reaches the SEntry1Level band; reaching the opposite entry band closes and
    reverses the position. A raw stop closes a long below the -RawStopLevel band and a short above the +RawStopLevel band.
    """

    def __init__(self):
        super(honest_volatility_grid_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 200).SetGreaterThanZero().SetDisplay("EMA Period", "EMA and ATR period of the Keltner channel", "Indicators")
        self._multiplier = self.Param("Multiplier", 1.0).SetGreaterThanZero().SetDisplay("Multiplier", "ATR multiplier of one channel level", "Indicators")
        self._l_entry1_level = self.Param("LEntry1Level", -2.0).SetDisplay("Long Entry Level", "Channel level of the long entry", "Grid")
        self._s_entry1_level = self.Param("SEntry1Level", 2.0).SetDisplay("Short Entry Level", "Channel level of the short entry", "Grid")
        self._raw_stop_level = self.Param("RawStopLevel", 20.0).SetNotNegative().SetDisplay("Raw Stop Level", "Channel level of the raw stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(honest_volatility_grid_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = float(ema_value)
        step = float(atr_value) * float(self._multiplier.Value)
        if step <= 0:
            return

        close = float(candle.ClosePrice)
        long_entry = ema + float(self._l_entry1_level.Value) * step
        short_entry = ema + float(self._s_entry1_level.Value) * step
        raw_stop = float(self._raw_stop_level.Value)

        if raw_stop > 0:
            if self.Position > 0 and close <= ema - raw_stop * step:
                self.SellMarket(self.Position)
                return
            if self.Position < 0 and close >= ema + raw_stop * step:
                self.BuyMarket(-self.Position)
                return

        if close <= long_entry and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close >= short_entry and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return honest_volatility_grid_strategy()
