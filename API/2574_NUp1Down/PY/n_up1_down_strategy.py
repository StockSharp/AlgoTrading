import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class n_up1_down_strategy(Strategy):
    def __init__(self):
        super(n_up1_down_strategy, self).__init__()

        self._bars_count = self.Param("BarsCount", 3)
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0)
        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0)
        self._risk_percent = self.Param("RiskPercent", 5.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._recent_candles = []
        self._pip_size = 0.0
        self._entry_price = None
        self._active_stop_price = None
        self._active_take_price = None

    @property
    def BarsCount(self):
        return self._bars_count.Value

    @BarsCount.setter
    def BarsCount(self, value):
        self._bars_count.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @TrailingStopPips.setter
    def TrailingStopPips(self, value):
        self._trailing_stop_pips.Value = value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @TrailingStepPips.setter
    def TrailingStepPips(self, value):
        self._trailing_step_pips.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @RiskPercent.setter
    def RiskPercent(self, value):
        self._risk_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _calculate_pip_size(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
            if step > 0.0:
                decimals = self.Security.Decimals if self.Security.Decimals is not None else 0
                if decimals == 3 or decimals == 5:
                    return step * 10.0
                return step
        return 1.0

    def OnStarted(self, time):
        super(n_up1_down_strategy, self).OnStarted(time)

        self._pip_size = self._calculate_pip_size()
        self._recent_candles = []
        self._entry_price = None
        self._active_stop_price = None
        self._active_take_price = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_trailing_and_exits(candle)

        open_price = float(candle.OpenPrice)
        close_price = float(candle.ClosePrice)

        self._recent_candles.append((open_price, close_price))
        bars_needed = int(self.BarsCount) + 1
        while len(self._recent_candles) > bars_needed:
            self._recent_candles.pop(0)

        if len(self._recent_candles) < bars_needed:
            return

        candles = self._recent_candles[:]
        last = candles[-1]

        # Last candle must be bearish
        if last[1] >= last[0]:
            return

        is_pattern = True
        bars_count = int(self.BarsCount)

        for i in range(1, bars_count + 1):
            index = len(candles) - 1 - i
            bar = candles[index]

            # Each preceding bar must be bullish
            if bar[1] <= bar[0]:
                is_pattern = False
                break

            # Each bullish bar must close higher than the previous
            if i < bars_count:
                prev = candles[index - 1]
                if bar[1] <= prev[1]:
                    is_pattern = False
                    break

        if not is_pattern:
            return

        if self.Position < 0:
            return

        self.SellMarket()

        self._entry_price = close_price
        self._active_stop_price = self._entry_price + float(self.StopLossPips) * self._pip_size
        self._active_take_price = self._entry_price - float(self.TakeProfitPips) * self._pip_size

    def _update_trailing_and_exits(self, candle):
        if self.Position < 0:
            high = float(candle.HighPrice)
            low = float(candle.LowPrice)
            close = float(candle.ClosePrice)

            if self._active_stop_price is not None and high >= self._active_stop_price:
                self.BuyMarket()
                self._reset_position_state()
                return

            if self._active_take_price is not None and low <= self._active_take_price:
                self.BuyMarket()
                self._reset_position_state()
                return

            if self._active_stop_price is not None:
                trailing_distance = float(self.TrailingStopPips) * self._pip_size
                trailing_step = float(self.TrailingStepPips) * self._pip_size

                if trailing_distance <= 0.0:
                    return

                new_stop_candidate = close + trailing_distance

                if new_stop_candidate + trailing_step < self._active_stop_price:
                    self._active_stop_price = new_stop_candidate

        elif self.Position == 0:
            self._reset_position_state()

    def _reset_position_state(self):
        self._entry_price = None
        self._active_stop_price = None
        self._active_take_price = None

    def OnReseted(self):
        super(n_up1_down_strategy, self).OnReseted()
        self._recent_candles = []
        self._pip_size = 0.0
        self._reset_position_state()

    def CreateClone(self):
        return n_up1_down_strategy()
