import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


FIB_VOLUMES = [1, 1, 2, 3, 5, 8, 13, 21, 34, 55]


class elite_e_fibo_trader_strategy(Strategy):
    def __init__(self):
        super(elite_e_fibo_trader_strategy, self).__init__()

        self._levels_count = self.Param("LevelsCount", 6)
        self._open_buy = self.Param("OpenBuy", True)
        self._open_sell = self.Param("OpenSell", True)
        self._level_distance = self.Param("LevelDistance", 50.0)
        self._stop_loss_points = self.Param("StopLossPoints", 200.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 100.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._entry_price = 0.0
        self._current_level = 0
        self._active_direction = 0
        self._cycle_active = False

    @property
    def LevelsCount(self):
        return self._levels_count.Value

    @LevelsCount.setter
    def LevelsCount(self, value):
        self._levels_count.Value = value

    @property
    def OpenBuy(self):
        return self._open_buy.Value

    @OpenBuy.setter
    def OpenBuy(self, value):
        self._open_buy.Value = value

    @property
    def OpenSell(self):
        return self._open_sell.Value

    @OpenSell.setter
    def OpenSell(self, value):
        self._open_sell.Value = value

    @property
    def LevelDistance(self):
        return self._level_distance.Value

    @LevelDistance.setter
    def LevelDistance(self, value):
        self._level_distance.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(elite_e_fibo_trader_strategy, self).OnStarted(time)

        self._cycle_active = False
        self._current_level = 0
        self._active_direction = 0
        self._entry_price = 0.0

        sma = SimpleMovingAverage()
        sma.Length = 20

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sma_val = float(sma_value)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0

        if not self._cycle_active and self.Position == 0:
            if self.OpenBuy and close > sma_val:
                self._active_direction = 1
                self._entry_price = close
                self._current_level = 0
                self._cycle_active = True
                self.BuyMarket()
            elif self.OpenSell and close < sma_val:
                self._active_direction = -1
                self._entry_price = close
                self._current_level = 0
                self._cycle_active = True
                self.SellMarket()
        elif self._cycle_active:
            stop_distance = float(self.StopLossPoints) * step
            tp_distance = float(self.TakeProfitPoints) * step
            level_dist = float(self.LevelDistance) * step

            if self._active_direction == 1 and close <= self._entry_price - stop_distance:
                self.SellMarket()
                self._reset_cycle()
                return
            elif self._active_direction == -1 and close >= self._entry_price + stop_distance:
                self.BuyMarket()
                self._reset_cycle()
                return

            if self._active_direction == 1 and close >= self._entry_price + tp_distance:
                self.SellMarket()
                self._reset_cycle()
                return
            elif self._active_direction == -1 and close <= self._entry_price - tp_distance:
                self.BuyMarket()
                self._reset_cycle()
                return

            next_level = self._current_level + 1
            if next_level < int(self.LevelsCount) and next_level < len(FIB_VOLUMES):
                if self._active_direction == 1 and close <= self._entry_price - level_dist * next_level:
                    self.BuyMarket()
                    self._current_level = next_level
                elif self._active_direction == -1 and close >= self._entry_price + level_dist * next_level:
                    self.SellMarket()
                    self._current_level = next_level

    def _reset_cycle(self):
        self._cycle_active = False
        self._current_level = 0
        self._active_direction = 0
        self._entry_price = 0.0

    def OnReseted(self):
        super(elite_e_fibo_trader_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._current_level = 0
        self._active_direction = 0
        self._cycle_active = False

    def CreateClone(self):
        return elite_e_fibo_trader_strategy()
