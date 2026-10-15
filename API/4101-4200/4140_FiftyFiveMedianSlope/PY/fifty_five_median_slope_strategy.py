import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class fifty_five_median_slope_strategy(Strategy):
    """
    Fifty Five Median Slope: EMA slope direction with ATR stops.
    Enters when EMA slope changes sign, exits at ATR-based levels or slope reversal.
    """

    def __init__(self):
        super(fifty_five_median_slope_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ema_length = self.Param("EmaLength", 55) \
            .SetDisplay("EMA Length", "Moving average period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._slope_shift = self.Param("SlopeShift", 13) \
            .SetDisplay("Slope Shift", "Bars between slope comparison", "Indicators")

        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._bar_count = 0
        self._ema_history = [0.0] * 20

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fifty_five_median_slope_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._bar_count = 0
        self._ema_history = [0.0] * 20

    def OnStarted2(self, time):
        super(fifty_five_median_slope_strategy, self).OnStarted2(time)

        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._bar_count = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_val)
        atr_val = float(atr_val)
        close = float(candle.ClosePrice)

        length = min(self._slope_shift.Value + 1, len(self._ema_history))
        idx = self._bar_count % length
        self._ema_history[idx] = ema_val
        self._bar_count += 1

        if self._bar_count < length or atr_val <= 0:
            return

        shift_idx = (self._bar_count - self._slope_shift.Value) % length
        if shift_idx < 0:
            shift_idx += length
        shifted_ema = self._ema_history[shift_idx]

        if self.Position > 0:
            if (close >= self._entry_price + atr_val * 3.0 or
                    close <= self._entry_price - atr_val * 1.5 or
                    ema_val < shifted_ema):
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if (close <= self._entry_price - atr_val * 3.0 or
                    close >= self._entry_price + atr_val * 1.5 or
                    ema_val > shifted_ema):
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if ema_val > shifted_ema and self._prev_ema <= shifted_ema:
                self._entry_price = close
                self.BuyMarket()
            elif ema_val < shifted_ema and self._prev_ema >= shifted_ema:
                self._entry_price = close
                self.SellMarket()

        self._prev_ema = ema_val

    def CreateClone(self):
        return fifty_five_median_slope_strategy()
