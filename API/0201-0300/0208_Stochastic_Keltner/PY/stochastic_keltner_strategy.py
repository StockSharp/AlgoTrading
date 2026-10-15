import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class stochastic_keltner_strategy(Strategy):
    """
    Stochastic Keltner strategy.
    The Keltner Channel is the EmaPeriod EMA plus and minus KeltnerMultiplier times the AtrPeriod ATR; %K is the stochastic over StochPeriod
    candles smoothed over StochK candles. %K below StochOversold with a close below the lower band goes long and %K above StochOverbought
    with a close above the upper band goes short, reversing an opposite position. A position closes once price returns to the middle band.
    The stop lies AtrMultiplier ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(stochastic_keltner_strategy, self).__init__()
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic")
        self._stoch_k = self.Param("StochK", 3).SetGreaterThanZero().SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stochastic Oversold", "%K level for longs", "Stochastic")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stochastic Overbought", "%K level for shorts", "Stochastic")
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "Period of the channel EMA", "Keltner")
        self._keltner_multiplier = self.Param("KeltnerMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Keltner Multiplier", "ATR multiplier of the channel width", "Keltner")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the channel and stop ATR", "Keltner")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(stochastic_keltner_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(stochastic_keltner_strategy, self).OnStarted2(time)

        self._reset_state()

        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_k.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stochastic)

    def _process_candle(self, candle, stochastic_value, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not stochastic_value.IsFormed or not ema_value.IsFormed or not atr_value.IsFormed:
            return
        # The smoothed %K is the moving average the core oscillator exposes as D.
        if stochastic_value.D is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k = stochastic_value.D
        middle = ema_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        multiplier = Decimal(self._keltner_multiplier.Value)
        upper = middle + multiplier * atr
        lower = middle - multiplier * atr
        close = candle.ClosePrice

        stop_atr = Decimal(self._atr_multiplier.Value)
        if k < Decimal(self._stoch_oversold.Value) and close < lower and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif k > Decimal(self._stoch_overbought.Value) and close > upper and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (close >= middle or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close <= middle or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return stochastic_keltner_strategy()
