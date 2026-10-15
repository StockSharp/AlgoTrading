import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, HurstExponent
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

RANDOM_WALK_HURST = 0.5


class hurst_volatility_filter_strategy(Strategy):
    """
    Hurst exponent mean reversion with a volatility filter.
    Trades back toward the moving average only while the Hurst exponent is below 0.5 (anti-persistent prices)
    and ATR is below its own average. Exits when price returns to the moving average or volatility expands.
    """

    def __init__(self):
        super(hurst_volatility_filter_strategy, self).__init__()

        self._hurst_period = self.Param("HurstPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Hurst Period", "Period for the Hurst exponent", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for the moving average", "Indicators")
        self._atr_period = self.Param("ATRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR and its average", "Indicators")
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._atr_average = None
        self._hurst = None
        self._prev_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(hurst_volatility_filter_strategy, self).OnReseted()
        self._atr_average = None
        self._hurst = None
        self._prev_close = None

    def OnStarted2(self, time):
        super(hurst_volatility_filter_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._hurst = HurstExponent()
        self._hurst.Length = self._hurst_period.Value
        self._atr_average = SimpleMovingAverage()
        self._atr_average.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, atr, self._process_candle).Start()

        stop = float(self._stop_loss.Value)
        self.StartProtection(None, Unit(stop, UnitTypes.Percent) if stop > 0 else None)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        sma_value = float(sma_value)
        atr_value = float(atr_value)
        close = float(candle.ClosePrice)

        atr_average = float(process_float(self._atr_average, atr_value, candle.ServerTime, True))

        prev_close = self._prev_close
        self._prev_close = close

        if prev_close is None:
            return

        # Hurst is measured on bar-to-bar price changes, where 0.5 separates trending from mean-reverting behaviour.
        hurst_result = process_float(self._hurst, close - prev_close, candle.ServerTime, True)

        if not self._atr_average.IsFormed or not self._hurst.IsFormed or hurst_result.IsEmpty:
            return

        hurst_value = float(hurst_result)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        is_quiet = atr_value < atr_average
        is_mean_reverting = hurst_value < RANDOM_WALK_HURST

        if self.Position > 0 and (close >= sma_value or not is_quiet):
            self.SellMarket(self.Position)
            return

        if self.Position < 0 and (close <= sma_value or not is_quiet):
            self.BuyMarket(-self.Position)
            return

        if not is_mean_reverting or not is_quiet:
            return

        if close < sma_value and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close > sma_value and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return hurst_volatility_filter_strategy()
