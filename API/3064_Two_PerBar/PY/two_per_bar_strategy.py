import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class two_per_bar_strategy(Strategy):
    """Hedged pair strategy: opens buy+sell on each bar with TP management and volume multiplier."""
    def __init__(self):
        super(two_per_bar_strategy, self).__init__()
        self._tp_points = self.Param("TakeProfitPoints", 50).SetNotNegative().SetDisplay("Take Profit", "TP in points", "Risk")
        self._vol_mult = self.Param("VolumeMultiplier", 2).SetGreaterThanZero().SetDisplay("Volume Multiplier", "Multiplier after cycle", "Trading")
        self._max_vol = self.Param("MaxVolume", 10).SetGreaterThanZero().SetDisplay("Max Volume", "Upper limit for lot size", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(two_per_bar_strategy, self).OnReseted()
        self._legs = []
        self._last_cycle_vol = 1.0
        self._bar_count = 0

    def OnStarted(self, time):
        super(two_per_bar_strategy, self).OnStarted(time)
        self._legs = []
        self._last_cycle_vol = 1.0
        self._bar_count = 0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bar_count += 1
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        tp_pts = self._tp_points.Value

        # Check TP hits
        if tp_pts > 0:
            i = len(self._legs) - 1
            while i >= 0:
                leg = self._legs[i]
                if leg['is_long'] and leg['tp'] is not None and high >= leg['tp']:
                    self.SellMarket()
                    self._legs.pop(i)
                elif not leg['is_long'] and leg['tp'] is not None and low <= leg['tp']:
                    self.BuyMarket()
                    self._legs.pop(i)
                i -= 1

        # Close remaining legs
        had_legs = len(self._legs) > 0
        max_vol = 0.0
        for leg in self._legs:
            if leg['vol'] > max_vol:
                max_vol = leg['vol']

        if len(self._legs) > 0:
            for leg in reversed(self._legs):
                if leg['is_long']:
                    self.SellMarket()
                else:
                    self.BuyMarket()
            self._legs = []

        # Calculate next volume
        if had_legs:
            next_vol = max_vol * self._vol_mult.Value
        else:
            next_vol = 1.0

        max_v = self._max_vol.Value
        if max_v > 0 and next_vol > max_v:
            next_vol = 1.0

        self._last_cycle_vol = next_vol

        # Skip first few bars to gather data
        if self._bar_count < 3:
            return

        # Open hedged pair
        tp_offset = tp_pts if tp_pts > 0 else 0
        self.BuyMarket()
        self._legs.append({'is_long': True, 'vol': next_vol, 'entry': close, 'tp': close + tp_offset if tp_offset > 0 else None})
        self.SellMarket()
        self._legs.append({'is_long': False, 'vol': next_vol, 'entry': close, 'tp': close - tp_offset if tp_offset > 0 else None})

    def CreateClone(self):
        return two_per_bar_strategy()
