# Estrategia Exp Sinewave2 X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Exp Sinewave2 X2 es una estrategia de seguimiento de tendencia multitemporal inspirada en el análisis Sinewave de John Ehlers. El filtro de marco temporal superior define la dirección dominante, mientras que el marco temporal inferior proporciona desencadenantes precisos de entrada y salida. Todos los cálculos usan el indicador Sinewave2 reconstruido, que internamente depende del módulo adaptativo CyclePeriod.

## Indicadores
- **Sinewave2 de marco temporal superior (línea lead vs. línea sine)** – detecta sesgo alcista o bajista usando el cruce de la sine lead sobre el componente sine principal.
- **Sinewave2 de marco temporal inferior** – monitorea los eventos de cruce más recientes para desencadenar trades alineados con la dirección del marco temporal superior.

## Lógica de trading
1. **Filtro de tendencia**
   - Calcular Sinewave2 en el marco temporal superior.
   - Evaluar las líneas lead y main `SignalBarHigh` barras atrás.
   - La tendencia es alcista si `Lead > Sine`, bajista si `Lead < Sine`, de lo contrario neutral.
2. **Señales de entrada**
   - Esperar una vela finalizada en el marco temporal inferior.
   - Recuperar los valores lead y sine en los desplazamientos definidos por `SignalBarLow` (actual) y `SignalBarLow + 1` (anterior).
   - Entrada larga: el cruce anterior fue hacia abajo (`Lead > Sine` anteriormente, `Lead <= Sine` ahora) mientras la tendencia del marco temporal superior es alcista y `EnableBuyOpen` está habilitado.
   - Entrada corta: el cruce anterior fue hacia arriba (`Lead < Sine` anteriormente, `Lead >= Sine` ahora) mientras la tendencia del marco temporal superior es bajista y `EnableSellOpen` está habilitado.
3. **Reglas de salida**
   - Los booleanos de salida del marco temporal inferior `EnableBuyCloseLower` y `EnableSellCloseLower` cierran posiciones en cruces opuestos.
   - Los booleanos de salida del marco temporal superior `EnableBuyCloseTrend` y `EnableSellCloseTrend` cierran posiciones inmediatamente siempre que la tendencia principal cambie contra la dirección abierta.
   - El stop loss protector y el take profit se evalúan en cada vela usando los máximos/mínimos intrabarra y las distancias `StopLossPoints` / `TakeProfitPoints` expresadas en pasos de precio.
4. **Gestión de riesgo**
   - Los reversales de posición dimensionan las nuevas órdenes como `Volume + |Position|` para aplanar la posición existente antes de establecer la nueva.
   - Después de cada entrada `SetRiskLevels` recalcula los precios absolutos de stop/objetivo usando `Security.PriceStep` (respaldo 1 cuando no está disponible).

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `AlphaHigh` | Factor alpha para el filtro Sinewave2 de marco temporal superior. |
| `AlphaLow` | Factor alpha para el disparador Sinewave2 de marco temporal inferior. |
| `SignalBarHigh` | Número de barras atrás en el marco temporal superior usadas para leer el estado de tendencia. |
| `SignalBarLow` | Número de barras atrás en el marco temporal inferior usadas para leer estados de cruce. |
| `EnableBuyOpen` / `EnableSellOpen` | Permitir entradas largas/cortas desde señales del marco temporal inferior. |
| `EnableBuyCloseTrend` / `EnableSellCloseTrend` | Forzar salidas cuando el marco temporal superior cambia contra la posición. |
| `EnableBuyCloseLower` / `EnableSellCloseLower` | Cerrar posiciones en cruces opuestos del marco temporal inferior. |
| `StopLossPoints` | Distancia del stop-loss expresada en pasos de precio del instrumento. |
| `TakeProfitPoints` | Distancia del take-profit expresada en pasos de precio del instrumento. |
| `HigherCandleType` / `LowerCandleType` | Tipos de datos de velas (marcos temporales) para los flujos de filtro y disparador. |

## Notas
- La estrategia procesa solo velas finalizadas e ignora actualizaciones parciales.
- La implementación adaptativa de Sinewave2 usa el algoritmo CyclePeriod original para mantenerse fiel a la versión MQL.
- Cuando los tipos de vela superior e inferior son idénticos, ambos indicadores comparten una sola suscripción de velas para evitar solicitudes de datos redundantes.
- Ajuste `Volume` en la `Strategy` base para controlar el tamaño de trade antes del despliegue.
