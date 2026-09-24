import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class martini_martingale_strategy(Strategy):
    def __init__(self):
        super(martini_martingale_strategy, self).__init__()

        self._step = self.Param("Step", 10.0).SetGreaterThanZero()
        self._profit_close = self.Param("ProfitClose", 10.0).SetGreaterThanZero()
        self._initial_volume = self.Param("InitialVolume", 0.1).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._legs = []
        self._buy_trigger = None
        self._sell_trigger = None
        self._last_execution_price = 0.0
        self._last_leg_volume = 0.0
        self._order_count = 0

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(martini_martingale_strategy, self).OnReseted()
        self._reset_cycle()

    def OnStarted2(self, time):
        super(martini_martingale_strategy, self).OnStarted2(time)
        self._reset_cycle()
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if self._legs and self._floating_pnl(close) >= float(self._profit_close.Value):
            self._flatten()
            self._reset_cycle()
            return

        step = float(self._step.Value)

        if not self._legs:
            if self._buy_trigger is None or self._sell_trigger is None:
                self._buy_trigger = close + step
                self._sell_trigger = close - step
                return

            buy_hit = float(candle.HighPrice) >= self._buy_trigger
            sell_hit = float(candle.LowPrice) <= self._sell_trigger
            if not buy_hit and not sell_hit:
                return

            if buy_hit and sell_hit:
                side = Sides.Buy if float(candle.ClosePrice) >= float(candle.OpenPrice) else Sides.Sell
            else:
                side = Sides.Buy if buy_hit else Sides.Sell

            price = self._buy_trigger if side == Sides.Buy else self._sell_trigger
            self._execute_leg(side, float(self._initial_volume.Value), price)
            self._buy_trigger = None
            self._sell_trigger = None
            return

        adverse = step * self._order_count
        if self.Position > 0 and float(candle.LowPrice) <= self._last_execution_price - adverse:
            self._execute_leg(Sides.Sell, self._last_leg_volume * 2.0, self._last_execution_price - adverse)
        elif self.Position < 0 and float(candle.HighPrice) >= self._last_execution_price + adverse:
            self._execute_leg(Sides.Buy, self._last_leg_volume * 2.0, self._last_execution_price + adverse)

    def _execute_leg(self, side, volume, price):
        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._legs.append((side, volume, price))
        self._last_execution_price = price
        self._last_leg_volume = volume
        self._order_count += 1

    def _floating_pnl(self, market_price):
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        step_price = float(self.Security.StepPrice) if self.Security is not None and self.Security.StepPrice is not None else 0.0
        multiplier = float(self.Security.Multiplier) if self.Security is not None and self.Security.Multiplier is not None else 1.0

        total = 0.0
        for side, volume, price in self._legs:
            direction = 1.0 if side == Sides.Buy else -1.0
            difference = (market_price - price) * direction
            total += difference / price_step * step_price * volume if price_step > 0 and step_price > 0 else difference * multiplier * volume

        return total

    def _flatten(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))

    def _reset_cycle(self):
        self._legs = []
        self._buy_trigger = None
        self._sell_trigger = None
        self._last_execution_price = 0.0
        self._last_leg_volume = 0.0
        self._order_count = 0

    def CreateClone(self):
        return martini_martingale_strategy()
