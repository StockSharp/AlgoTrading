import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy

class mfi_with_oversold_zone_exit_and_averaging_strategy(Strategy):
    """
    MFI oversold zone exit: buy market when MFI exits oversold, with percent SL/TP.
    Simplified from C# (no limit orders, uses market orders and StartProtection).
    """

    def __init__(self):
        super(mfi_with_oversold_zone_exit_and_averaging_strategy, self).__init__()
        self._mfi_period = self.Param("MfiPeriod", 14).SetDisplay("MFI Period", "MFI period", "Indicator")
        self._mfi_oversold = self.Param("MfiOversoldLevel", 20.0).SetDisplay("MFI Oversold", "Oversold level", "Signal")
        self._stop_loss_pct = self.Param("StopLossPercentage", 1.0).SetDisplay("SL %", "Stop loss pct", "Risk")
        self._take_profit_pct = self.Param("ExitGainPercentage", 1.0).SetDisplay("TP %", "Take profit pct", "Risk")
        self._cooldown_bars = self.Param("SignalCooldownBars", 20).SetDisplay("Cooldown", "Min bars between entries", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._in_oversold = False
        self._bars_from_signal = 20

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mfi_with_oversold_zone_exit_and_averaging_strategy, self).OnReseted()
        self._in_oversold = False
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(mfi_with_oversold_zone_exit_and_averaging_strategy, self).OnStarted(time)
        mfi = MoneyFlowIndex()
        mfi.Length = self._mfi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(mfi, self._process_candle).Start()
        sl_pct = float(self._stop_loss_pct.Value)
        tp_pct = float(self._take_profit_pct.Value)
        self.StartProtection(
            Unit(tp_pct, UnitTypes.Percent),
            Unit(sl_pct, UnitTypes.Percent)
        )
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, mfi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, mfi_val):
        if candle.State != CandleStates.Finished:
            return
        mfi = float(mfi_val)
        self._bars_from_signal += 1
        oversold = float(self._mfi_oversold.Value)
        if mfi < oversold:
            self._in_oversold = True
        elif self._in_oversold and mfi > oversold and self.Position == 0 and self._bars_from_signal >= self._cooldown_bars.Value:
            self._in_oversold = False
            self.BuyMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return mfi_with_oversold_zone_exit_and_averaging_strategy()
