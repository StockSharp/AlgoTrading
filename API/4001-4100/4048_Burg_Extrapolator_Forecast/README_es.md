# Estrategia de pronóstico del extrapolador de Burg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Burg Extrapolator es una versión StockSharp del MetaTrader 4 asesor experto "Burg Extrapolator". El sistema original ajusta un modelo autorregresivo (AR) de Burg a una ventana deslizante de precios de apertura (o su impulso/transformaciones ROC) y proyecta una trayectoria de precios futuros. Las decisiones comerciales se derivan de los valores de pronóstico más extremos: si la excursión prevista en una dirección es lo suficientemente grande, la estrategia acumula nuevas posiciones o liquida la exposición en la dirección opuesta. La conversión mantiene los mismos bloques de modelado mientras asigna la gestión de posiciones y la gestión del dinero a StockSharp primitivas.

## Lógica de trading
1. **Preparación de datos**
   - Cree un historial continuo de precios de apertura de `PastBars + 1` para el `CandleType` seleccionado.
   - Opcionalmente, transforme los datos en impulso logarítmico (predeterminado) o tasa de cambio porcentual antes de enviarlos al modelo AR. Los precios brutos están centrados por su promedio móvil para reflejar el código MT4.
2. **Predicción lineal de robo**
   - Calcule los coeficientes de reflexión hasta el orden `PastBars * ModelOrder` utilizando el algoritmo de Burg.
   - Genere una secuencia de valores futuros (`PastBars` pasos adelante en la práctica) expandiendo recursivamente el modelo AR. Las transformadas se invierten nuevamente al espacio de precios para que todos los pronósticos operen en unidades de precios absolutos.
3. **Detección de señal**
   - Recorra la ruta del pronóstico y registre el precio más alto y más bajo pronosticado antes de que aparezca otro extremo. La distancia entre el primer extremo y el otro lado del rango de pronóstico se compara con los umbrales `MaxLoss` y `MinProfit` (convertidos a precio absoluto multiplicando con el instrumento `PriceStep`).
   - Un repunte suficientemente grande desencadena `OpenSignal = 1` mientras que un repunte grande genera `OpenSignal = -1`. Si el extremo opuesto aparece primero, la lógica establece que `CloseSignal` salga de la exposición actual incluso si no se planea una nueva entrada.
4. **Gestión de pedidos**
   - Las salidas de protección (stop-loss, take-profit y trailing-stop opcional) se ejecutan antes de que se ejecute cualquier señal nueva. El trailing-stop reutiliza el mejor precio desde la última entrada y cierra la posición cuando el precio retrocede `TrailingStop` puntos, coincidiendo con el comportamiento MT4 de mover la orden de protección.
   - Si una señal solicita cerrar la exposición en la dirección opuesta, la estrategia envía una orden de mercado del tamaño de aplanar la posición neta actual.
   - Las señales de entrada acumulan órdenes de mercado adicionales en la dirección indicada hasta alcanzar `MaxTrades`. El volumen de pedidos aumenta linealmente con el número de operaciones activas utilizando el factor `1 + existingTrades * MaxRisk`, un reemplazo fácil de StockSharp para la rutina de dimensionamiento original basada en márgenes.

## Indicadores y datos
- Suscripción de vela definida por `CandleType` (período de tiempo predeterminado de 30 minutos).
- Modelo autorregresivo de Burg interno (implementado sin indicadores externos).
- Transformaciones opcionales de impulso logarítmico y tasa de cambio porcentual.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | velas de 30 minutos | Plazo primario procesado por la estrategia. |
| `MaxRisk` | 0,5 | Multiplicador de riesgo utilizado al acumular múltiples operaciones. |
| `MaxTrades` | 5 | Número máximo de operaciones simultáneas por dirección. |
| `MinProfit` | 160 | Beneficio mínimo previsto (en puntos) requerido para abrir nuevas operaciones. |
| `MaxLoss` | 130 | Pérdida prevista máxima tolerada (en puntos) antes de cerrar las operaciones. |
| `TakeProfit` | 0 | Distancia de toma de ganancias fija opcional en puntos (0 la deshabilita). |
| `StopLoss` | 180 | Distancia de stop-loss fija opcional en puntos (0 lo desactiva). |
| `TrailingStop` | 10 | Distancia del trailing stop en puntos, activo solo cuando `StopLoss > 0`. |
| `PastBars` | 200 | Número de velas históricas utilizadas por el modelo Burg. |
| `ModelOrder` | 0,37 | Fracción de `PastBars` convertida al orden Burg. |
| `UseMomentum` | cierto | Aplique la transformación de momento logarítmico a los datos de entrada. |
| `UseRateOfChange` | falso | Aplicar tasa porcentual de cambio (ignorada cuando el impulso está habilitado). |

Todos los parámetros son instancias de `StrategyParam<T>` y se pueden optimizar o ajustar en el StockSharp Diseñador.

## Notas de implementación
- El algoritmo Burg se implementa directamente en C# y mantiene la misma recursividad que la versión MT4. Todos los cálculos se ejecutan con doble precisión mientras los pronósticos finales se convierten nuevamente a `decimal` antes de las verificaciones de señales.
- El EA original podía basarse en la información de la cuenta MetaTrader para dimensionar las posiciones. En StockSharp el bloque de administración de dinero se reemplaza con una regla de escala determinista basada en `Volume` y `MaxRisk`. Establezca `Volume` en el lote base deseado y la estrategia escalará las entradas posteriores proporcionalmente.
- La lógica protectora cierra posiciones con órdenes de mercado explícitas en lugar de modificar las paradas del corredor; esto coincide con el diseño de alto nivel API de StockSharp y evita el estado obsoleto cuando se ejecuta en simulación.
- Las matrices de pronóstico se vuelven a crear cada vez que `PastBars` o `ModelOrder` cambian, de modo que las ediciones de parámetros sobre la marcha afectan inmediatamente al modelo AR sin reiniciar la estrategia.
- Para visualizar el comportamiento, puede adjuntar un gráfico en Designer: la estrategia ya dibuja velas y ejecuta operaciones en el área predeterminada. Ampliar la muestra con series personalizadas (por ejemplo, ruta de pronóstico) es sencillo si se desea.
