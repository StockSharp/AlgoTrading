import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class atr_range_strategy(Strategy):
    """
    ATR Range strategy.
    Enters long when price moves up by at least ATR over N candles,
    enters short when price moves down by at least ATR over N candles.
    """

    def __init__(self):
        super(atr_range_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._atr_period = self.Param("ATRPeriod", 14).SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 5).SetDisplay("Lookback Period", "Number of candles to measure price movement", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._n_bars_ago_price = 0.0
        self._bar_counter = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_range_strategy, self).OnReseted()
        self._n_bars_ago_price = 0.0
        self._bar_counter = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(atr_range_strategy, self).OnStarted(time)

        self._n_bars_ago_price = 0.0
        self._bar_counter = 0
        self._cooldown = 0

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        self._bar_counter += 1
        close = float(candle.ClosePrice)
        lb = self._lookback_period.Value

        if self._bar_counter == 1 or self._bar_counter % lb == 1:
            self._n_bars_ago_price = close
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        mv = float(ma_val)
        av = float(atr_val)
        cd = self._cooldown_bars.Value

        # Check at end of each lookback period
        if self._bar_counter % lb == 0:
            price_movement = close - self._n_bars_ago_price
            abs_movement = abs(price_movement)

            if abs_movement >= av:
                if self.Position == 0 and price_movement > 0:
                    self.BuyMarket()
                    self._cooldown = cd
                elif self.Position == 0 and price_movement < 0:
                    self.SellMarket()
                    self._cooldown = cd

        # Exit logic: price crosses MA
        if self.Position > 0 and close < mv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > mv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return atr_range_strategy()
