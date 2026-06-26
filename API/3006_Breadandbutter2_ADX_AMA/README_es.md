# Bread and Butter 2 (ADX + AMA)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port del asesor experto de MetaTrader 5 *Breadandbutter2* creado por Ron Thompson. La lógica original espera una barra nueva, compara el último valor del Average Directional Index (ADX) con el anterior, y comprueba si la Kaufman Adaptive Moving Average (KAMA, también conocida como AMA) está subiendo o bajando. Se abre una posición larga cuando la fuerza de la tendencia se debilita mientras el momentum del precio mejora, mientras que se abre una posición corta cuando la fuerza de la tendencia aumenta mientras el momentum se deteriora. La versión StockSharp mantiene el comportamiento de cerrar cualquier exposición contraria antes de abrir una nueva orden, y aplica las mismas distancias fijas de stop-loss y take-profit que se especificaron en pips en el script original.

## Indicadores
- **Average Directional Index (ADX)** – mide la fuerza de la tendencia actual. La estrategia mira la línea ADX principal y compara los últimos dos valores para determinar si la fuerza de la tendencia está aumentando o disminuyendo.
- **Kaufman Adaptive Moving Average (KAMA/AMA)** – se adapta a la volatilidad del mercado usando constantes de suavizado rápido y lento separadas. La estrategia compara los últimos dos valores para evaluar la dirección del momentum.

## Lógica de la estrategia
1. Trabajar con el tipo de vela configurado (predeterminado: barras de 1 hora) y esperar hasta que una vela esté completamente cerrada antes de procesar.
2. Calcular KAMA con la longitud seleccionada, período rápido y período lento.
3. Calcular ADX con el período de promediado configurado y extraer el valor de la línea principal.
4. Comparar las lecturas actuales y anteriores del indicador:
   - **Configuración larga** – el valor ADX disminuye (la fuerza de la tendencia se debilita) mientras KAMA sube (el momentum del precio mejora).
   - **Configuración corta** – el valor ADX aumenta mientras KAMA cae.
5. Cuando aparece una señal, cerrar cualquier exposición del lado contrario y abrir una nueva orden de mercado para que la posición final coincida con el volumen base de la estrategia.
6. Monitorear continuamente la posición activa. Si el precio toca los niveles de stop-loss o take-profit configurados (expresados en pips y convertidos a unidades de precio según el tamaño del tick del instrumento), salir de la operación inmediatamente.

## Gestión de operaciones
- **Stop-loss** – expresado en pips; convertido a unidades de precio usando el `PriceStep` del instrumento. Para símbolos cotizados con 3 o 5 decimales, el tamaño del pip es 10 veces el paso de precio, coincidiendo con la implementación de MetaTrader.
- **Take-profit** – también expresado en pips y manejado de la misma manera que la distancia del stop-loss.
- La estrategia usa órdenes de mercado para entradas y salidas y voltea la posición cuando ocurre una señal contraria.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Tipo de vela usado para todos los cálculos. |
| `AdxPeriod` | `14` | Longitud de promediado de la línea principal del ADX. |
| `AmaPeriod` | `9` | Período base de la Kaufman Adaptive Moving Average. |
| `AmaFastPeriod` | `2` | Período EMA rápido usado dentro del AMA. |
| `AmaSlowPeriod` | `30` | Período EMA lento usado dentro del AMA. |
| `StopLossPips` | `50` | Distancia al stop-loss de protección en pips. Establecer en `0` para deshabilitar. |
| `TakeProfitPips` | `50` | Distancia al objetivo de beneficio en pips. Establecer en `0` para deshabilitar. |

## Notas de uso
- Asegurarse de que la estrategia esté adjunta a un instrumento que exponga un `PriceStep` válido. Para símbolos forex con pips fraccionarios, el tamaño del pip se calcula automáticamente.
- `Volume` controla el tamaño base de la orden. Cuando aparece una señal de reversión, el algoritmo agrega suficiente volumen para cerrar cualquier exposición contraria y establecer una posición igual a `Volume` en la nueva dirección.
- Porque las salidas de stop-loss y take-profit se evalúan en los máximos y mínimos de las velas, el comportamiento aproxima la ejecución de órdenes pendientes de MetaTrader.

## Referencias
- Estrategia original de MetaTrader 5: `MQL/22003/Breadandbutter2.mq5`
- Indicadores StockSharp: `KaufmanAdaptiveMovingAverage`, `AverageDirectionalIndex`
