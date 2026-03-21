import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class plc_strategy(Strategy):
    def __init__(self):
        super(plc_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._shift_pips = self.Param("ShiftPips", 15) \
            .SetDisplay("Shift Pips", "Offset added to candle high/low for breakout", "Trading")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for volatility measurement", "Indicators")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def ShiftPips(self):
        return self._shift_pips.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    def OnReseted(self):
        super(plc_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(plc_strategy, self).OnStarted(time)

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._initialized = False

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(atr, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_value)

        if not self._initialized:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._initialized = True
            return

        shift = av * self.ShiftPips / 100.0
        buy_level = self._prev_high + shift
        sell_level = self._prev_low - shift

        close = float(candle.ClosePrice)

        if close > buy_level and self.Position <= 0:
            self.BuyMarket()
        elif close < sell_level and self.Position >= 0:
            self.SellMarket()

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        return plc_strategy()
