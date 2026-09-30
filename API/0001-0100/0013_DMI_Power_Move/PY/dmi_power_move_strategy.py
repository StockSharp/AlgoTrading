import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class dmi_power_move_strategy(Strategy):
    """Trades strong, rising ADX with a DI spread; exits on weakening or an ATR stop."""

    def __init__(self):
        super(dmi_power_move_strategy, self).__init__()
        self._dmi_period = self.Param("DmiPeriod", 14) \
            .SetDisplay("DMI Period", "Period for DMI and ATR", "Indicators")
        self._di_difference_threshold = self.Param("DiDifferenceThreshold", 5.0) \
            .SetDisplay("DI Difference Threshold", "Minimum directional DI spread", "Trading")
        self._adx_threshold = self.Param("AdxThreshold", 30.0) \
            .SetDisplay("ADX Threshold", "Minimum ADX for entry", "Trading")
        self._adx_exit_threshold = self.Param("AdxExitThreshold", 25.0) \
            .SetDisplay("ADX Exit Threshold", "Close when ADX falls below this value", "Trading")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Stop distance in entry ATR multiples", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._previous_adx = None
        self._stop_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(dmi_power_move_strategy, self).OnReseted()
        self._previous_adx = None
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(dmi_power_move_strategy, self).OnStarted2(time)
        dmi = AverageDirectionalIndex()
        dmi.Length = int(self._dmi_period.Value)
        atr = AverageTrueRange()
        atr.Length = int(self._dmi_period.Value)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(dmi, atr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, dmi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, dmi, atr_value):
        if candle.State != CandleStates.Finished or not dmi.IsFormed or not atr_value.IsFormed:
            return
        if dmi.MovingAverage is None or dmi.Dx.Plus is None or dmi.Dx.Minus is None:
            return

        adx = float(dmi.MovingAverage)
        spread = float(dmi.Dx.Plus) - float(dmi.Dx.Minus)
        rising = self._previous_adx is not None and adx > self._previous_adx
        self._previous_adx = adx
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        price = float(candle.ClosePrice)
        threshold = float(self._di_difference_threshold.Value)
        stopped = self._stop_price != 0.0 and (price <= self._stop_price if self.Position > 0 else price >= self._stop_price)
        if self.Position > 0 and (stopped or adx < float(self._adx_exit_threshold.Value) or spread <= threshold):
            self.SellMarket(self.Position)
            self._stop_price = 0.0
            return
        if self.Position < 0 and (stopped or adx < float(self._adx_exit_threshold.Value) or spread >= -threshold):
            self.BuyMarket(-self.Position)
            self._stop_price = 0.0
            return

        atr = float(atr_value.GetValue[Decimal](None))
        if self.Position != 0 or not rising or adx <= float(self._adx_threshold.Value) or atr <= 0:
            return
        offset = float(self._atr_multiplier.Value) * atr
        if spread > threshold:
            self.BuyMarket()
            self._stop_price = price - offset
        elif spread < -threshold:
            self.SellMarket()
            self._stop_price = price + offset

    def CreateClone(self):
        return dmi_power_move_strategy()
