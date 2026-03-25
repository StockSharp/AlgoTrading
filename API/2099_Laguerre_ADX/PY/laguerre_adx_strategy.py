import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class laguerre_adx_strategy(Strategy):
    def __init__(self):
        super(laguerre_adx_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
        self._gamma = self.Param("Gamma", 0.764) \
            .SetDisplay("Gamma", "Laguerre smoothing factor", "Indicators")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._adx = None
        self._prev_up = 0.0
        self._prev_down = 0.0
        self._l0_up = 0.0
        self._l1_up = 0.0
        self._l2_up = 0.0
        self._l3_up = 0.0
        self._l0_down = 0.0
        self._l1_down = 0.0
        self._l2_down = 0.0
        self._l3_down = 0.0
        self._is_initialized = False

    @property
    def adx_period(self):
        return self._adx_period.Value
    @property
    def gamma(self):
        return self._gamma.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(laguerre_adx_strategy, self).OnReseted()
        self._adx = None
        self._prev_up = 0.0
        self._prev_down = 0.0
        self._l0_up = 0.0
        self._l1_up = 0.0
        self._l2_up = 0.0
        self._l3_up = 0.0
        self._l0_down = 0.0
        self._l1_down = 0.0
        self._l2_down = 0.0
        self._l3_down = 0.0
        self._is_initialized = False

    def OnStarted(self, time):
        super(laguerre_adx_strategy, self).OnStarted(time)
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.adx_period
        self.Indicators.Add(self._adx)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _laguerre_rsi(self, value, l_state):
        g = float(self.gamma)
        l0_prev = l_state[0]
        l1_prev = l_state[1]
        l2_prev = l_state[2]
        l_state[0] = (1.0 - g) * value + g * l_state[0]
        l_state[1] = -g * l_state[0] + l0_prev + g * l_state[1]
        l_state[2] = -g * l_state[1] + l1_prev + g * l_state[2]
        l_state[3] = -g * l_state[2] + l2_prev + g * l_state[3]
        cu = 0.0
        cd = 0.0
        if l_state[0] >= l_state[1]:
            cu = l_state[0] - l_state[1]
        else:
            cd = l_state[1] - l_state[0]
        if l_state[1] >= l_state[2]:
            cu += l_state[1] - l_state[2]
        else:
            cd += l_state[2] - l_state[1]
        if l_state[2] >= l_state[3]:
            cu += l_state[2] - l_state[3]
        else:
            cd += l_state[3] - l_state[2]
        return 0.0 if cu + cd == 0 else cu / (cu + cd)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        cv = CandleIndicatorValue(self._adx, candle)
        adx_result = self._adx.Process(cv)
        if not adx_result.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        plus = adx_result.Dx.Plus
        minus = adx_result.Dx.Minus
        plus = float(plus) if plus is not None else 0.0
        minus = float(minus) if minus is not None else 0.0

        up_state = [self._l0_up, self._l1_up, self._l2_up, self._l3_up]
        up = self._laguerre_rsi(plus, up_state)
        self._l0_up, self._l1_up, self._l2_up, self._l3_up = up_state

        down_state = [self._l0_down, self._l1_down, self._l2_down, self._l3_down]
        down = self._laguerre_rsi(minus, down_state)
        self._l0_down, self._l1_down, self._l2_down, self._l3_down = down_state

        if not self._is_initialized:
            self._prev_up = up
            self._prev_down = down
            self._is_initialized = True
            return

        if self._prev_up <= self._prev_down and up > down and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_up >= self._prev_down and up < down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_up = up
        self._prev_down = down

    def CreateClone(self):
        return laguerre_adx_strategy()
