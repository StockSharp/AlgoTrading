import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, CommodityChannelIndex, SimpleMovingAverage, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class exp_oracle_strategy(Strategy):
    # Algorithm modes
    BREAKDOWN = 0
    TWIST = 1
    DISPOSITION = 2

    def __init__(self):
        super(exp_oracle_strategy, self).__init__()
        self._oracle_period = self.Param("OraclePeriod", 55) \
            .SetGreaterThanZero() \
            .SetDisplay("Oracle Period", "Oracle period", "Parameters")
        self._smooth = self.Param("Smooth", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Smooth", "Smoothing length", "Parameters")
        self._mode = self.Param("Mode", 0) \
            .SetDisplay("Mode", "Signal algorithm (0=Breakdown, 1=Twist, 2=Disposition)", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum number of bars between entries", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type", "Parameters")
        self._allow_buy = self.Param("AllowBuy", True) \
            .SetDisplay("Allow Buy", "Enable long entries", "Parameters")
        self._allow_sell = self.Param("AllowSell", True) \
            .SetDisplay("Allow Sell", "Enable short entries", "Parameters")

        self._rsi = None
        self._cci = None
        self._sma = None
        self._rsi_buf = [0.0] * 4
        self._cci_buf = [0.0] * 4
        self._prev_signal = 0.0
        self._prev_prev_signal = 0.0
        self._prev_oracle = 0.0
        self._bars_since_trade = 0

    @property
    def oracle_period(self):
        return self._oracle_period.Value
    @property
    def smooth(self):
        return self._smooth.Value
    @property
    def mode(self):
        return self._mode.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def allow_buy(self):
        return self._allow_buy.Value
    @property
    def allow_sell(self):
        return self._allow_sell.Value

    def OnReseted(self):
        super(exp_oracle_strategy, self).OnReseted()
        self._prev_signal = 0.0
        self._prev_prev_signal = 0.0
        self._prev_oracle = 0.0
        self._bars_since_trade = self.cooldown_bars
        self._rsi_buf = [0.0] * 4
        self._cci_buf = [0.0] * 4

    def OnStarted(self, time):
        super(exp_oracle_strategy, self).OnStarted(time)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.oracle_period
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.oracle_period
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.smooth
        self.StartProtection(None, None)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        rsi_inp = DecimalIndicatorValue(self._rsi, candle.ClosePrice, candle.OpenTime)
        rsi_inp.IsFinal = True
        rsi_result = self._rsi.Process(rsi_inp)
        cci_inp = CandleIndicatorValue(self._cci, candle)
        cci_result = self._cci.Process(cci_inp)

        if not rsi_result.IsFormed or not cci_result.IsFormed:
            return

        rsi_val = float(rsi_result)
        cci_val = float(cci_result)

        # shift buffers
        self._rsi_buf[3] = self._rsi_buf[2]
        self._rsi_buf[2] = self._rsi_buf[1]
        self._rsi_buf[1] = self._rsi_buf[0]
        self._rsi_buf[0] = rsi_val

        self._cci_buf[3] = self._cci_buf[2]
        self._cci_buf[2] = self._cci_buf[1]
        self._cci_buf[1] = self._cci_buf[0]
        self._cci_buf[0] = cci_val

        # compute Oracle value
        div0 = self._cci_buf[0] - self._rsi_buf[0]
        d_div = div0
        div1 = self._cci_buf[1] - self._rsi_buf[1] - d_div
        d_div += div1
        div2 = self._cci_buf[2] - self._rsi_buf[2] - d_div
        d_div += div2
        div3 = self._cci_buf[3] - self._rsi_buf[3] - d_div

        max_val = max(div0, div1, div2, div3)
        min_val = min(div0, div1, div2, div3)
        oracle = max_val + min_val

        # smooth to get signal
        sma_inp = DecimalIndicatorValue(self._sma, Decimal(oracle), candle.OpenTime)
        sma_inp.IsFinal = True
        signal_result = self._sma.Process(sma_inp)
        if not signal_result.IsFormed:
            return

        signal = float(signal_result)
        self._bars_since_trade += 1

        m = self.mode
        if m == self.BREAKDOWN:
            if self.allow_buy and self._bars_since_trade >= self.cooldown_bars and self._prev_signal <= 0.0 and signal > 0.0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._bars_since_trade = 0
            elif self.allow_sell and self._bars_since_trade >= self.cooldown_bars and self._prev_signal >= 0.0 and signal < 0.0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._bars_since_trade = 0
        elif m == self.TWIST:
            if self.allow_buy and self._bars_since_trade >= self.cooldown_bars and self._prev_prev_signal > self._prev_signal and signal >= self._prev_signal and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._bars_since_trade = 0
            elif self.allow_sell and self._bars_since_trade >= self.cooldown_bars and self._prev_prev_signal < self._prev_signal and signal <= self._prev_signal and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._bars_since_trade = 0
        elif m == self.DISPOSITION:
            if self.allow_buy and self._bars_since_trade >= self.cooldown_bars and self._prev_signal < self._prev_oracle and signal >= oracle and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._bars_since_trade = 0
            elif self.allow_sell and self._bars_since_trade >= self.cooldown_bars and self._prev_signal > self._prev_oracle and signal <= oracle and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._bars_since_trade = 0

        self._prev_prev_signal = self._prev_signal
        self._prev_signal = signal
        self._prev_oracle = oracle

    def CreateClone(self):
        return exp_oracle_strategy()
