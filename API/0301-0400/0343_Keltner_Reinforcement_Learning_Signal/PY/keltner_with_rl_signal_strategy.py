import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal, Array, Double
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class NeuralQModel:
    """Four inputs, eight trainable tanh hidden neurons, three linear Q outputs."""

    def __init__(self, seed, learning_rate, discount, exploration):
        if int(seed) < 0:
            raise ValueError("RandomSeed must be nonnegative")
        for value in (learning_rate, discount, exploration):
            if not Double.IsFinite(float(value)) or value < 0 or value > 1:
                raise ValueError("LearningRate, DiscountFactor and Exploration must be in [0, 1]")
        self._learning_rate = float(learning_rate)
        self._discount = float(discount)
        self._exploration = float(exploration)
        self._random_state = int(seed)
        self.Updates = 0
        self._hidden = [[(self._next_random() - 0.5) * 0.2 for _ in range(5)] for _ in range(8)]
        self._output = [[(self._next_random() - 0.5) * 0.2 for _ in range(9)] for _ in range(3)]

    def _next_random(self):
        # Identical unsigned 32-bit generator in C# and Python.
        self._random_state = (1664525 * self._random_state + 1013904223) & 0xffffffff
        return self._random_state / 4294967296.0

    def _hidden_values(self, features):
        if len(features) != 4 or any(not Double.IsFinite(float(value)) for value in features):
            raise ValueError("Four finite features are required")
        values = []
        for neuron in range(8):
            total = self._hidden[neuron][4]
            for feature in range(4):
                total += self._hidden[neuron][feature] * float(features[feature])
            values.append(float(Math.Tanh(total)))
        return values

    def Predict(self, features):
        hidden = self._hidden_values(features)
        values = []
        for action in range(3):
            total = self._output[action][8]
            for neuron in range(8):
                total += self._output[action][neuron] * hidden[neuron]
            values.append(total)
        return Array[Double](values)

    def SelectAction(self, features):
        values = self.Predict(features)
        if self._next_random() < self._exploration:
            return int(self._next_random() * 3)
        best = 0
        for action in range(1, 3):
            if values[action] > values[best]:
                best = action
        return best

    def Learn(self, state, action, reward, next_state):
        action = int(action)
        reward = float(reward)
        if action < 0 or action > 2 or not Double.IsFinite(reward):
            raise ValueError("A valid action and finite reward are required")
        hidden = self._hidden_values(state)
        error = max(-1.0, min(1.0, reward + self._discount * max(self.Predict(next_state)) - self.Predict(state)[action]))
        if self._learning_rate == 0:
            return
        previous_output = list(self._output[action])
        for neuron in range(8):
            self._output[action][neuron] += self._learning_rate * error * hidden[neuron]
            gradient = error * previous_output[neuron] * (1.0 - hidden[neuron] * hidden[neuron])
            for feature in range(4):
                self._hidden[neuron][feature] += self._learning_rate * gradient * float(state[feature])
            self._hidden[neuron][4] += self._learning_rate * gradient
        self._output[action][8] += self._learning_rate * error
        self.Updates += 1

    def GetWeights(self):
        return Array[Double]([value for layer in self._hidden + self._output for value in layer])


class keltner_with_rl_signal_strategy(Strategy):
    """Keltner breakouts filtered by an online neural Q learner."""

    def __init__(self):
        super(keltner_with_rl_signal_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "Period for the exponential moving average", "Keltner Settings")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period for the average true range", "Keltner Settings")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channels", "Keltner Settings")
        self._stop_loss_atr = self.Param("StopLossAtr", 2.0).SetGreaterThanZero().SetDisplay("Stop Loss (ATR)", "Stop Loss in multiples of ATR", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 48).SetNotNegative().SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._learning_rate = self.Param("LearningRate", 0.05).SetNotNegative()
        self._discount_factor = self.Param("DiscountFactor", 0.9).SetNotNegative()
        self._exploration = self.Param("Exploration", 0.1).SetNotNegative()
        self._random_seed = self.Param("RandomSeed", 42).SetNotNegative()
        self._reset_state()

    def _reset_state(self):
        self.LearningModel = None
        self.CurrentSignal = 0
        self._previous_features = None
        self._previous_action = 0
        self._previous_price = Decimal(0)
        self._previous_atr = Decimal(0)
        self._entry_price = Decimal(0)
        self._cooldown_remaining = 0
        self._previous_above_upper = False
        self._previous_below_lower = False
        self._pending_order = None

    def CreateLearningModel(self):
        return NeuralQModel(int(self._random_seed.Value), float(self._learning_rate.Value),
                            float(self._discount_factor.Value), float(self._exploration.Value))

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(keltner_with_rl_signal_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(keltner_with_rl_signal_strategy, self).OnStarted2(time)
        self._reset_state()
        self.LearningModel = self.CreateLearningModel()
        ema = ExponentialMovingAverage()
        ema.Length = int(self._ema_period.Value)
        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.ProcessCandle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, middle_band, atr):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading() or atr <= 0:
            return
        price = candle.ClosePrice
        features = Array[Double]([
            float(Math.Tanh(float((price - middle_band) / atr))),
            0.0 if self._previous_price == 0 else float(Math.Tanh(float((price - self._previous_price) / atr))),
            0.0 if self._previous_atr == 0 else float(Math.Tanh(float((atr - self._previous_atr) / self._previous_atr))),
            float(Math.Tanh(float((price - candle.OpenPrice) / atr))),
        ])
        # Only the next completed bar supplies the reward for the preceding action.
        # Hypothetical one-bar return, not actual fill PnL.
        if self._previous_features is not None:
            direction = 1.0 if self._previous_action == 1 else -1.0 if self._previous_action == 2 else 0.0
            reward = max(-1.0, min(1.0, direction * float((price - self._previous_price) / self._previous_atr)))
            self.LearningModel.Learn(self._previous_features, self._previous_action, reward, features)
        self.CurrentSignal = self.LearningModel.SelectAction(features)
        self._previous_features = features
        self._previous_action = self.CurrentSignal
        self._previous_price = price
        self._previous_atr = atr

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self._pending_order is not None and self._pending_order.State in (OrderStates.Done, OrderStates.Failed):
            self._pending_order = None
        if self.Position == 0:
            self._entry_price = Decimal(0)

        offset = Decimal(self._atr_multiplier.Value) * atr
        above = price > middle_band + offset
        below = price < middle_band - offset
        buy = not self._previous_above_upper and above and self.CurrentSignal == 1
        sell = not self._previous_below_lower and below and self.CurrentSignal == 2
        self._previous_above_upper = above
        self._previous_below_lower = below
        if self._pending_order is not None:
            return
        stop_offset = Decimal(self._stop_loss_atr.Value) * atr
        if self._cooldown_remaining == 0 and buy and self.Position <= 0:
            self._submit(Sides.Buy, self.Volume + Math.Abs(self.Position), price)
        elif self._cooldown_remaining == 0 and sell and self.Position >= 0:
            self._submit(Sides.Sell, self.Volume + Math.Abs(self.Position), price)
        elif self.Position > 0 and (price < middle_band or (self._entry_price > 0 and price < self._entry_price - stop_offset)):
            self._submit(Sides.Sell, Math.Abs(self.Position), Decimal(0))
        elif self.Position < 0 and (price > middle_band or (self._entry_price > 0 and price > self._entry_price + stop_offset)):
            self._submit(Sides.Buy, Math.Abs(self.Position), Decimal(0))

    def _submit(self, side, volume, entry_price):
        self._entry_price = entry_price
        self._cooldown_remaining = int(self._cooldown_bars.Value)
        self._pending_order = self.BuyMarket(volume) if side == Sides.Buy else self.SellMarket(volume)

    def CreateClone(self):
        return keltner_with_rl_signal_strategy()
