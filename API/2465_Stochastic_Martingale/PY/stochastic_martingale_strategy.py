import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class stochastic_martingale_strategy(Strategy):
    """Stochastic oscillator with martingale averaging."""
    def __init__(self):
        super(stochastic_martingale_strategy, self).__init__()
        self._step = self.Param("Step", 25).SetGreaterThanZero().SetDisplay("Step", "Price step in points for averaging", "Martingale")
        self._step_mode = self.Param("StepMode", 0).SetDisplay("Step Mode", "0-fixed, 1-multiplied by orders count", "Martingale")
        self._profit_factor = self.Param("ProfitFactor", 20).SetGreaterThanZero().SetDisplay("Profit Factor", "Points for take profit per order", "Martingale")
        self._mult = self.Param("Mult", 1.5).SetGreaterThanZero().SetDisplay("Multiplier", "Volume multiplier for averaging", "Martingale")
        self._k_period = self.Param("KPeriod", 200).SetGreaterThanZero().SetDisplay("%K Period", "Stochastic %K period", "Indicators")
        self._d_period = self.Param("DPeriod", 20).SetGreaterThanZero().SetDisplay("%D Period", "Stochastic %D period", "Indicators")
        self._zone_buy = self.Param("ZoneBuy", 65.0).SetDisplay("Zone Buy", "Oversold level", "Indicators")
        self._zone_sell = self.Param("ZoneSell", 70.0).SetDisplay("Zone Sell", "Overbought level", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(stochastic_martingale_strategy, self).OnReseted()
        self._last_buy_price = 0
        self._last_sell_price = 0
        self._buy_count = 0
        self._sell_count = 0

    def OnStarted(self, time):
        super(stochastic_martingale_strategy, self).OnStarted(time)
        self._last_buy_price = 0
        self._last_sell_price = 0
        self._buy_count = 0
        self._sell_count = 0

        self._stoch = StochasticOscillator()
        self._stoch.K.Length = self._k_period.Value
        self._stoch.D.Length = self._d_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(self._stoch, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._stoch)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, stoch_val):
        if candle.State != CandleStates.Finished:
            return

        k_value = stoch_val.ToDecimal()
        inner = stoch_val.InnerValues
        d_value = None
        if inner is not None:
            for iv in inner:
                d_value = iv.Value.ToDecimal()
                break
        if k_value is None or d_value is None:
            return

        k_value = float(k_value)
        d_value = float(d_value)

        step = self._step.Value
        profit = self._profit_factor.Value
        price = float(candle.ClosePrice)
        zone_buy = self._zone_buy.Value
        zone_sell = self._zone_sell.Value

        if self._buy_count > 0 and self.Position > 0:
            step_dist = step if self._step_mode.Value == 0 else step * self._buy_count
            if price <= self._last_buy_price - step_dist:
                self.BuyMarket()
                self._last_buy_price = price
                self._buy_count += 1
            if price >= self._last_buy_price + profit * self._buy_count:
                self.SellMarket()
                self._buy_count = 0
        elif self._sell_count > 0 and self.Position < 0:
            step_dist = step if self._step_mode.Value == 0 else step * self._sell_count
            if price >= self._last_sell_price + step_dist:
                self.SellMarket()
                self._last_sell_price = price
                self._sell_count += 1
            if price <= self._last_sell_price - profit * self._sell_count:
                self.BuyMarket()
                self._sell_count = 0
        elif self.Position == 0:
            if k_value > d_value and d_value > zone_buy:
                self.BuyMarket()
                self._last_buy_price = price
                self._buy_count = 1
            elif k_value < d_value and d_value < zone_sell:
                self.SellMarket()
                self._last_sell_price = price
                self._sell_count = 1

    def CreateClone(self):
        return stochastic_martingale_strategy()
