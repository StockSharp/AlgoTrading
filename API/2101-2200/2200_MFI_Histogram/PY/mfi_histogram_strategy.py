import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class mfi_histogram_strategy(Strategy):
    """MFI threshold crossings; risk distances are instrument price steps, not raw prices."""

    def __init__(self):
        super(mfi_histogram_strategy, self).__init__()
        self._mfi_period = self.Param("MfiPeriod", 14).SetDisplay("MFI Period", "Period for Money Flow Index", "MFI")
        self._high_level = self.Param("HighLevel", 60.0).SetDisplay("High Level", "Upper MFI crossing threshold", "MFI")
        self._low_level = self.Param("LowLevel", 40.0).SetDisplay("Low Level", "Lower MFI crossing threshold", "MFI")
        # As in C#, keep Unit parameters. The numeric value is the count of price steps.
        self._stop_loss = self.Param("StopLoss", Unit(1000, UnitTypes.Absolute)).SetDisplay("Stop Loss", "Distance in instrument price steps", "Risk")
        self._take_profit = self.Param("TakeProfit", Unit(2000, UnitTypes.Absolute)).SetDisplay("Take Profit", "Distance in instrument price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe of candles", "General")
        self._prev_mfi = None
        self._pending_entry = None

    @property
    def mfi_period(self): return self._mfi_period.Value
    @property
    def high_level(self): return self._high_level.Value
    @property
    def low_level(self): return self._low_level.Value
    @property
    def stop_loss(self): return self._stop_loss.Value
    @property
    def take_profit(self): return self._take_profit.Value
    @property
    def candle_type(self): return self._candle_type.Value

    def OnReseted(self):
        super(mfi_histogram_strategy, self).OnReseted()
        self._prev_mfi = None
        self._pending_entry = None

    def _absolute_price_distance(self, count, step, name):
        if count is None or count.Type != UnitTypes.Absolute or float(count.Value) <= 0:
            raise ValueError(name + " must be a positive number of price steps")
        return Unit(float(count.Value) * float(step), UnitTypes.Absolute)

    def OnStarted2(self, time):
        super(mfi_histogram_strategy, self).OnStarted2(time)
        self._prev_mfi = None
        self._pending_entry = None
        step = self.Security.PriceStep if self.Security is not None else None
        if step is None or float(step) <= 0:
            raise ValueError("MFI Histogram requires a positive Security.PriceStep")
        self.StartProtection(
            takeProfit=self._absolute_price_distance(self.take_profit, step, "TakeProfit"),
            stopLoss=self._absolute_price_distance(self.stop_loss, step, "StopLoss"),
            useMarketOrders=True, isLocalStop=True)

        mfi = MoneyFlowIndex()
        mfi.Length = self.mfi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(mfi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            indicator_area = self.CreateChartArea()
            if indicator_area is not None:
                self.DrawIndicator(indicator_area, mfi)

    def process_candle(self, candle, mfi_value):
        if candle.State != CandleStates.Finished:
            return
        mfi = float(mfi_value)
        previous = self._prev_mfi
        self._prev_mfi = mfi
        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_entry is not None and self._pending_entry.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if previous <= float(self.high_level) and mfi > float(self.high_level) and self.Position <= 0:
            self._pending_entry = self.BuyMarket(self.Volume + abs(self.Position))
        elif previous >= float(self.low_level) and mfi < float(self.low_level) and self.Position >= 0:
            self._pending_entry = self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return mfi_histogram_strategy()
