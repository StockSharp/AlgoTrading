import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class ilan14_strategy(Strategy):
    def __init__(self):
        super(ilan14_strategy, self).__init__()

        self._pip_step = self.Param("PipStep", 30.0)
        self._lot_exponent = self.Param("LotExponent", 1.667)
        self._max_trades = self.Param("MaxTrades", 1)
        self._take_profit = self.Param("TakeProfit", 300.0)
        self._initial_volume = self.Param("InitialVolume", 0.1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_volume = 0.0
        self._buy_volume = 0.0
        self._sell_volume = 0.0
        self._avg_buy_price = 0.0
        self._avg_sell_price = 0.0
        self._buy_count = 0
        self._sell_count = 0

    @property
    def PipStep(self):
        return self._pip_step.Value

    @PipStep.setter
    def PipStep(self, value):
        self._pip_step.Value = value

    @property
    def LotExponent(self):
        return self._lot_exponent.Value

    @LotExponent.setter
    def LotExponent(self, value):
        self._lot_exponent.Value = value

    @property
    def MaxTrades(self):
        return self._max_trades.Value

    @MaxTrades.setter
    def MaxTrades(self, value):
        self._max_trades.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    @InitialVolume.setter
    def InitialVolume(self, value):
        self._initial_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ilan14_strategy, self).OnStarted(time)

        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_volume = 0.0
        self._buy_volume = 0.0
        self._sell_volume = 0.0
        self._avg_buy_price = 0.0
        self._avg_sell_price = 0.0
        self._buy_count = 0
        self._sell_count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        price = float(candle.ClosePrice)
        pip_step = float(self.PipStep)
        lot_exp = float(self.LotExponent)
        max_trades = int(self.MaxTrades)
        tp = float(self.TakeProfit)
        init_vol = float(self.InitialVolume)

        if self._buy_count == 0 and self._sell_count == 0:
            self.BuyMarket()
            self.SellMarket()
            self._last_buy_price = price
            self._last_sell_price = price
            self._last_buy_volume = init_vol
            self._last_sell_volume = init_vol
            self._buy_volume = init_vol
            self._sell_volume = init_vol
            self._avg_buy_price = price
            self._avg_sell_price = price
            self._buy_count = 1
            self._sell_count = 1
            return

        if self._buy_count > 0:
            if self._buy_count < max_trades and price <= self._last_buy_price - pip_step * step:
                vol = self._last_buy_volume * lot_exp
                self.BuyMarket()
                self._last_buy_volume = vol
                self._last_buy_price = price
                self._avg_buy_price = (self._avg_buy_price * self._buy_volume + price * vol) / (self._buy_volume + vol)
                self._buy_volume += vol
                self._buy_count += 1

            if self._buy_volume > 0.0 and price >= self._avg_buy_price + tp * step:
                self.SellMarket()
                self._buy_volume = 0.0
                self._last_buy_price = 0.0
                self._last_buy_volume = 0.0
                self._avg_buy_price = 0.0
                self._buy_count = 0

        if self._sell_count > 0:
            if self._sell_count < max_trades and price >= self._last_sell_price + pip_step * step:
                vol = self._last_sell_volume * lot_exp
                self.SellMarket()
                self._last_sell_volume = vol
                self._last_sell_price = price
                self._avg_sell_price = (self._avg_sell_price * self._sell_volume + price * vol) / (self._sell_volume + vol)
                self._sell_volume += vol
                self._sell_count += 1

            if self._sell_volume > 0.0 and price <= self._avg_sell_price - tp * step:
                self.BuyMarket()
                self._sell_volume = 0.0
                self._last_sell_price = 0.0
                self._last_sell_volume = 0.0
                self._avg_sell_price = 0.0
                self._sell_count = 0

    def OnReseted(self):
        super(ilan14_strategy, self).OnReseted()
        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_volume = 0.0
        self._buy_volume = 0.0
        self._sell_volume = 0.0
        self._avg_buy_price = 0.0
        self._avg_sell_price = 0.0
        self._buy_count = 0
        self._sell_count = 0

    def CreateClone(self):
        return ilan14_strategy()
