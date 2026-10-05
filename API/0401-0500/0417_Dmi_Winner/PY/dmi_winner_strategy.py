import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DirectionalIndex, SmoothedMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class dmi_winner_strategy(Strategy):
    """
    DMI Winner strategy.
    Goes long when +DI crosses above -DI and short when -DI crosses above +DI, both only while ADX is above KeyLevel
    and, when UseMA is set, price is on the trade side of the moving average. The opposite DI cross closes the position
    (and reverses it when it is a valid entry), and an optional percent stop-loss caps the risk.
    """

    def __init__(self):
        super(dmi_winner_strategy, self).__init__()
        self._di_length = self.Param("DILength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("DI Length", "Directional indicator period", "DMI")
        self._adx_smoothing = self.Param("ADXSmoothing", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI")
        self._key_level = self.Param("KeyLevel", 23.0) \
            .SetDisplay("Key Level", "ADX level a crossover needs to be traded", "DMI")
        self._use_ma = self.Param("UseMA", True) \
            .SetDisplay("Use MA", "Trade only on the trend side of the moving average", "Moving Average")
        self._ma_length = self.Param("MALength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Moving average period", "Moving Average")
        self._use_sl = self.Param("UseSL", False) \
            .SetDisplay("Use Stop Loss", "Enable the percent stop-loss", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from the entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._adx = None
        self._prev_di_plus = None
        self._prev_di_minus = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(dmi_winner_strategy, self).OnReseted()
        self._adx = None
        self._prev_di_plus = None
        self._prev_di_minus = None

    def OnStarted2(self, time):
        super(dmi_winner_strategy, self).OnStarted2(time)

        self._prev_di_plus = None
        self._prev_di_minus = None

        dmi = DirectionalIndex()
        dmi.Length = self._di_length.Value
        # ADX is the DX of the DILength directional lines smoothed over ADXSmoothing bars.
        self._adx = SmoothedMovingAverage()
        self._adx.Length = self._adx_smoothing.Value
        ma = ExponentialMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(dmi, ma, self._process_candle).Start()

        if self._use_sl.Value:
            self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)
            dmi_area = self.CreateChartArea()
            if dmi_area is not None:
                self.DrawIndicator(dmi_area, dmi)

    def _process_candle(self, candle, dmi_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not dmi_value.IsFormed:
            return

        if dmi_value.Plus is None or dmi_value.Minus is None:
            return

        di_plus = float(dmi_value.Plus)
        di_minus = float(dmi_value.Minus)

        di_sum = di_plus + di_minus
        dx = 0.0 if di_sum == 0 else 100.0 * abs(di_plus - di_minus) / di_sum
        adx = float(to_decimal(process_float(self._adx, dx, candle.ServerTime, True)))

        prev_plus = self._prev_di_plus
        prev_minus = self._prev_di_minus
        self._prev_di_plus = di_plus
        self._prev_di_minus = di_minus

        if not self._adx.IsFormed or not ma_value.IsFormed or prev_plus is None or prev_minus is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = float(ma_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)
        key_level = float(self._key_level.Value)
        use_ma = self._use_ma.Value

        cross_up = prev_plus <= prev_minus and di_plus > di_minus
        cross_down = prev_plus >= prev_minus and di_plus < di_minus

        long_signal = cross_up and adx > key_level and (not use_ma or close > ma)
        short_signal = cross_down and adx > key_level and (not use_ma or close < ma)

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and cross_down:
            self.SellMarket(self.Position)
        elif self.Position < 0 and cross_up:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return dmi_winner_strategy()
