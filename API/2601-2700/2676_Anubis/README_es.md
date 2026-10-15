# Estrategia Anubis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Anubis combina filtros de volatilidad y momentum en múltiples marcos temporales para capturar reversiones contra picos contratendencia fuertes. El asesor experto original de MetaTrader 5 usaba indicadores H4 para filtrar entradas y señales M15 para el timing. Esta conversión mantiene la misma estructura adaptando la lógica a la API de alto nivel de StockSharp y proporcionando telemetría de ejecución enriquecida.

## Lógica de la estrategia
- **Marcos temporales**
  - Marco temporal de señal principal: tipo de vela configurable (velas de 15 minutos por defecto).
  - Confirmación de marco temporal superior: velas fijas de 4 horas usadas para CCI y desviaciones estándar.
- **Indicadores**
  - *Commodity Channel Index (CCI)* en el marco temporal superior detecta extremos de sobrecompra/sobreventa.
  - *Dos desviaciones estándar* en el marco temporal superior ofrecen mediciones de volatilidad para dimensionar el take-profit.
  - *MACD* en el marco temporal de señal suministra confirmación de cruce de momentum.
  - *Average True Range (ATR)* en el marco temporal de señal define salidas por rango de vela anormal.
- **Criterios de entrada**
  - **Largo:** CCI cae por debajo de `-CciThreshold`, la línea principal del MACD cruza hacia arriba la línea de señal, y el histograma MACD anterior era negativo.
  - **Corto:** CCI sube por encima de `+CciThreshold`, la línea principal del MACD cruza hacia abajo la línea de señal, y el histograma MACD anterior era positivo.
  - La estrategia cierra opcionalmente una posición opuesta antes de apilar una nueva y aplica una distancia mínima de precio entre entradas consecutivas.
- **Gestión de la posición**
  - Se permiten hasta `MaxLongPositions` o `MaxShortPositions` entradas apiladas, cada una abierta con `TradeVolume` contratos.
  - Las distancias de stop-loss y take-profit se derivan de configuraciones basadas en pips y la volatilidad del marco temporal superior.
  - Una vez que el precio se mueve `BreakevenPips`, el stop protector se eleva al precio de entrada promedio.
- **Criterios de salida**
  - Stops duros: los niveles de stop-loss y take-profit se monitorean en cada vela cerrada.
  - Salidas por rango: las posiciones se cierran si el rango de la vela anterior supera `CloseAtrMultiplier × ATR`.
  - Salidas por momentum: las posiciones con suficiente beneficio se cierran cuando el momentum del MACD gira contra la operación y la ganancia supera `ThresholdPips`.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TradeVolume` | 1 | Tamaño de la orden para cada entrada. |
| `CciThreshold` | 80 | Nivel absoluto de CCI en el gráfico de 4 horas usado para detectar extremos. |
| `CciPeriod` | 11 | Longitud de retroceso de CCI en el marco temporal superior. |
| `StopLossPips` | 100 | Distancia de stop-loss expresada en pips. Establece 0 para deshabilitar el stop inicial. |
| `BreakevenPips` | 65 | Distancia de beneficio en pips antes de mover el stop al punto de equilibrio. |
| `ThresholdPips` | 28 | Colchón de beneficio adicional requerido antes de activar las salidas basadas en MACD. |
| `TakeStdMultiplier` | 2.9 | Multiplicador aplicado a la desviación estándar lenta al calcular la distancia de take-profit. |
| `CloseAtrMultiplier` | 2 | Multiplicador del ATR del marco temporal de señal usado para salidas basadas en rango. |
| `SpacingPips` | 20 | Distancia mínima de precio entre entradas consecutivas en la misma dirección. |
| `MaxLongPositions` | 2 | Número máximo de entradas largas simultáneas. |
| `MaxShortPositions` | 2 | Número máximo de entradas cortas simultáneas. |
| `MacdFastLength` | 20 | Longitud de EMA rápida para MACD en el marco temporal de señal. |
| `MacdSlowLength` | 50 | Longitud de EMA lenta para MACD en el marco temporal de señal. |
| `MacdSignalLength` | 2 | Longitud de suavizado de la señal para MACD. |
| `AtrLength` | 12 | Período de retroceso de ATR en el marco temporal de señal. |
| `StdFastLength` | 20 | Período para la desviación estándar rápida (usado para diagnósticos). |
| `StdSlowLength` | 30 | Período para la desviación estándar lenta que impulsa la distancia de take-profit. |
| `CandleType` | Velas de 15m | Marco temporal principal usado para los cálculos de MACD y ATR. |

## Notas de trading
- El marco temporal superior está fijado en cuatro horas; ajusta `CandleType` si deseas sincronizar el marco temporal de señal principal con diferentes mercados.
- Dado que StockSharp agrega posiciones netas por defecto, la exposición larga y corta no se mantiene simultáneamente; una señal opuesta aplanará la posición abierta antes de colocar la nueva orden.
- El cálculo de la desviación estándar sigue la implementación de StockSharp. La longitud lenta aproxima la desviación basada en EMA de la versión MQL original.
- Asegúrate de que el instrumento seleccionado expone un `PriceStep` válido para que los parámetros basados en pips se traduzcan con precisión en distancias de precio.
