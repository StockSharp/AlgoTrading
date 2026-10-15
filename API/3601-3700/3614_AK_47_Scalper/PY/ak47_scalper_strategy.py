import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class ak47_scalper_strategy(Strategy):
    def __init__(self):
        super(ak47_scalper_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._lookback_period = self.Param("LookbackPeriod", 5)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._atr_stop_multiplier = self.Param("AtrStopMultiplier", 1.5)

        self._highest_high = 0.0
        self._lowest_low = float('inf')
        self._bars_collected = 0
        self._entry_price = None
        self._entry_side = None
        self._stop_distance = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrStopMultiplier(self):
        return self._atr_stop_multiplier.Value

    @AtrStopMultiplier.setter
    def AtrStopMultiplier(self, value):
        self._atr_stop_multiplier.Value = value

    def OnReseted(self):
        super(ak47_scalper_strategy, self).OnReseted()
        self._highest_high = 0.0
        self._lowest_low = float('inf')
        self._bars_collected = 0
        self._entry_price = None
        self._entry_side = None
        self._stop_distance = 0.0

    def OnStarted2(self, time):
        super(ak47_scalper_strategy, self).OnStarted2(time)
        self._highest_high = 0.0
        self._lowest_low = float('inf')
        self._bars_collected = 0
        self._entry_price = None
        self._entry_side = None
        self._stop_distance = 0.0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_value)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        lookback = self.LookbackPeriod

        # Build lookback channel
        if self._bars_collected < lookback:
            if high > self._highest_high:
                self._highest_high = high
            if low < self._lowest_low:
                self._lowest_low = low
            self._bars_collected += 1
            return

        atr_stop_mult = float(self.AtrStopMultiplier)
        self._stop_distance = atr_val * atr_stop_mult

        # Check stop loss / take profit on existing position
        if self._entry_price is not None and self._entry_side is not None:
            if self._entry_side == "buy" and close <= self._entry_price - self._stop_distance:
                self.SellMarket()
                self._entry_price = None
                self._entry_side = None
            elif self._entry_side == "sell" and close >= self._entry_price + self._stop_distance:
                self.BuyMarket()
                self._entry_price = None
                self._entry_side = None
            elif self._entry_side == "buy" and close >= self._entry_price + self._stop_distance * 1.5:
                self.SellMarket()
                self._entry_price = None
                self._entry_side = None
            elif self._entry_side == "sell" and close <= self._entry_price - self._stop_distance * 1.5:
                self.BuyMarket()
                self._entry_price = None
                self._entry_side = None

        # Entry signals: breakout
        if self.Position == 0:
            if close > self._highest_high:
                self.BuyMarket()
                self._entry_price = close
                self._entry_side = "buy"
            elif close < self._lowest_low:
                self.SellMarket()
                self._entry_price = close
                self._entry_side = "sell"

        # Update channel with slow decay
        if high > self._highest_high:
            self._highest_high = high
        else:
            self._highest_high = self._highest_high * 0.999 + high * 0.001

        if low < self._lowest_low:
            self._lowest_low = low
        else:
            self._lowest_low = self._lowest_low * 0.999 + low * 0.001

    def CreateClone(self):
        return ak47_scalper_strategy()
