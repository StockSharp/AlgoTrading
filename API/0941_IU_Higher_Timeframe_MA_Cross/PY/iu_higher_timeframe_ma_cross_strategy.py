import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage as SMA, ExponentialMovingAverage as EMA, SmoothedMovingAverage, WeightedMovingAverage, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class iu_higher_timeframe_ma_cross_strategy(Strategy):
    def __init__(self):
        super(iu_higher_timeframe_ma_cross_strategy, self).__init__()

        self._risk_to_reward = self.Param("RiskToReward", 2.0) \
            .SetDisplay("RTR", "Risk to reward ratio", "Protection")

        self._ma1_candle_type = self.Param("Ma1CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("MA1 Timeframe", "Timeframe for first MA", "Moving Averages")

        self._ma1_length = self.Param("Ma1Length", 20) \
            .SetDisplay("MA1 Length", "Period for first MA", "Moving Averages")

        self._ma1_type = self.Param("Ma1Type", 1) \
            .SetDisplay("MA1 Type", "Type of first MA (0=SMA,1=EMA,2=Smoothed,3=Weighted,4=VWMA)", "Moving Averages")

        self._ma2_candle_type = self.Param("Ma2CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("MA2 Timeframe", "Timeframe for second MA", "Moving Averages")

        self._ma2_length = self.Param("Ma2Length", 50) \
            .SetDisplay("MA2 Length", "Period for second MA", "Moving Averages")

        self._ma2_type = self.Param("Ma2Type", 1) \
            .SetDisplay("MA2 Type", "Type of second MA (0=SMA,1=EMA,2=Smoothed,3=Weighted,4=VWMA)", "Moving Averages")

        self._ma1 = None
        self._ma2 = None
        self._prev_ma1 = None
        self._prev_ma2 = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._prev_low = None
        self._prev_high = None
        self._last_ma1_candle = None

    @property
    def risk_to_reward(self):
        return self._risk_to_reward.Value

    @property
    def ma1_candle_type(self):
        return self._ma1_candle_type.Value

    @property
    def ma1_length(self):
        return self._ma1_length.Value

    @property
    def ma2_candle_type(self):
        return self._ma2_candle_type.Value

    @property
    def ma2_length(self):
        return self._ma2_length.Value

    def _create_ma(self, ma_type, length):
        t = int(ma_type)
        if t == 0:
            ma = SMA()
        elif t == 1:
            ma = EMA()
        elif t == 2:
            ma = SmoothedMovingAverage()
        elif t == 3:
            ma = WeightedMovingAverage()
        elif t == 4:
            ma = VolumeWeightedMovingAverage()
        else:
            ma = SMA()
        ma.Length = length
        return ma

    def OnReseted(self):
        super(iu_higher_timeframe_ma_cross_strategy, self).OnReseted()
        self._ma1 = None
        self._ma2 = None
        self._prev_ma1 = None
        self._prev_ma2 = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._prev_low = None
        self._prev_high = None
        self._last_ma1_candle = None

    def OnStarted(self, time):
        super(iu_higher_timeframe_ma_cross_strategy, self).OnStarted(time)

        ma1_ind = self._create_ma(self._ma1_type.Value, self.ma1_length)
        ma2_ind = self._create_ma(self._ma2_type.Value, self.ma2_length)

        ma1_sub = self.SubscribeCandles(self.ma1_candle_type)
        ma1_sub.Bind(ma1_ind, self._process_ma1).Start()

        ma2_sub = self.SubscribeCandles(self.ma2_candle_type)
        ma2_sub.Bind(ma2_ind, self._process_ma2).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, ma1_sub)
            self.DrawIndicator(area, ma1_ind)
            self.DrawIndicator(area, ma2_ind)
            self.DrawOwnTrades(area)

    def _process_ma1(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        if self._last_ma1_candle is not None:
            self._prev_low = float(self._last_ma1_candle.LowPrice)
            self._prev_high = float(self._last_ma1_candle.HighPrice)
        self._last_ma1_candle = candle

        self._prev_ma1 = self._ma1
        self._ma1 = float(value)

        pos = self.Position
        if pos > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(pos)
                self._reset_protection()
            elif self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket(pos)
                self._reset_protection()
        elif pos < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(pos))
                self._reset_protection()
            elif self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket(abs(pos))
                self._reset_protection()

        if self._prev_ma1 is not None and self._prev_ma2 is not None and self._ma1 is not None and self._ma2 is not None and self.Position == 0:
            cross_up = self._prev_ma1 < self._prev_ma2 and self._ma1 > self._ma2
            cross_down = self._prev_ma1 > self._prev_ma2 and self._ma1 < self._ma2

            if cross_up:
                self.BuyMarket()
                self._entry_price = float(candle.ClosePrice)
                sl = self._prev_low if self._prev_low is not None else float(candle.LowPrice)
                self._stop_price = sl
                self._take_price = (self._entry_price - sl) * float(self.risk_to_reward) + self._entry_price
            elif cross_down:
                self.SellMarket()
                self._entry_price = float(candle.ClosePrice)
                sl = self._prev_high if self._prev_high is not None else float(candle.HighPrice)
                self._stop_price = sl
                self._take_price = self._entry_price - (sl - self._entry_price) * float(self.risk_to_reward)

    def _process_ma2(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        self._prev_ma2 = self._ma2
        self._ma2 = float(value)

    def _reset_protection(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return iu_higher_timeframe_ma_cross_strategy()
