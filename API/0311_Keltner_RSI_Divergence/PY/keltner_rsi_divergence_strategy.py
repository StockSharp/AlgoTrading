import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class keltner_rsi_divergence_strategy(Strategy):
    """
    Mean-reversion strategy that trades Keltner band extremes only when RSI diverges from price.
    """

    def __init__(self):
        super(keltner_rsi_divergence_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetRange(2, 100) \
            .SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetRange(2, 100) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")

        self._atr_multiplier = self.Param("AtrMultiplier", 1.15) \
            .SetRange(0.1, 10.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for the Keltner band width", "Indicators")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(2, 100) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 72) \
            .SetRange(1, 500) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_rsi = 50.0
        self._prev_price = 0.0
        self._is_initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_rsi_divergence_strategy, self).OnReseted()
        self._prev_rsi = 50.0
        self._prev_price = 0.0
        self._is_initialized = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(keltner_rsi_divergence_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = int(self._ema_period.Value)
        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = int(self._rsi_period.Value)
        self._is_initialized = False
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0, UnitTypes.Absolute),
            Unit(self._stop_loss_percent.Value, UnitTypes.Percent),
            False
        )

    def _process_candle(self, candle, ema_val, atr_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = float(ema_val)
        atr = float(atr_val)
        rsi = float(rsi_val)
        price = float(candle.ClosePrice)

        if not self._is_initialized:
            self._prev_price = price
            self._prev_rsi = rsi
            self._is_initialized = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_price = price
            self._prev_rsi = rsi
            return

        mult = float(self._atr_multiplier.Value)
        upper_band = ema + mult * atr
        lower_band = ema - mult * atr

        bullish_divergence = (rsi >= self._prev_rsi and price < self._prev_price) or rsi <= 30.0
        bearish_divergence = (rsi <= self._prev_rsi and price > self._prev_price) or rsi >= 70.0

        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if price <= lower_band + atr * 0.1 and bullish_divergence:
                self.BuyMarket()
                self._cooldown = cd
            elif price >= upper_band - atr * 0.1 and bearish_divergence:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if price >= ema or rsi >= 50.0:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = cd
        elif self.Position < 0:
            if price <= ema or rsi <= 50.0:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = cd

        self._prev_price = price
        self._prev_rsi = rsi

    def CreateClone(self):
        return keltner_rsi_divergence_strategy()
