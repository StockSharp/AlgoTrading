import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class volume_ma_cross_strategy(Strategy):
    """
    Volume MA Cross strategy.
    Uses fast/slow volume MA crossover with price MA for direction.
    Long: Price crosses above SMA.
    Short: Price crosses below SMA.
    """

    def __init__(self):
        super(volume_ma_cross_strategy, self).__init__()
        self._price_ma_period = self.Param("PriceMaPeriod", 20).SetDisplay("Price MA Period", "Period for price SMA", "Indicators")
        self._fast_vol_period = self.Param("FastVolPeriod", 10).SetDisplay("Fast Vol Period", "Period for fast volume MA", "Indicators")
        self._slow_vol_period = self.Param("SlowVolPeriod", 30).SetDisplay("Slow Vol Period", "Period for slow volume MA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._fast_vol_ma = None
        self._slow_vol_ma = None
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_ma_cross_strategy, self).OnReseted()
        self._fast_vol_ma = None
        self._slow_vol_ma = None
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(volume_ma_cross_strategy, self).OnStarted2(time)

        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._cooldown = 0

        self._fast_vol_ma = SimpleMovingAverage()
        self._fast_vol_ma.Length = self._fast_vol_period.Value
        self._slow_vol_ma = SimpleMovingAverage()
        self._slow_vol_ma.Length = self._slow_vol_period.Value

        sma = SimpleMovingAverage()
        sma.Length = self._price_ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        # Process volume through manual MAs
        fast_result = self._fast_vol_ma.Process(DecimalIndicatorValue(self._fast_vol_ma, candle.TotalVolume, candle.ServerTime))
        slow_result = self._slow_vol_ma.Process(DecimalIndicatorValue(self._slow_vol_ma, candle.TotalVolume, candle.ServerTime))
        fast_vol = float(fast_result)
        slow_vol = float(slow_result)

        close = float(candle.ClosePrice)
        sv = float(sma_val)

        if self._prev_close == 0:
            self._prev_close = close
            self._prev_ma = sv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            self._prev_ma = sv
            return

        cd = self._cooldown_bars.Value
        cross_up = self._prev_close <= self._prev_ma and close > sv
        cross_down = self._prev_close >= self._prev_ma and close < sv
        volume_expanding = self._slow_vol_ma.IsFormed and fast_vol > slow_vol

        if self.Position == 0 and cross_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and cross_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and (cross_down or (volume_expanding and close < sv)):
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and (cross_up or (volume_expanding and close > sv)):
            self.BuyMarket()
            self._cooldown = cd

        self._prev_close = close
        self._prev_ma = sv

    def CreateClone(self):
        return volume_ma_cross_strategy()
