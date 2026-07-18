import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, StochasticOscillator, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class rsi_stochastic_ma_strategy(Strategy):
    def __init__(self):
        super(rsi_stochastic_ma_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 3) \
            .SetDisplay("RSI Period", "RSI calculation period", "RSI")
        self._rsi_upper_level = self.Param("RsiUpperLevel", 65.0) \
            .SetDisplay("RSI Upper Level", "RSI overbought level", "RSI")
        self._rsi_lower_level = self.Param("RsiLowerLevel", 35.0) \
            .SetDisplay("RSI Lower Level", "RSI oversold level", "RSI")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period", "Trend")
        self._stoch_upper_level = self.Param("StochUpperLevel", 60.0) \
            .SetDisplay("Stochastic Upper", "Stochastic overbought level", "Stochastic")
        self._stoch_lower_level = self.Param("StochLowerLevel", 40.0) \
            .SetDisplay("Stochastic Lower", "Stochastic oversold level", "Stochastic")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._stochastic = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value
    @property
    def rsi_upper_level(self):
        return self._rsi_upper_level.Value
    @property
    def rsi_lower_level(self):
        return self._rsi_lower_level.Value
    @property
    def ma_period(self):
        return self._ma_period.Value
    @property
    def stoch_upper_level(self):
        return self._stoch_upper_level.Value
    @property
    def stoch_lower_level(self):
        return self._stoch_lower_level.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_stochastic_ma_strategy, self).OnReseted()
        self._stochastic = None

    def OnStarted2(self, time):
        super(rsi_stochastic_ma_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        ma = ExponentialMovingAverage()
        ma.Length = self.ma_period
        self._stochastic = StochasticOscillator()
        self.Indicators.Add(self._stochastic)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, rsi, self.on_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, ma_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        cv = CandleIndicatorValue(self._stochastic, candle)
        stoch_result = self._stochastic.Process(cv)
        if not self._stochastic.IsFormed:
            return
        k = stoch_result.K
        d = stoch_result.D
        if k is None or d is None:
            return
        k = float(k)
        ma_value = float(ma_value)
        rsi_value = float(rsi_value)
        price = float(candle.ClosePrice)
        is_up_trend = price > ma_value
        is_down_trend = price < ma_value

        if is_up_trend and rsi_value < float(self.rsi_lower_level) and k < float(self.stoch_lower_level) and self.Position == 0:
            self.BuyMarket()
        elif is_down_trend and rsi_value > float(self.rsi_upper_level) and k > float(self.stoch_upper_level) and self.Position == 0:
            self.SellMarket()

    def CreateClone(self):
        return rsi_stochastic_ma_strategy()
