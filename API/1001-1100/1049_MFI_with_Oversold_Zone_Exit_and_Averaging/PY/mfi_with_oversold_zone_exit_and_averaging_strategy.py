import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class mfi_with_oversold_zone_exit_and_averaging_strategy(Strategy):
    """
    MFI strategy with oversold zone exit and averaging.
    After MFI has been below MfiOversoldLevel, the first candle on which it climbs back above the level places a limit buy
    LongEntryPercentage percent below the close. An order still unfilled after CancelAfterBars candles is cancelled.
    Positions are closed by the percent take profit and stop loss of StartProtection.
    """

    def __init__(self):
        super(mfi_with_oversold_zone_exit_and_averaging_strategy, self).__init__()
        self._mfi_period = self.Param("MfiPeriod", 14).SetGreaterThanZero().SetDisplay("MFI Period", "Period for the MFI indicator", "Indicators")
        self._mfi_oversold_level = self.Param("MfiOversoldLevel", 20.0).SetDisplay("MFI Oversold", "Oversold level for MFI", "Indicators")
        self._long_entry_percentage = self.Param("LongEntryPercentage", 0.1).SetNotNegative().SetDisplay("Entry %", "Percent below close for the limit entry", "Trading")
        self._stop_loss_percentage = self.Param("StopLossPercentage", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk")
        self._exit_gain_percentage = self.Param("ExitGainPercentage", 1.0).SetNotNegative().SetDisplay("Take Profit %", "Take-profit percentage", "Risk")
        self._cancel_after_bars = self.Param("CancelAfterBars", 5).SetGreaterThanZero().SetDisplay("Cancel After Bars", "Bars before an unfilled limit order is cancelled", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._entry_order = None
        self._bars_since_order = 0
        self._in_oversold_zone = False

    def OnReseted(self):
        super(mfi_with_oversold_zone_exit_and_averaging_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(mfi_with_oversold_zone_exit_and_averaging_strategy, self).OnStarted2(time)

        self._reset_state()

        mfi = MoneyFlowIndex()
        mfi.Length = self._mfi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(mfi, self._process_candle).Start()

        tp = self._exit_gain_percentage.Value
        sl = self._stop_loss_percentage.Value
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
                self.DrawIndicator(oscillators, mfi)

    def _process_candle(self, candle, mfi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._entry_order is not None:
            state = self._entry_order.State
            if state == OrderStates.Active or state == OrderStates.Pending:
                self._bars_since_order += 1
                if self._bars_since_order >= self._cancel_after_bars.Value:
                    if state == OrderStates.Active:
                        self.CancelOrder(self._entry_order)
                    self._entry_order = None
            else:
                self._entry_order = None

        level = Decimal(self._mfi_oversold_level.Value)
        crossed_up = False

        if mfi_value < level:
            self._in_oversold_zone = True
        elif self._in_oversold_zone and mfi_value > level:
            self._in_oversold_zone = False
            crossed_up = True

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Every new signal adds another limit buy, so the position averages down while orders keep filling.
        if crossed_up and self._entry_order is None:
            price = candle.ClosePrice * (Decimal(1) - Decimal(self._long_entry_percentage.Value) / Decimal(100))
            step = self.Security.PriceStep if self.Security is not None else None
            if step is not None and step > 0:
                price = Math.Floor(price / step) * step

            self._entry_order = self.BuyLimit(price, self.Volume)
            self._bars_since_order = 0

    def CreateClone(self):
        return mfi_with_oversold_zone_exit_and_averaging_strategy()
