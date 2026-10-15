# Estrategia de Largo Post-Apertura con ATR Stop Loss y Take Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra en una posición larga durante la apertura del mercado tras un rompimiento de resistencia mientras el precio permanece cerca de la banda media de Bollinger. Utiliza filtros EMA, RSI, ADX y ATR, y sale mediante stop loss y take profit basados en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Rompimiento por encima de la resistencia reciente durante la apertura del mercado, precio cerca de la banda media de Bollinger, RSI por encima del umbral, ADX por encima del umbral, tendencia de corto plazo alcista y sin retroceso.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Stop loss o take profit basado en ATR alcanzado.
- **Stops**:
  - Stop loss y take profit basados en ATR.
- **Valores predeterminados**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
  - `SessionStart` = 09:30 UTC
  - `SessionDuration` = TimeSpan.FromHours(1)
  - `ResistancePeriod` = 5
  - `BasisProximity` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Long
  - Indicadores: Bollinger Bands, EMA, RSI, ADX, ATR
  - Stops: ATR
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Cálculo y ejecución

Las señales usan velas terminadas con todos los indicadores formados. Resistencia es el máximo de las `ResistancePeriod` velas anteriores, sin la actual; la entrada requiere `Close > resistance`. La ventana usa la hora UTC de apertura de la vela: inicio incluido, final excluido, con cruce de medianoche. Configure `SessionStart` para la apertura del instrumento.

La base Bollinger es la SMA de cierres incluyendo la vela actual; desviación poblacional. Proximidad: `abs(Close - basis) <= (UpperBand - basis) * BasisProximity`. Se requieren `EMA corta > EMA larga` y `Close > EMA corta`. Sin retroceso significa `Close >= Open` en la vela de señal. RSI y ADX deben superar estrictamente sus umbrales.

Compra `Volume` solo sin posición ni orden pendiente. Wilder ATR de entrada fija las distancias SL y TP. La protección local nativa sitúa los niveles debajo/encima del precio real de ejecución (ponderado por volumen para ejecuciones parciales), observa best bid Level1 y precios de velas y cierra todo el largo restante. Las salidas siguen activas fuera de la ventana. Cambios posteriores de ATR no mueven los niveles. Sin cortos, salidas por cruces EMA ni espera.
