# La Estrategia MasterMind
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza el oscilador Stochastic y Williams %R para capturar condiciones extremas de sobrecompra y sobreventa.

## Descripción general
La estrategia monitorea dos indicadores de momentum:
- **Stochastic Oscillator** con longitud base 100 y suavizado 3/3.
- **Williams %R** con longitud 100.

Se abre una posición larga cuando el valor %D del Stochastic cae por debajo de 3 mientras Williams %R está por debajo de -99.9, indicando un mercado sobrevendido.
Se abre una posición corta cuando el %D del Stochastic sube por encima de 97 y Williams %R sube por encima de -0.1, señalando un mercado sobrecomprado.

Tras entrar en una operación, el algoritmo gestiona el riesgo mediante stop loss, take profit, trailing stop y movimiento opcional de break-even.

## Parámetros
- `StochasticLength` – período para los cálculos de Stochastic y Williams %R.
- `StopLoss` – distancia desde el precio de entrada para el stop loss en puntos.
- `TakeProfit` – distancia del take profit en puntos.
- `TrailingStop` – distancia de activación del trailing en puntos.
- `TrailingStep` – paso del trailing stop en puntos.
- `BreakEven` – ganancia en puntos en la que el stop se mueve al precio de entrada.
- `CandleType` – marco temporal de velas para los cálculos de la estrategia.

## Indicadores
- `StochasticOscillator`
- `WilliamsR`

## Reglas de trading
1. **Comprar** cuando `%D < 3` y `Williams %R < -99.9`.  
2. **Vender** cuando `%D > 97` y `Williams %R > -0.1`.  
3. Tras la entrada, aplicar stop loss y take profit.  
4. Mover el stop al break-even cuando el precio avanza `BreakEven`.  
5. Activar el trailing stop cuando el precio se mueve `TrailingStop`, desplazando en `TrailingStep`.

## Notas
La estrategia utiliza la API de alto nivel de StockSharp y está pensada como ejemplo educativo.
