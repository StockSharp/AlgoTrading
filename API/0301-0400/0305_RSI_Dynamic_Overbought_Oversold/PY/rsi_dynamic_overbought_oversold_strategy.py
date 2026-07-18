import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class rsi_dynamic_overbought_oversold_strategy(Strategy):
    """
    RSI strategy with dynamic overbought and oversold bands derived from
    the rolling mean and volatility of RSI.
    """

    def __init__(self):
        super(rsi_dynamic_overbought_oversold_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")

        self._moving_avg_period = self.Param("MovingAvgPeriod", 34) \
            .SetDisplay("Average Period", "Period for moving averages and RSI volatility", "Indicators")

        self._std_dev_multiplier = self.Param("StdDevMultiplier", 1.3) \
            .SetDisplay("StdDev Multiplier", "Multiplier for the dynamic RSI bands", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 48) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for the strategy", "General")

        self._rsi = None
        self._price_sma = None
        self._rsi_sma = None
        self._rsi_std_dev = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_dynamic_overbought_oversold_strategy, self).OnReseted()
        self._rsi = None
        self._price_sma = None
        self._rsi_sma = None
        self._rsi_std_dev = None
        self._cooldown = 0

    def OnStarted2(self, time):
        super(rsi_dynamic_overbought_oversold_strategy, self).OnStarted2(time)

        ma_period = int(self._moving_avg_period.Value)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_period.Value)
        self._price_sma = SimpleMovingAverage()
        self._price_sma.Length = ma_period
        self._rsi_sma = SimpleMovingAverage()
        self._rsi_sma.Length = ma_period
        self._rsi_std_dev = StandardDeviation()
        self._rsi_std_dev.Length = ma_period
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._price_sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawIndicator(area, self._price_sma)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_percent.Value, UnitTypes.Percent), False)

    def _process_candle(self, candle, rsi_value, price_sma_value):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_value)

        rsi_average_value = float(process_float(self._rsi_sma, Decimal(rv), candle.OpenTime, True))

        rsi_std_dev_value = float(process_float(self._rsi_std_dev, Decimal(rv), candle.OpenTime, True))

        if not self._rsi.IsFormed or not self._price_sma.IsFormed or not self._rsi_sma.IsFormed or not self._rsi_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        sdm = float(self._std_dev_multiplier.Value)
        dynamic_overbought = min(rsi_average_value + sdm * rsi_std_dev_value, 85.0)
        dynamic_oversold = max(rsi_average_value - sdm * rsi_std_dev_value, 15.0)
        price = float(candle.ClosePrice)
        psv = float(price_sma_value)
        bullish_filter = price >= psv * 0.995
        bearish_filter = price <= psv * 1.005
        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if rv <= dynamic_oversold and bullish_filter:
                self.BuyMarket()
                self._cooldown = cd
            elif rv >= dynamic_overbought and bearish_filter:
                self.SellMarket()
                self._cooldown = cd
            return

        if self.Position > 0 and (rv >= rsi_average_value or price < psv * 0.995):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = cd
        elif self.Position < 0 and (rv <= rsi_average_value or price > psv * 1.005):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = cd

    def CreateClone(self):
        return rsi_dynamic_overbought_oversold_strategy()
