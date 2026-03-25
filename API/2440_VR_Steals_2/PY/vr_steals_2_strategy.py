import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vr_steals_2_strategy(Strategy):
    """SMA crossover (8/34) with breakeven/SL/TP management and StartProtection."""
    def __init__(self):
        super(vr_steals_2_strategy, self).__init__()
        self._tp = self.Param("TakeProfit", 50).SetDisplay("Take Profit", "TP in steps", "General")
        self._sl = self.Param("StopLoss", 50).SetDisplay("Stop Loss", "SL in steps", "General")
        self._breakeven = self.Param("Breakeven", 20).SetDisplay("Breakeven", "Distance to activate breakeven", "General")
        self._breakeven_offset = self.Param("BreakevenOffset", 9).SetDisplay("Breakeven Offset", "Offset when breakeven triggered", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vr_steals_2_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = 0
        self._be_activated = False
        self._prev_fast = 0
        self._prev_slow = 0
        self._ma_init = False

    def OnStarted(self, time):
        super(vr_steals_2_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._stop_price = 0
        self._be_activated = False
        self._prev_fast = 0
        self._prev_slow = 0
        self._ma_init = False

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = 8
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = 34

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_sma, slow_sma, self.OnProcess).Start()

        self.StartProtection(
            Unit(2000, UnitTypes.Absolute),
            Unit(1000, UnitTypes.Absolute))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        fv = float(fast_val)
        sv = float(slow_val)
        step = 1.0
        tp_val = self._tp.Value
        sl_val = self._sl.Value
        be_val = self._breakeven.Value
        be_off = self._breakeven_offset.Value

        # Manage long
        if self.Position > 0:
            if not self._be_activated and be_val > 0 and price >= self._entry_price + be_val * step:
                self._stop_price = self._entry_price + be_off * step
                self._be_activated = True

            if tp_val > 0 and price >= self._entry_price + tp_val * step:
                self.SellMarket()
                self._entry_price = 0
                self._be_activated = False
                self._prev_fast = fv
                self._prev_slow = sv
                self._ma_init = True
                return

            if self._be_activated:
                stop = self._stop_price
            else:
                stop = self._entry_price - sl_val * step if sl_val > 0 else -999999

            if price <= stop:
                self.SellMarket()
                self._entry_price = 0
                self._be_activated = False
                self._prev_fast = fv
                self._prev_slow = sv
                self._ma_init = True
                return

        # Manage short
        elif self.Position < 0:
            if not self._be_activated and be_val > 0 and price <= self._entry_price - be_val * step:
                self._stop_price = self._entry_price - be_off * step
                self._be_activated = True

            if tp_val > 0 and price <= self._entry_price - tp_val * step:
                self.BuyMarket()
                self._entry_price = 0
                self._be_activated = False
                self._prev_fast = fv
                self._prev_slow = sv
                self._ma_init = True
                return

            if self._be_activated:
                stop = self._stop_price
            else:
                stop = self._entry_price + sl_val * step if sl_val > 0 else 999999

            if price >= stop:
                self.BuyMarket()
                self._entry_price = 0
                self._be_activated = False
                self._prev_fast = fv
                self._prev_slow = sv
                self._ma_init = True
                return

        # Entry on SMA cross
        if self.Position == 0:
            if self._ma_init and self._prev_fast <= self._prev_slow and fv > sv:
                self.BuyMarket()
                self._entry_price = price
                self._be_activated = False
            elif self._ma_init and self._prev_fast >= self._prev_slow and fv < sv:
                self.SellMarket()
                self._entry_price = price
                self._be_activated = False

        self._prev_fast = fv
        self._prev_slow = sv
        self._ma_init = True

    def CreateClone(self):
        return vr_steals_2_strategy()
