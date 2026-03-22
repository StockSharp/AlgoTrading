import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class sniper_jaw_strategy(Strategy):
    """Alligator jaw/teeth/lips trend following with SL/TP."""
    def __init__(self):
        super(sniper_jaw_strategy, self).__init__()
        self._jaw_period = self.Param("JawPeriod", 13).SetGreaterThanZero().SetDisplay("Jaw Period", "Jaw SMA length", "Alligator")
        self._teeth_period = self.Param("TeethPeriod", 8).SetGreaterThanZero().SetDisplay("Teeth Period", "Teeth SMA length", "Alligator")
        self._lips_period = self.Param("LipsPeriod", 5).SetGreaterThanZero().SetDisplay("Lips Period", "Lips SMA length", "Alligator")
        self._sl_pips = self.Param("StopLossPips", 20).SetNotNegative().SetDisplay("Stop Loss (pips)", "SL distance", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 50).SetNotNegative().SetDisplay("Take Profit (pips)", "TP distance", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Candle type", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(sniper_jaw_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None

    def OnStarted(self, time):
        super(sniper_jaw_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._pip_size = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._pip_size = float(self.Security.PriceStep)

        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = self._jaw_period.Value
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = self._teeth_period.Value
        self._lips = SmoothedMovingAverage()
        self._lips.Length = self._lips_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0

        jaw_result = self._jaw.Process(DecimalIndicatorValue(self._jaw, median, candle.OpenTime))
        teeth_result = self._teeth.Process(DecimalIndicatorValue(self._teeth, median, candle.OpenTime))
        lips_result = self._lips.Process(DecimalIndicatorValue(self._lips, median, candle.OpenTime))

        if not self._jaw.IsFormed or not self._teeth.IsFormed or not self._lips.IsFormed:
            return

        jaw_val = float(jaw_result)
        teeth_val = float(teeth_result)
        lips_val = float(lips_result)

        # Manage position
        if self.Position > 0:
            if self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
                self._reset()
                return
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset()
                return
        elif self.Position < 0:
            if self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()
                self._reset()
                return
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset()
                return

        is_uptrend = jaw_val < teeth_val and teeth_val < lips_val
        is_downtrend = jaw_val > teeth_val and teeth_val > lips_val
        close = float(candle.ClosePrice)

        if is_uptrend and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = close - self._sl_pips.Value * self._pip_size if self._sl_pips.Value > 0 else None
            self._take_price = close + self._tp_pips.Value * self._pip_size if self._tp_pips.Value > 0 else None
        elif is_downtrend and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._stop_price = close + self._sl_pips.Value * self._pip_size if self._sl_pips.Value > 0 else None
            self._take_price = close - self._tp_pips.Value * self._pip_size if self._tp_pips.Value > 0 else None

    def _reset(self):
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return sniper_jaw_strategy()
