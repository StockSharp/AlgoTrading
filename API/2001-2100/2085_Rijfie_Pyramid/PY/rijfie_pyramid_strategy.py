import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator, ExponentialMovingAverage, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class rijfie_pyramid_strategy(Strategy):
    def __init__(self):
        super(rijfie_pyramid_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._low_level = self.Param("LowLevel", 20.0) \
            .SetDisplay("Stochastic Low", "Oversold threshold", "Parameters")
        self._ma_period = self.Param("MaPeriod", 10) \
            .SetDisplay("EMA Period", "EMA length", "Parameters")
        self._step_level = self.Param("StepLevel", 1.0) \
            .SetDisplay("Step Level", "Percent drop for next buy", "Parameters")
        self._take_profit_pct = self.Param("TakeProfitPct", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stochastic = None
        self._next_buy_price = 0.0
        self._prev_k = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def low_level(self):
        return self._low_level.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def step_level(self):
        return self._step_level.Value

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    def OnReseted(self):
        super(rijfie_pyramid_strategy, self).OnReseted()
        self._stochastic = None
        self._next_buy_price = 0.0
        self._prev_k = None

    def OnStarted2(self, time):
        super(rijfie_pyramid_strategy, self).OnStarted2(time)
        self._stochastic = StochasticOscillator()
        ema = ExponentialMovingAverage()
        ema.Length = self.ma_period
        self.Indicators.Add(self._stochastic)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(float(self.take_profit_pct) * 2, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        cv = CandleIndicatorValue(self._stochastic, candle)
        stoch_result = self._stochastic.Process(cv)
        if not stoch_result.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k = stoch_result.K
        if k is None:
            return
        k = float(k)
        ema_value = float(ema_value)
        price = float(candle.ClosePrice)

        if self._prev_k is not None and self._prev_k < float(self.low_level) and k >= float(self.low_level) and self.Position == 0:
            self.BuyMarket()
            self._next_buy_price = price * (1.0 - float(self.step_level) / 100.0)
        elif self.Position > 0 and self._next_buy_price > 0 and price <= self._next_buy_price and price > ema_value:
            self.BuyMarket()
            self._next_buy_price = price * (1.0 - float(self.step_level) / 100.0)

        if self.Position > 0 and k > 80.0:
            self.SellMarket()
            self._next_buy_price = 0.0

        self._prev_k = k

    def CreateClone(self):
        return rijfie_pyramid_strategy()
