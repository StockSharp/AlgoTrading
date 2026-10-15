import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class expert_adc_pl_stoch_strategy(Strategy):
    def __init__(self):
        super(expert_adc_pl_stoch_strategy, self).__init__()

        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stoch Period", "Stochastic period", "Indicators")
        self._long_threshold = self.Param("LongThreshold", 30.0) \
            .SetDisplay("Long Threshold", "Stochastic below this for long", "Signals")
        self._short_threshold = self.Param("ShortThreshold", 70.0) \
            .SetDisplay("Short Threshold", "Stochastic above this for short", "Signals")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._stoch = None
        self._candles = []
        self._candles_since_trade = 0

    @property
    def stoch_period(self):
        return self._stoch_period.Value

    @property
    def long_threshold(self):
        return self._long_threshold.Value

    @property
    def short_threshold(self):
        return self._short_threshold.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(expert_adc_pl_stoch_strategy, self).OnReseted()
        self._stoch = None
        self._candles = []
        self._candles_since_trade = self.signal_cooldown

    def OnStarted2(self, time):
        super(expert_adc_pl_stoch_strategy, self).OnStarted2(time)

        self._stoch = StochasticOscillator()
        self._stoch.K.Length = self.stoch_period
        self._stoch.D.Length = 3
        self._candles = []
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.BindEx(self._stoch, self._process_candle)
        subscription.Start()

        self.StartProtection(takeProfit=Unit(2, UnitTypes.Percent), stopLoss=Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        k_value = float(stoch_value.K)

        self._candles.append(candle)
        if len(self._candles) > 10:
            self._candles.pop(0)

        if len(self._candles) >= 2:
            curr = self._candles[-1]
            prev = self._candles[-2]

            is_piercing = (float(prev.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) > float(curr.OpenPrice)
                and float(curr.OpenPrice) < float(prev.LowPrice)
                and float(curr.ClosePrice) > (float(prev.OpenPrice) + float(prev.ClosePrice)) / 2.0)

            is_dark_cloud = (float(prev.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) > float(curr.ClosePrice)
                and float(curr.OpenPrice) > float(prev.HighPrice)
                and float(curr.ClosePrice) < (float(prev.OpenPrice) + float(prev.ClosePrice)) / 2.0)

            if is_piercing and k_value < self.long_threshold and self.Position == 0 and self._candles_since_trade >= self.signal_cooldown:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif is_dark_cloud and k_value > self.short_threshold and self.Position == 0 and self._candles_since_trade >= self.signal_cooldown:
                self.SellMarket()
                self._candles_since_trade = 0

    def CreateClone(self):
        return expert_adc_pl_stoch_strategy()
