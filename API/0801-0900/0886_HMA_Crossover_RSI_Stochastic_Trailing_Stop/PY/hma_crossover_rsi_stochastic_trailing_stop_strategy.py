import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class hma_crossover_rsi_stochastic_trailing_stop_strategy(Strategy):
    """
    HMA Crossover RSI Stochastic Trailing Stop strategy.
    Goes long when the fast HMA crosses above the slow HMA while RSI is below RsiBuyLevel and the smoothed Stochastic is below
    StochBuyLevel. Goes short when the fast HMA crosses below the slow HMA while RSI is above RsiSellLevel and the smoothed
    Stochastic is above StochSellLevel. An opposite signal reverses the position and a percent trailing stop manages exits.
    """

    def __init__(self):
        super(hma_crossover_rsi_stochastic_trailing_stop_strategy, self).__init__()
        self._fast_hma_length = self.Param("FastHmaLength", 5).SetGreaterThanZero().SetDisplay("Fast HMA", "Fast HMA length", "Indicators")
        self._slow_hma_length = self.Param("SlowHmaLength", 20).SetGreaterThanZero().SetDisplay("Slow HMA", "Slow HMA length", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_buy_level = self.Param("RsiBuyLevel", 45.0).SetDisplay("RSI Buy Level", "RSI must be below this level for a long", "Signals")
        self._rsi_sell_level = self.Param("RsiSellLevel", 60.0).SetDisplay("RSI Sell Level", "RSI must be above this level for a short", "Signals")
        self._stoch_length = self.Param("StochLength", 14).SetGreaterThanZero().SetDisplay("Stoch Length", "Stochastic %K length", "Indicators")
        self._stoch_smooth = self.Param("StochSmooth", 3).SetGreaterThanZero().SetDisplay("Stoch Smooth", "Smoothing period of the Stochastic", "Indicators")
        self._stoch_buy_level = self.Param("StochBuyLevel", 39.0).SetDisplay("Stoch Buy Level", "Smoothed Stochastic must be below this level for a long", "Signals")
        self._stoch_sell_level = self.Param("StochSellLevel", 63.0).SetDisplay("Stoch Sell Level", "Smoothed Stochastic must be above this level for a short", "Signals")
        self._trailing_percent = self.Param("TrailingPercent", 5.0).SetNotNegative().SetDisplay("Trailing %", "Trailing stop distance in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fast = None
        self._prev_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hma_crossover_rsi_stochastic_trailing_stop_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(hma_crossover_rsi_stochastic_trailing_stop_strategy, self).OnStarted2(time)

        self._prev_fast = None
        self._prev_slow = None

        fast_hma = HullMovingAverage()
        fast_hma.Length = self._fast_hma_length.Value
        slow_hma = HullMovingAverage()
        slow_hma.Length = self._slow_hma_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_length.Value
        stochastic.D.Length = self._stoch_smooth.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_hma, slow_hma, rsi, stochastic, self._process_candle).Start()

        trailing = float(self._trailing_percent.Value)
        self.StartProtection(Unit(), Unit(Decimal(trailing), UnitTypes.Percent) if trailing > 0 else Unit(), isStopTrailing=True, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_hma)
            self.DrawIndicator(area, slow_hma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, stochastic)

    def _process_candle(self, candle, fast_value, slow_value, rsi_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not slow_value.IsFormed:
            return

        fast = float(fast_value.GetValue[Decimal](None))
        slow = float(slow_value.GetValue[Decimal](None))

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if prev_fast is None or prev_slow is None:
            return

        if not rsi_value.IsFormed or not stoch_value.IsFormed or stoch_value.D is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = float(rsi_value.GetValue[Decimal](None))
        stoch = float(stoch_value.D)
        cross_up = prev_fast <= prev_slow and fast > slow
        cross_down = prev_fast >= prev_slow and fast < slow

        if cross_up and rsi < float(self._rsi_buy_level.Value) and stoch < float(self._stoch_buy_level.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and rsi > float(self._rsi_sell_level.Value) and stoch > float(self._stoch_sell_level.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return hma_crossover_rsi_stochastic_trailing_stop_strategy()
