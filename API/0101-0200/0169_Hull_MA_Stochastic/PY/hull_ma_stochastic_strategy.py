import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, StochasticOscillator, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_stochastic_strategy(Strategy):
    """
    Hull MA Stochastic strategy.
    The Hull average turns up when it rises after falling and turns down when it falls after rising. A turn up with %K below StochOversold
    goes long and a turn down with %K above StochOverbought goes short, reversing an opposite position. A long closes when the Hull average
    falls and a short when it rises. %K is the stochastic over StochPeriod candles
    smoothed over StochK candles. The stop lies StopLossAtr ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(hull_ma_stochastic_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetGreaterThanZero().SetDisplay("HMA Period", "Period of the Hull moving average", "Indicators")
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic")
        self._stoch_k = self.Param("StochK", 3).SetGreaterThanZero().SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stochastic Oversold", "%K level for longs", "Stochastic")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stochastic Overbought", "%K level for shorts", "Stochastic")
        self._stop_loss_atr = self.Param("StopLossAtr", 2.0).SetNotNegative().SetDisplay("Stop Loss ATR", "Stop distance from the entry in ATRs", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_hull = None
        self._prev_prev_hull = None
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(hull_ma_stochastic_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hull_ma_stochastic_strategy, self).OnStarted2(time)

        self._reset_state()

        hull = HullMovingAverage()
        hull.Length = self._hma_period.Value
        # The D line of the core oscillator is the smoothed %K.
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_k.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hull, stochastic, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stochastic)

    def _process_candle(self, candle, hull_value, stochastic_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not hull_value.IsFormed or not stochastic_value.IsFormed or not atr_value.IsFormed or stochastic_value.D is None:
            return

        hull = hull_value.GetValue[Decimal](None)
        prev_hull = self._prev_hull
        prev_prev_hull = self._prev_prev_hull
        self._prev_prev_hull = prev_hull
        self._prev_hull = hull

        if prev_hull is None or prev_prev_hull is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k = stochastic_value.D
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        rising = hull > prev_hull
        falling = hull < prev_hull
        turns_up = rising and prev_hull < prev_prev_hull
        turns_down = falling and prev_hull > prev_prev_hull

        stop_atr = Decimal(self._stop_loss_atr.Value)
        if turns_up and k < Decimal(self._stoch_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif turns_down and k > Decimal(self._stoch_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (falling or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (rising or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return hull_ma_stochastic_strategy()
