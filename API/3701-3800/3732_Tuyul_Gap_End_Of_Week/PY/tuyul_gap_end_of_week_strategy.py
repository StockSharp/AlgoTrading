import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class tuyul_gap_end_of_week_strategy(Strategy):
    """Breakout above highest / below lowest with stop loss management."""
    def __init__(self):
        super(tuyul_gap_end_of_week_strategy, self).__init__()
        self._sl_points = self.Param("StopLossPoints", 60).SetDisplay("Stop Loss", "SL in points", "Risk")
        self._lookback = self.Param("LookbackBars", 12).SetDisplay("Lookback", "Bars for high/low", "Setup")
        self._secure_profit = self.Param("SecureProfitTarget", 5.0).SetDisplay("Secure Profit", "Profit target for exit", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Timeframe", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(tuyul_gap_end_of_week_strategy, self).OnReseted()
        self._entry_price = 0
        self._virtual_stop = None
        self._prev_highest = 0
        self._prev_lowest = 0
        self._tick_size = 0

    def OnStarted2(self, time):
        super(tuyul_gap_end_of_week_strategy, self).OnStarted2(time)
        self._entry_price = 0
        self._virtual_stop = None
        self._prev_highest = 0
        self._prev_lowest = 0

        self._tick_size = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            self._tick_size = float(self.Security.PriceStep)

        hi = Highest()
        hi.Length = max(2, self._lookback.Value)
        lo = Lowest()
        lo.Length = max(2, self._lookback.Value)

        self._hi = hi
        self._lo = lo

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(hi, lo, self.OnProcess).Start()

    def OnProcess(self, candle, hi_val, lo_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._hi.IsFormed or not self._lo.IsFormed:
            return

        highest = float(hi_val)
        lowest = float(lo_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        # Check virtual stop
        if self.Position > 0 and self._virtual_stop is not None and low <= self._virtual_stop:
            self.SellMarket(abs(float(self.Position)))
            self._virtual_stop = None
            self._entry_price = 0
            return
        if self.Position < 0 and self._virtual_stop is not None and high >= self._virtual_stop:
            self.BuyMarket(abs(float(self.Position)))
            self._virtual_stop = None
            self._entry_price = 0
            return

        # Close on profit
        if self.Position != 0 and self._secure_profit.Value > 0 and float(self.PnL) >= self._secure_profit.Value:
            if self.Position > 0:
                self.SellMarket(abs(float(self.Position)))
            else:
                self.BuyMarket(abs(float(self.Position)))
            self._virtual_stop = None
            self._entry_price = 0
            return

        # Entry on breakout
        if self.Position == 0 and self._prev_highest > 0 and self._prev_lowest > 0:
            stop_dist = self._get_stop_distance()
            if close > self._prev_highest:
                self.BuyMarket()
                self._entry_price = close
                if stop_dist > 0:
                    self._virtual_stop = close - stop_dist
            elif close < self._prev_lowest:
                self.SellMarket()
                self._entry_price = close
                if stop_dist > 0:
                    self._virtual_stop = close + stop_dist

        self._prev_highest = highest
        self._prev_lowest = lowest

    def _get_stop_distance(self):
        tick = self._tick_size
        if tick <= 0:
            if self.Security is not None and self.Security.PriceStep is not None:
                tick = float(self.Security.PriceStep)
        if tick <= 0:
            return 0
        if self._sl_points.Value > 0:
            return self._sl_points.Value * tick
        return 0

    def CreateClone(self):
        return tuyul_gap_end_of_week_strategy()
