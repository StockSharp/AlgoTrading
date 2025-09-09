// This source code is subject to the terms of the Mozilla Public License 2.0 at https://mozilla.org/MPL/2.0/
// © mattzab v126

//@version=4
study(title="Bias And Sentiment Strength", shorttitle="BASS")
explain_scale = input(1, title="Change the scaling values below if some of your indicators aren't showing up (that happens with FOREX). Some may need extreme values- Sometimes 10,000 can help, or even very small numbers like 0.0001. It'll be easiest to click the style tab and turn things on & off to find out what's distorting the scale, then adjust that one up or down by moving the decimal around and adding zeros. Multiples of 10 tend to help.. Experiment with each until you see changes. This variable doesn't change anything, it simply explains scaling.")
macd_scale = input(1, defval=1, minval=0, step=0.1, title="MACD Scaling")
rsi_scale = input(1, defval=1, minval=0, step=0.1, title="RSI Scaling")
stoch_scale = input(1, defval=1, minval=0, step=0.1, title="Slow Stochastic Scaling")
ao_scale = input(1, defval=1, minval=0, step=0.1, title="Awesome Oscillator Scaling")
gator_scale = input(1, defval=1, minval=0, step=0.1, title="Alligator Scaling")
volume_scale = input(1, defval=1, minval=0, step=0.1, title="Volume Bias Scaling")

// MACD
fast_length = 12
slow_length = 26
src = close
signal_length = 9
sma_source = false
sma_signal = false
fast_ma = sma_source ? sma(src, fast_length) : ema(src, fast_length)
slow_ma = sma_source ? sma(src, slow_length) : ema(src, slow_length)
macd = fast_ma - slow_ma
signal = sma_signal ? sma(macd, signal_length) : ema(macd, signal_length)
macd_hist = ((macd - signal)*2)*macd_scale
plot(macd_hist, title="MACD Histogram", style=plot.style_area, color=(macd_hist>=0 ? color.blue : color.red ), transp=70 )


// RSI
rsi_src = close, rsi_len = 14
up = rma(max(change(rsi_src), 0), rsi_len)
down = rma(-min(change(rsi_src), 0), rsi_len)
rsi = ((down == 0 ? 100 : up == 0 ? 0 : 100 - (100 / (1 + up / down))-50)/5)*rsi_scale
plot(rsi, title="RSI Histogram", style=plot.style_area, color=(rsi<=0 ? color.red : color.blue ), transp=70 )


// SLOW STOCHASTIC
periodK = 21
periodD = 14
smoothK = 14
k = (sma(stoch(close, high, low, periodK), smoothK)-50)/10
d = sma(k, periodD)
stoch_hist = ((k - d)*1.5)*stoch_scale
plot(stoch_hist, title="Stochastic Histogram", style=plot.style_area, color=(stoch_hist<=0 ? color.red : color.blue ), transp=70 )


// AWESOME OSCILLATOR
ao = ((sma(hl2,5) - sma(hl2,34))*.6)*ao_scale
plot(ao, title="Awesome Oscillator Histogram", color = (ao <= 0 ? color.red : color.blue), style=plot.style_area, transp=70)


// ALLIGATOR OSCILLATOR
smma(gator_src, gator_length) =>
    smma =  0.0
    smma := na(smma[1]) ? sma(gator_src, gator_length) : (smma[1] * (gator_length - 1) + gator_src) / gator_length
    smma
jawLength = 13
teethLength = 8
lipsLength = 5
jawOffset = 8
teethOffset = 5
lipsOffset = 3
jaw = smma(hl2, jawLength)
teeth = smma(hl2, teethLength)
lips = smma(hl2, lipsLength)
average = ((lips - teeth) + (teeth - jaw))*gator_scale
plot(average, title="Alligator Hunger Histogram", color=(average >= 0 ? color.blue : color.red), style=plot.style_area, transp=70)


// VOLUME BIAS
volumebias_src = input(hlc3, title="Volume Bias Source"), volumebias_len = input(30, title="Volume Bias Length", minval=1)
volumebias_vwma = vwma(volumebias_src, volumebias_len)
volumebias_sma = sma(volumebias_src, volumebias_len)
volumebias_hist = (volumebias_vwma - volumebias_sma)*volume_scale
plot(volumebias_hist, color=(volumebias_hist >= 0 ? color.purple : color.purple), title="Volume Bias", style=plot.style_histogram, linewidth=5, transp=50)


plot(0, color=color.black, transp=0, title="Zero")