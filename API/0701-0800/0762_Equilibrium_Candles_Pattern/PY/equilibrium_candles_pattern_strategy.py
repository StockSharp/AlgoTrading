import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class equilibrium_candles_pattern_strategy(Strategy):
    """
    Equilibrium candles pattern strategy.
    The equilibrium is the midpoint of the highest high and lowest low over EquilibriumLength candles. At least CandlesForTrend
    consecutive closes above it form a bullish trend; the first MaxPullbackCandles closes back below it are a pullback that goes
    long. A bearish trend of closes below it followed by closes back above goes short. UseReverse swaps the directions and an
    opposite signal reverses the position. With UseTpSl the stop and target lie StopMultiplier ATR from the entry, and with
    UseBigCandleExit a candle body larger than BigCandleMultiplier ATR closes the position.
    """

    def __init__(self):
        super(equilibrium_candles_pattern_strategy, self).__init__()
        self._equilibrium_length = self.Param("EquilibriumLength", 9).SetGreaterThanZero().SetDisplay("Equilibrium Length", "Lookback of the equilibrium", "Pattern")
        self._candles_for_trend = self.Param("CandlesForTrend", 7).SetGreaterThanZero().SetDisplay("Candles For Trend", "Consecutive closes that form a trend", "Pattern")
        self._max_pullback_candles = self.Param("MaxPullbackCandles", 2).SetGreaterThanZero().SetDisplay("Max Pullback Candles", "Maximum pullback candles that still give an entry", "Pattern")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Risk")
        self._stop_multiplier = self.Param("StopMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Stop Multiplier", "Stop and target distance in ATR", "Risk")
        self._use_tp_sl = self.Param("UseTpSl", True).SetDisplay("Use TP/SL", "Use the ATR stop and target", "Risk")
        self._use_big_candle_exit = self.Param("UseBigCandleExit", True).SetDisplay("Big Candle Exit", "Close the position on a big candle", "Risk")
        self._big_candle_multiplier = self.Param("BigCandleMultiplier", 1.0).SetGreaterThanZero().SetDisplay("Big Candle Multiplier", "Body size in ATR that makes a big candle", "Risk")
        self._use_reverse = self.Param("UseReverse", False).SetDisplay("Reverse", "Trade in the opposite direction", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._above_count = 0
        self._below_count = 0
        self._bull_trend_length = 0
        self._bear_trend_length = 0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(equilibrium_candles_pattern_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(equilibrium_candles_pattern_strategy, self).OnStarted2(time)

        self._reset_state()

        highest = Highest()
        highest.Length = self._equilibrium_length.Value
        lowest = Lowest()
        lowest.Length = self._equilibrium_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest_value, lowest_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        equilibrium = (float(highest_value) + float(lowest_value)) / 2.0
        atr = float(atr_value)
        close = float(candle.ClosePrice)
        trend_len = self._candles_for_trend.Value
        max_pullback = self._max_pullback_candles.Value

        bullish_pullback = False
        bearish_pullback = False

        if close > equilibrium:
            # The bearish trend that preceded this pullback.
            if self._below_count > 0:
                self._bear_trend_length = self._below_count
            self._above_count += 1
            self._below_count = 0
            bearish_pullback = self._bear_trend_length >= trend_len and self._above_count <= max_pullback
            if self._above_count > max_pullback:
                self._bear_trend_length = 0
        elif close < equilibrium:
            if self._above_count > 0:
                self._bull_trend_length = self._above_count
            self._below_count += 1
            self._above_count = 0
            bullish_pullback = self._bull_trend_length >= trend_len and self._below_count <= max_pullback
            if self._below_count > max_pullback:
                self._bull_trend_length = 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0 and self._check_exit(candle, atr):
            return

        reverse = self._use_reverse.Value
        go_long = bearish_pullback if reverse else bullish_pullback
        go_short = bullish_pullback if reverse else bearish_pullback
        mult = float(self._stop_multiplier.Value)

        if go_long and self.Position <= 0:
            self._stop_price = close - atr * mult
            self._take_price = close + atr * mult
            self.BuyMarket(self.Volume + abs(self.Position))
        elif go_short and self.Position >= 0:
            self._stop_price = close + atr * mult
            self._take_price = close - atr * mult
            self.SellMarket(self.Volume + abs(self.Position))

    def _check_exit(self, candle, atr):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        big_candle = self._use_big_candle_exit.Value and body > atr * float(self._big_candle_multiplier.Value)
        use_tp_sl = self._use_tp_sl.Value

        if self.Position > 0:
            if big_candle or (use_tp_sl and (low <= self._stop_price or high >= self._take_price)):
                self.SellMarket(self.Position)
                return True
        elif big_candle or (use_tp_sl and (high >= self._stop_price or low <= self._take_price)):
            self.BuyMarket(-self.Position)
            return True

        return False

    def CreateClone(self):
        return equilibrium_candles_pattern_strategy()
