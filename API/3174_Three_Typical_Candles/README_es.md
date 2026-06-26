# Estrategia de Three Typical Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Three Typical Candles** recrea el Asesor Experto de MetaTrader "Three Typical Candles" dentro de la API de alto nivel de StockSharp. El sistema observa el precio típico de las tres últimas velas completadas y opera cuando detecta una secuencia estrictamente monótona. El precio típico se define como la media aritmética del máximo, mínimo y cierre de una vela. Cuando las tres velas finalizadas más recientes forman una secuencia creciente de precios típicos, la estrategia entra en largo. A la inversa, una secuencia decreciente desencadena una entrada en corto.

El port sigue de cerca la lógica MQL5 original:
- Las señales se evalúan solo una vez por vela finalizada para evitar ruido intrabarra.
- Una ventana de trading configurable puede deshabilitar el trading fuera de las horas seleccionadas y fuerza la estrategia a posición plana cuando el filtro está activo.
- Las posiciones opuestas se cierran antes de abrir una nueva, por lo que la estrategia nunca mantiene ambas direcciones al mismo tiempo.
- El volumen de las órdenes espeja el EA fuente usando un tamaño de lote fijo, respetando el paso de volumen de la bolsa así como las restricciones de volumen mínimo y máximo informadas por el instrumento.

## Reglas de trading
1. **Detección de señal**
   - Calcular el precio típico `Tp = (High + Low + Close) / 3` para cada vela finalizada.
   - Rastrear los dos valores típicos anteriores. Una vez disponibles tres valores, verificar una secuencia estrictamente creciente o estrictamente decreciente.
2. **Entrada en largo**
   - Si `Tp[-2] < Tp[-1] < Tp[0]` (tres precios típicos crecientes) y la posición actual no es larga, la estrategia cierra cualquier exposición corta y envía una orden de compra a mercado.
3. **Entrada en corto**
   - Si `Tp[-2] > Tp[-1] > Tp[0]` (tres precios típicos decrecientes) y la posición actual no es corta, la estrategia cierra cualquier exposición larga y envía una orden de venta a mercado.
4. **Control de tiempo**
   - Cuando el filtro de tiempo opcional está habilitado, la estrategia evalúa la señal solo cuando el tiempo de apertura de la vela cae dentro de la sesión de trading configurada. Fuera de esa ventana, cualquier posición abierta se liquida inmediatamente y no se colocan nuevas operaciones.
5. **Gestión de posiciones**
   - La estrategia no tiene niveles explícitos de stop-loss o take-profit. La gestión de riesgos debe manejarse externamente (p. ej., mediante estrategias protectoras o supervisión manual).

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
|--------|------|----------------|-------------|
| `Volume` | decimal | `1` | Volumen de orden fijo (lotes o contratos). La estrategia redondea automáticamente el valor al paso de volumen válido más cercano y aplica los límites mínimo/máximo del instrumento. |
| `UseTimeControl` | bool | `true` | Habilita el filtro de ventana de trading intradía. Cuando está deshabilitado, las señales se evalúan las 24 horas. |
| `StartHour` | int | `11` | Hora de inicio inclusiva (0-23) de la ventana de trading cuando `UseTimeControl` es verdadero. |
| `EndHour` | int | `17` | Hora de fin exclusiva (0-23) de la ventana de trading cuando `UseTimeControl` es verdadero. Si la hora de fin es menor que la de inicio, la ventana cruza medianoche. |
| `CandleType` | `DataType` | `TimeFrame(1h)` | Tipo de vela usado para el análisis. Seleccione un marco temporal compatible con su fuente de datos. |

## Notas de implementación
- La clase base `Strategy` de StockSharp maneja las suscripciones y el enrutamiento de órdenes. Las señales se evalúan en `ProcessCandle`, que recibe velas completadas a través de la API de enlace de alto nivel.
- Las órdenes de mercado se emiten a través de `BuyMarket` y `SellMarket`. Cuando ocurre una reversión, la estrategia primero cierra la exposición existente usando una orden de mercado opuesta antes de enviar la nueva entrada.
- `StartProtection()` se llama durante la inicialización para permitir adjuntar mecanismos de protección opcionales si se desea.
- El helper `GetTradeVolume` replica la normalización de lotes de MetaTrader ajustando el volumen configurado a las restricciones de la bolsa (paso de volumen, mínimo y máximo).
- La estrategia almacena solo dos precios típicos históricos, suficientes para evaluar el patrón de tres velas sin mantener grandes colecciones.

## Consejos de uso
- Adjunte la estrategia a un instrumento con liquidez suficiente. El EA original usaba datos Forex intradía, pero cualquier mercado que proporcione velas OHLC puede usarse.
- Elija un marco temporal de velas que se adapte a su horizonte de trading. Las velas de una hora predeterminadas replican el comportamiento del EA fuente, aunque intervalos más cortos o más largos pueden explorarse mediante optimización de parámetros.
- Considere combinar la estrategia con controles de riesgo como límites de drawdown máximo o stop loss a nivel de cartera a través del framework de estrategias protectoras de StockSharp.
- Realice backtests en múltiples instrumentos y sesiones de trading para confirmar que el patrón estrictamente monótono produce señales accionables bajo sus condiciones de mercado.
