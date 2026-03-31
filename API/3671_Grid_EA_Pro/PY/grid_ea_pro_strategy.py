import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class grid_ea_pro_strategy(Strategy):
    def __init__(self):
        super(grid_ea_pro_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._rsi_period = self.Param("RsiPeriod", 10)
        self._rsi_upper_level = self.Param("RsiUpperLevel", 70.0)
        self._rsi_lower_level = self.Param("RsiLowerLevel", 30.0)
        self._take_profit_pct = self.Param("TakeProfitPct", 1.0)
        self._stop_loss_pct = self.Param("StopLossPct", 0.5)

        self._entry_price = 0.0
        self._direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiUpperLevel(self):
        return self._rsi_upper_level.Value

    @RsiUpperLevel.setter
    def RsiUpperLevel(self, value):
        self._rsi_upper_level.Value = value

    @property
    def RsiLowerLevel(self):
        return self._rsi_lower_level.Value

    @RsiLowerLevel.setter
    def RsiLowerLevel(self, value):
        self._rsi_lower_level.Value = value

    @property
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

    def OnReseted(self):
        super(grid_ea_pro_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._direction = 0

    def OnStarted2(self, time):
        super(grid_ea_pro_strategy, self).OnStarted2(time)
        self._entry_price = 0.0
        self._direction = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        close = float(candle.ClosePrice)
        tp_pct = float(self.TakeProfitPct)
        sl_pct = float(self.StopLossPct)

        # Check exit
        if self.Position != 0 and self._entry_price > 0:
            if self._direction > 0:
                pnl = (close - self._entry_price) / self._entry_price * 100.0
                if pnl >= tp_pct or pnl <= -sl_pct:
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._direction = 0
                    return
            elif self._direction < 0:
                pnl = (self._entry_price - close) / self._entry_price * 100.0
                if pnl >= tp_pct or pnl <= -sl_pct:
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._direction = 0
                    return

        # RSI entry
        if self.Position == 0:
            if rsi_val <= float(self.RsiLowerLevel):
                self.BuyMarket()
                self._entry_price = close
                self._direction = 1
            elif rsi_val >= float(self.RsiUpperLevel):
                self.SellMarket()
                self._entry_price = close
                self._direction = -1

    def CreateClone(self):
        return grid_ea_pro_strategy()
