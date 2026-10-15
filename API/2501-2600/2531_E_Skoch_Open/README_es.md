# Estrategia E-Skoch-Open (Port de StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **E-Skoch-Open** replica el asesor experto original de MetaTrader 5 que opera un patrón simple de tres velas. La implementación de StockSharp procesa velas completadas, evalúa reversiones de momentum en los cierres recientes y abre una nueva posición cuando aparece la configuración requerida. El riesgo se controla con offsets de stop-loss/take-profit medidos en puntos ajustados (pips) y un objetivo de crecimiento del capital que puede aplanar todas las posiciones abiertas. El dimensionamiento de posición sigue un esquema de martingala: después de una operación perdedora el siguiente tamaño de orden se multiplica por 1.6, mientras que las operaciones rentables reinician el volumen al valor inicial.

## Lógica de trading
1. Trabaja con el marco temporal definido por el parámetro `CandleType` (por defecto: 1 hora).
2. Espera hasta que haya al menos tres velas completadas disponibles.
3. **Configuración de compra**: si `Close[n-3] > Close[n-2]` y `Close[n-1] < Close[n-2]`, y las operaciones largas están habilitadas.
4. **Configuración de venta**: si `Close[n-3] > Close[n-2]` y `Close[n-2] < Close[n-1]`, y las operaciones cortas están habilitadas.
5. Si `CloseOnOppositeSignal` está habilitado, recibir una señal opuesta cierra la posición existente inmediatamente y omite nuevas entradas para la barra actual.
6. Para cada nueva posición la estrategia adjunta niveles estáticos de stop-loss y take-profit calculados desde el cierre actual y la distancia configurada en puntos ajustados. Cuando el máximo/mínimo de una vela completada alcanza uno de estos niveles la posición se cierra.
7. La estrategia verifica continuamente el capital de la cuenta. Cuando el crecimiento del capital relativo al último momento plano supera `TargetProfitPercent`, se cierran todas las posiciones.
8. Después de que una operación cierra con pérdida, el siguiente volumen de orden se multiplica por 1.6. Después de una operación rentable el volumen vuelve al tamaño inicial. Los volúmenes se normalizan usando las restricciones del instrumento (`VolumeStep`, `VolumeMin`, `VolumeMax`).

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal usado para la detección de patrones. Funciona con cualquier vela soportada por StockSharp. |
| `InitialOrderVolume` | Tamaño de lote base para la primera operación en una secuencia (por defecto: 0.01). |
| `StopLossPoints` | Distancia del stop-loss expresada en puntos ajustados. Para instrumentos de 5 o 3 dígitos el valor del punto es `PriceStep * 10`, de lo contrario `PriceStep`. |
| `TakeProfitPoints` | Distancia del take-profit usando la misma convención de punto ajustado. |
| `EnableBuySignals` / `EnableSellSignals` | Activar o desactivar entradas largas o cortas. |
| `MaxBuyTrades` / `MaxSellTrades` | Número máximo de operaciones consecutivas permitidas por dirección (`-1` elimina el límite). El port mantiene como máximo una posición por dirección por defecto. |
| `TargetProfitPercent` | Ganancia porcentual del capital que desencadena el cierre de todas las posiciones (por defecto: 1.2%). |
| `CloseOnOppositeSignal` | Si está habilitado, una señal en la dirección opuesta fuerza una posición plana antes de considerar nuevas operaciones. |

## Notas de gestión de riesgo
- Los niveles de stop-loss y take-profit se simulan desde los extremos de las velas. En trading en vivo la ejecución intrabar puede diferir de MetaTrader donde las órdenes protectoras están registradas en el servidor.
- El multiplicador de martingala (1.6) puede hacer crecer los volúmenes rápidamente durante drawdowns. Asegurarse de que los límites del instrumento (`VolumeMax`) y el capital del portafolio puedan soportar la posición más grande esperada.
- El bloqueo de ganancias basado en capital funciona solo cuando la información del portafolio está disponible a través de `Portfolio.CurrentValue`.

## Consejos de uso
- Ajustar `CandleType` para que coincida con el marco temporal usado en el asesor experto original.
- Ajustar `StopLossPoints` / `TakeProfitPoints` a la volatilidad del instrumento; son basados en pips gracias al cálculo del punto ajustado.
- Deshabilitar una dirección si el hedging no está permitido por el broker o política de riesgo.
- Vigilar el objetivo de capital y la configuración de martingala al ejecutar pruebas largas para evitar liquidaciones inesperadas.
