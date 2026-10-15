import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StochasticOscillator, AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class gold_eur_usd_strategy(Strategy):
    """
    Gold and EUR/USD liquidity grab strategy.
    A bullish liquidity grab is a candle that wicks below the lowest low of the previous candles while RSI is below Oversold and the
    Stochastic %K below StochOversold. Within the following candles a bullish fair value gap (low above the high two candles back) whose
    candle spans more than one ATR and closes above the previous high marks the market structure shift; if the close is also above the
    MaLength SMA the strategy goes long. Shorts mirror the rules. An opposite signal reverses the position.
    """

    SWING_LENGTH = 20
    ATR_LENGTH = 14
    SETUP_BARS = 10
    NEVER = 1 << 30

    def __init__(self):
        super(gold_eur_usd_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._ma_length = self.Param("MaLength", 50).SetGreaterThanZero().SetDisplay("MA Length", "SMA trend filter length", "Indicators")
        self._stoch_length = self.Param("StochLength", 14).SetGreaterThanZero().SetDisplay("Stoch Length", "Stochastic %K length", "Indicators")
        self._overbought = self.Param("Overbought", 70.0).SetDisplay("Overbought", "RSI overbought level", "Levels")
        self._oversold = self.Param("Oversold", 30.0).SetDisplay("Oversold", "RSI oversold level", "Levels")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stoch Overbought", "Stochastic overbought level", "Levels")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stoch Oversold", "Stochastic oversold level", "Levels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_highest = None
        self._prev_lowest = None
        self._high1 = None
        self._high2 = None
        self._low1 = None
        self._low2 = None
        self._bars_since_bull_grab = self.NEVER
        self._bars_since_bear_grab = self.NEVER

    def OnReseted(self):
        super(gold_eur_usd_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(gold_eur_usd_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_length.Value
        stochastic.D.Length = 3
        atr = AverageTrueRange()
        atr.Length = self.ATR_LENGTH
        highest = Highest()
        highest.Length = self.SWING_LENGTH
        lowest = Lowest()
        lowest.Length = self.SWING_LENGTH

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, sma, stochastic, atr, highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, stochastic)

    def _process_candle(self, candle, rsi_value, sma_value, stoch_value, atr_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        # The swing extremes and the fair value gap use the candles before this one.
        swing_high = self._prev_highest
        swing_low = self._prev_lowest
        high1 = self._high1
        high2 = self._high2
        low1 = self._low1
        low2 = self._low2

        if highest_value.IsFormed and lowest_value.IsFormed:
            self._prev_highest = highest_value.GetValue[Decimal](None)
            self._prev_lowest = lowest_value.GetValue[Decimal](None)

        self._high2 = self._high1
        self._low2 = self._low1
        self._high1 = candle.HighPrice
        self._low1 = candle.LowPrice

        if self._bars_since_bull_grab < self.NEVER:
            self._bars_since_bull_grab += 1
        if self._bars_since_bear_grab < self.NEVER:
            self._bars_since_bear_grab += 1

        if not rsi_value.IsFormed or not sma_value.IsFormed or not stoch_value.IsFormed or not atr_value.IsFormed:
            return

        stoch_k = stoch_value.K
        if stoch_k is None:
            return

        if swing_high is None or swing_low is None or high1 is None or high2 is None or low1 is None or low2 is None:
            return

        rsi = rsi_value.GetValue[Decimal](None)
        sma = sma_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if candle.LowPrice < swing_low and close > swing_low and rsi < Decimal(self._oversold.Value) and stoch_k < Decimal(self._stoch_oversold.Value):
            self._bars_since_bull_grab = 0

        if candle.HighPrice > swing_high and close < swing_high and rsi > Decimal(self._overbought.Value) and stoch_k > Decimal(self._stoch_overbought.Value):
            self._bars_since_bear_grab = 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        displacement = candle.HighPrice - candle.LowPrice > atr
        bullish_shift = displacement and candle.LowPrice > high2 and close > high1
        bearish_shift = displacement and candle.HighPrice < low2 and close < low1

        if self._bars_since_bull_grab <= self.SETUP_BARS and bullish_shift and close > sma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._bars_since_bull_grab = self.NEVER
        elif self._bars_since_bear_grab <= self.SETUP_BARS and bearish_shift and close < sma and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._bars_since_bear_grab = self.NEVER

    def CreateClone(self):
        return gold_eur_usd_strategy()
