import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, DirectionalIndex, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class macd_dmi_strategy(Strategy):
    """
    MACD + DMI strategy.
    The MACD line is the difference of Ma1Length and Ma2Length EMAs. A long opens when it crosses above its signal line
    while +DI is above -DI and ADX is above KeyLevel; a short opens on the mirrored setup. The reverse signal flips the
    position, and percent stop-loss and take-profit protection limit each trade.
    """

    def __init__(self):
        super(macd_dmi_strategy, self).__init__()
        self._ma1_length = self.Param("Ma1Length", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("MA1 Length", "Fast EMA period of MACD", "MACD")
        self._ma2_length = self.Param("Ma2Length", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA2 Length", "Slow EMA period of MACD", "MACD")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Length", "MACD signal line period", "MACD")
        self._dmi_length = self.Param("DmiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("DMI Length", "Directional movement period", "DMI")
        self._adx_smoothing = self.Param("AdxSmoothing", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI")
        self._key_level = self.Param("KeyLevel", 20.0) \
            .SetDisplay("Key Level", "ADX level required for entries", "DMI")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit %", "Take-profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._adx = None
        self._prev_macd = None
        self._prev_signal = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(macd_dmi_strategy, self).OnReseted()
        self._adx = None
        self._prev_macd = None
        self._prev_signal = None

    def OnStarted2(self, time):
        super(macd_dmi_strategy, self).OnStarted2(time)

        self._prev_macd = None
        self._prev_signal = None

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._ma1_length.Value
        macd.Macd.LongMa.Length = self._ma2_length.Value
        macd.SignalMa.Length = self._signal_length.Value
        dmi = DirectionalIndex()
        dmi.Length = self._dmi_length.Value
        # ADX is the DX of the DmiLength directional lines smoothed over AdxSmoothing bars.
        self._adx = SmoothedMovingAverage()
        self._adx.Length = self._adx_smoothing.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, dmi, self._process_candle).Start()

        tp = float(self._take_profit_percent.Value)
        sl = float(self._stop_loss_percent.Value)
        self.StartProtection(
            Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit(),
            Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)
                self.DrawIndicator(oscillators, dmi)

    def _process_candle(self, candle, macd_value, dmi_value):
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

        if not macd_value.IsFormed:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = float(macd_value.Macd)
        signal = float(macd_value.Signal)

        prev_macd = self._prev_macd
        prev_signal = self._prev_signal
        self._prev_macd = macd
        self._prev_signal = signal

        if not self._adx.IsFormed or prev_macd is None or prev_signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        key_level = float(self._key_level.Value)
        cross_up = prev_macd <= prev_signal and macd > signal
        cross_down = prev_macd >= prev_signal and macd < signal

        if cross_up and di_plus > di_minus and adx > key_level and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and di_minus > di_plus and adx > key_level and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return macd_dmi_strategy()
