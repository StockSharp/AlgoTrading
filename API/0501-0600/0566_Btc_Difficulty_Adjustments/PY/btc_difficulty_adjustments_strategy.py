import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

import math
from collections import deque
from System import TimeSpan, DateTime, DateTimeKind, Decimal, Nullable
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates, Unit, UnitTypes
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

from System.IO import File
from System.Globalization import CultureInfo, DateTimeStyles

class btc_difficulty_adjustments_strategy(Strategy):
    def __init__(self):
        super(btc_difficulty_adjustments_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay(
            "CandleType", "Finished decision candle interval", "Parameters")
        self._threshold_mode = self.Param("ThresholdMode", False).SetDisplay(
            "ThresholdMode", "Use contrarian thresholded adjustments", "Parameters")
        self._threshold = self.Param("Threshold", 10.0).SetGreaterThanZero().SetDisplay(
            "Threshold", "Minimum absolute difficulty change in percent", "Parameters")
        self._difficulty_history_file = self.Param("DifficultyHistoryFile", "").SetDisplay(
            "DifficultyHistoryFile", "Required Time,Difficulty CSV with UTC publication timestamps", "Parameters")
        self._reset_state()
        self.OrderRegistering += self._remember_order

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ThresholdMode(self):
        return self._threshold_mode.Value

    @ThresholdMode.setter
    def ThresholdMode(self, value):
        self._threshold_mode.Value = value

    @property
    def Threshold(self):
        return self._threshold.Value

    @Threshold.setter
    def Threshold(self, value):
        self._threshold.Value = value

    @property
    def DifficultyHistoryFile(self):
        return self._difficulty_history_file.Value

    @DifficultyHistoryFile.setter
    def DifficultyHistoryFile(self, value):
        self._difficulty_history_file.Value = value

    def _reset_state(self):
        self._observations = []
        self._cursor = 0
        self._previous = None
        self._last_time = DateTime.MinValue
        self._pending = None

    def _remember_order(self, order):
        self._pending = order

    def OnReseted(self):
        super(btc_difficulty_adjustments_strategy, self).OnReseted()
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        if not self.DifficultyHistoryFile or not File.Exists(self.DifficultyHistoryFile):
            raise ValueError("DifficultyHistoryFile must identify an existing Time,Difficulty CSV.")
        lines = list(File.ReadAllLines(self.DifficultyHistoryFile))
        if len(lines) < 3 or lines[0].strip() != "Time,Difficulty":
            raise ValueError("DifficultyHistoryFile requires Time,Difficulty and at least two observations.")
        self._observations = []
        for line in lines[1:]:
            if not line.strip():
                continue
            cells = line.split(",")
            if len(cells) != 2 or not cells[0].strip().upper().endswith("Z"):
                raise ValueError("DifficultyHistoryFile timestamps must be ISO UTC values ending in Z.")
            stamp = DateTime.Parse(cells[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            value = Decimal.Parse(cells[1], CultureInfo.InvariantCulture)
            if value <= 0 or (self._observations and stamp <= self._observations[-1][0]):
                raise ValueError("DifficultyHistoryFile requires positive values and strictly increasing timestamps.")
            self._observations.append((stamp, value))
        if len(self._observations) < 2:
            raise ValueError("DifficultyHistoryFile requires at least two observations.")
        super(btc_difficulty_adjustments_strategy, self).OnStarted2(time)
        candles = self.SubscribeCandles(self.CandleType)
        candles.Bind(self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def _process(self, candle):
        if candle.State != CandleStates.Finished or candle.OpenTime <= self._last_time:
            return
        self._last_time = candle.OpenTime
        side = None
        while self._cursor < len(self._observations) and self._observations[self._cursor][0] <= candle.OpenTime:
            value = self._observations[self._cursor][1]
            self._cursor += 1
            if self._previous is not None and value != self._previous:
                change = (value / self._previous - Decimal(1)) * Decimal(100)
                if not self.ThresholdMode or abs(change) >= self.Threshold:
                    side = (Sides.Buy if change < 0 else Sides.Sell) if self.ThresholdMode else (Sides.Buy if change > 0 else Sides.Sell)
                else:
                    side = None
            self._previous = value
        if side is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if side == Sides.Buy and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif side == Sides.Sell and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return btc_difficulty_adjustments_strategy()
