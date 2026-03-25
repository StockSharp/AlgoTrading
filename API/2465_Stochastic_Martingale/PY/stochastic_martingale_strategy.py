import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class stochastic_martingale_strategy(Strategy):
    def __init__(self):
        super(stochastic_martingale_strategy, self).__init__()

        self._step = self.Param("Step", 25)
        self._step_mode = self.Param("StepMode", 0)
        self._profit_factor = self.Param("ProfitFactor", 20)
        self._mult = self.Param("Mult", 1.5)
        self._buy_volume_param = self.Param("BuyVolume", 0.01)
        self._sell_volume_param = self.Param("SellVolume", 0.01)
        self._k_period = self.Param("KPeriod", 200)
        self._d_period = self.Param("DPeriod", 20)
        self._zone_buy = self.Param("ZoneBuy", 65.0)
        self._zone_sell = self.Param("ZoneSell", 70.0)
        self._reverse = self.Param("Reverse", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._last_buy_price = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_price = 0.0
        self._last_sell_volume = 0.0
        self._buy_count = 0
        self._sell_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(stochastic_martingale_strategy, self).OnStarted(time)

        self._last_buy_price = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_price = 0.0
        self._last_sell_volume = 0.0
        self._buy_count = 0
        self._sell_count = 0

        stochastic = StochasticOscillator()
        stochastic.K.Length = int(self._k_period.Value)
        stochastic.D.Length = int(self._d_period.Value)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stochastic, self.ProcessCandle).Start()

    def _check_volume(self, volume):
        sec = self.Security
        vol_step = float(sec.VolumeStep) if sec is not None and sec.VolumeStep is not None else 0.0
        if vol_step > 0:
            volume = vol_step * Math.Floor(volume / vol_step)
        if volume <= 0:
            volume = 0.0
        return volume

    def ProcessCandle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k_val = stoch_value.K
        d_val = stoch_value.D
        if k_val is None or d_val is None:
            return

        k_val = float(k_val)
        d_val = float(d_val)

        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        step = float(self._step.Value) * price_step
        profit = float(self._profit_factor.Value) * price_step
        price = float(candle.ClosePrice)
        mult = float(self._mult.Value)
        zone_buy = float(self._zone_buy.Value)
        zone_sell = float(self._zone_sell.Value)
        reverse = bool(self._reverse.Value)
        buy_vol = float(self._buy_volume_param.Value)
        sell_vol = float(self._sell_volume_param.Value)
        pos = float(self.Position)
        step_mode = int(self._step_mode.Value)

        if self._buy_count > 0 and pos > 0:
            if (step_mode == 0 and price <= self._last_buy_price - step) or \
               (step_mode == 1 and price <= self._last_buy_price - step * self._buy_count):
                volume = self._check_volume(self._last_buy_volume * mult)
                if volume > 0:
                    self.BuyMarket(volume)
                    self._last_buy_price = price
                    self._last_buy_volume = volume
                    self._buy_count += 1

            if price >= self._last_buy_price + profit * self._buy_count:
                self.SellMarket(abs(float(self.Position)))
                self._buy_count = 0

        elif self._sell_count > 0 and pos < 0:
            if (step_mode == 0 and price >= self._last_sell_price + step) or \
               (step_mode == 1 and price >= self._last_sell_price + step * self._sell_count):
                volume = self._check_volume(self._last_sell_volume * mult)
                if volume > 0:
                    self.SellMarket(volume)
                    self._last_sell_price = price
                    self._last_sell_volume = volume
                    self._sell_count += 1

            if price <= self._last_sell_price - profit * self._sell_count:
                self.BuyMarket(abs(float(self.Position)))
                self._sell_count = 0

        elif float(self.Position) == 0:
            if k_val > d_val and d_val > zone_buy:
                if not reverse:
                    volume = self._check_volume(buy_vol)
                    if volume > 0:
                        self.BuyMarket(volume)
                        self._last_buy_price = price
                        self._last_buy_volume = volume
                        self._buy_count = 1
                else:
                    volume = self._check_volume(sell_vol)
                    if volume > 0:
                        self.SellMarket(volume)
                        self._last_sell_price = price
                        self._last_sell_volume = volume
                        self._sell_count = 1
            elif k_val < d_val and d_val < zone_sell:
                if not reverse:
                    volume = self._check_volume(sell_vol)
                    if volume > 0:
                        self.SellMarket(volume)
                        self._last_sell_price = price
                        self._last_sell_volume = volume
                        self._sell_count = 1
                else:
                    volume = self._check_volume(buy_vol)
                    if volume > 0:
                        self.BuyMarket(volume)
                        self._last_buy_price = price
                        self._last_buy_volume = volume
                        self._buy_count = 1

    def OnReseted(self):
        super(stochastic_martingale_strategy, self).OnReseted()
        self._last_buy_price = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_price = 0.0
        self._last_sell_volume = 0.0
        self._buy_count = 0
        self._sell_count = 0

    def CreateClone(self):
        return stochastic_martingale_strategy()
