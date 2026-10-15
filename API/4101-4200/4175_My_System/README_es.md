# Mi estrategia del sistema
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**Mi estrategia del sistema** es un puerto StockSharp del asesor experto MetaTrader 4 `MySystem.mq4` (directorio `MQL/9601`). El guión original evalúa los indicadores Bulls Power y Bears Power, combina sus valores en una señal de impulso compuesta y abre posiciones de estilo de reversión cuando el impulso cambia de signo. Esta versión de C# reproduce el proceso de decisión central, agrega un estado explícito de gestión de riesgos y expone cada constante ajustable a través de parámetros de estrategia para su optimización.

A diferencia de la implementación MQL, que consultaba directamente `iBullsPower`/`iBearsPower` con diferentes precios aplicados en cada barra, la edición StockSharp alimenta ambos indicadores de la serie de velas configuradas y rastrea internamente el valor compuesto anterior. La traducción mantiene el período de tiempo predeterminado de 15 minutos, las mismas distancias de obtención de beneficios/límite de pérdidas y las condiciones de salida final especificadas en el código fuente.

## Lógica de trading
1. Suscríbase al flujo de velas configurado (velas de 15 minutos de forma predeterminada) y espere a que las velas estén completamente terminadas.
2. Para cada vela completa, recupere los últimos valores de Bulls Power y Bears Power y calcule su promedio `((bulls + bears) / 2)`.
3. Mantenga el promedio anterior en `_previousAveragePower` para reflejar las llamadas basadas en turnos en MQL.
4. Reglas de entrada (solo cuando no hay ninguna posición abierta):
   - **Entrada corta** – si el promedio anterior es mayor que el promedio actual y el promedio actual sigue siendo positivo. Esto coincide con la condición MQL `pos1pre > pos2cur && pos2cur > 0`.
   - **Entrada larga**: si el promedio actual se vuelve negativo (`pos2cur < 0`), significa que Bears Power domina.
5. La gestión de salida se ejecuta en cada vela incluso antes de nuevas señales:
   - Evalúe los niveles estrictos de toma de ganancias y límite de pérdidas que se registraron cuando se abrió la posición.
   - Aplique la lógica del trailing-stop desde la fuente EA: para posiciones largas, salga cuando el impulso se debilite (`pos1pre > pos2cur`) y el precio haya avanzado la distancia de seguimiento; para los cortos, salga cuando el impulso compuesto se vuelva negativo y el precio se haya movido la distancia solicitada a favor.
6. Si se activa una señal de salida, llame a `ClosePosition()` para aplanar; Luego, la estrategia espera la siguiente vela para evaluar nuevas entradas.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `TakeProfitPoints` | Distancia al nivel de toma de ganancias expresada en pasos de precio. | `86` | Refleja la entrada `TakeProfit`. Establezca en `0` para deshabilitar el objetivo de ganancias. |
| `StopLossPoints` | Distancia al nivel de stop-loss expresada en pasos de precio. | `60` | Refleja la entrada `StopLoss`. Establezca en `0` para desactivar la parada de protección. |
| `TrailingStopPoints` | Distancia utilizada por la condición de salida final (pasos de precio). | `10` | Cuando es cero, se omite la lógica de seguimiento. |
| `OrderVolume` | Volumen presentado en cada nueva entrada. | `8.3` | Coincide con el parámetro `Lots` en EA. |
| `PowerPeriod` | Período aplicado a los indicadores Bulls Power y Bears Power. | `13` | Replica el período original. |
| `CandleType` | Serie de velas que impulsa los cálculos del indicador. | `15m` | Cambie para trasladar la estrategia a otro período de tiempo. |

Todos los parámetros se declaran a través de `Param()` para admitir el enlace de la interfaz de usuario y los barridos de optimización.

## Gestión del riesgo
- Los niveles de protección se almacenan cuando `OnPositionChanged` detecta una nueva exposición larga o corta. Las distancias se convierten a precios absolutos utilizando un asistente de tamaño de pip que se aproxima a la lógica `Point` de MetaTrader (`PriceStep`, ajustada para símbolos FX de 3/5 decimales).
- `ClosePosition()` se invoca una vez que se cumple una condición de obtención de ganancias, límite de pérdidas o seguimiento, lo que garantiza que la estrategia salga con una única orden de mercado y evita solicitudes de cierre duplicadas.
- No se realizan coberturas ni cierres parciales; la estrategia impone una sola posición a la vez, exactamente como la guardia `OrdersTotal() < 1` en el script MQL.

## Notas de conversión
- Los argumentos `PRICE_WEIGHTED` frente a `PRICE_CLOSE` de MetaTrader se aproximaron almacenando el valor compuesto anterior (`pos1pre`) en lugar de mantener dos instancias de indicador con diferentes precios. Esto mantiene la intención del comportamiento sin duplicar las transformaciones de las velas.
- El EA original contenía varias llamadas `OrderSelect` con formato incorrecto dentro de la lógica final. El puerto implementa el efecto deseado (cerrar operaciones una vez que el precio recorre la distancia final mientras se satisface la condición de impulso) de una manera determinista.
- Las salidas finales se evalúan contra los máximos y mínimos de las velas para emular los toques intrabar porque StockSharp procesa las velas completadas de forma predeterminada.
- El tamaño de las órdenes, las distancias de parada y los períodos de los indicadores conservan los valores predeterminados originales para que las optimizaciones existentes se puedan reproducir sin ajustes.

## Consejos de uso
1. Adjunte la estrategia a un valor que exponga `PriceStep` y `Decimals`. Si faltan, el asistente vuelve a tener un tamaño de pip de `1`.
2. Ajuste `OrderVolume`, `TakeProfitPoints` y `StopLossPoints` para alinearlos con el tamaño del contrato y el valor del tick del instrumento.
3. Cuando realice pruebas en diferentes períodos de tiempo, recuerde actualizar `CandleType` y considere volver a optimizar la distancia de seguimiento, ya que las barras más cortas alcanzarán el umbral con más frecuencia.
4. Utilice los gráficos StockSharp (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) para validar que las entradas se produzcan cuando el poder de los alcistas y bajistas cruza los umbrales especificados.

## Archivos
- `CS/MySystemStrategy.cs`: implementación de estrategia utilizando el nivel alto de StockSharp API.
- `README.md`, `README_zh.md`, `README_ru.md`: documentación multilingüe para el asesor experto convertido.
