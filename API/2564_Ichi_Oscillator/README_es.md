# Estrategia Ichi Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del experto MetaTrader 5 **Exp_ICHI_OSC** a la API de alto nivel de StockSharp.
- Opera en una serie de velas configurable y deriva señales de un oscilador construido sobre líneas de Ichimoku.
- El valor bruto del oscilador es `((Close - SenkouA) - (Tenkan - Kijun)) / Step`, suavizado por una media móvil seleccionable.
- Las órdenes se ejecutan con el volumen de la estrategia; los bloques complejos de gestión de dinero del código original fueron reemplazados por el manejo de posiciones de StockSharp.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal de velas utilizado para todos los cálculos de indicadores. |
| `IchimokuBase` | Período base que define las longitudes de Tenkan (`base * 0.5`), Kijun (`base * 1.5`) y Senkou B (`base * 3`). |
| `Smoothing Method` | Media móvil usada para suavizar el oscilador. Opciones: `Simple`, `Exponential`, `Smoothed`, `Weighted`, `Jurik`, `Kaufman`. |
| `Smoothing Length` | Período del método de suavizado seleccionado. |
| `Smoothing Phase` | Parámetro de compatibilidad reservado (mantenido de la versión MQL, actualmente no usado por las implementaciones de suavizado integradas). |
| `Signal Bar` | Número de barras hacia atrás desde el último vela terminado usado para leer los colores del oscilador (predeterminado `1`). |
| `Enable Buy Entries / Enable Sell Entries` | Permitir abrir posiciones largas o cortas respectivamente. |
| `Enable Buy Exits / Enable Sell Exits` | Permitir cerrar posiciones largas o cortas existentes. |
| `Stop Loss (points)` | Distancia de stop protector expresada en pasos de precio. |
| `Take Profit (points)` | Distancia de take-profit expresada en pasos de precio. |
| `Order Volume` | Volumen base de orden utilizado por las órdenes de mercado. |

## Lógica de trading
1. Suscribirse a la serie de velas solicitada y calcular los valores de Tenkan, Kijun y Senkou A usando los períodos de Ichimoku derivados.
2. Construir el oscilador a partir de las diferencias entre el precio, Senkou A, Tenkan y Kijun y pasarlo por el suavizador seleccionado.
3. Asignar un código de color a cada valor suavizado:
   - `0` — oscilador por encima de cero y subiendo.
   - `1` — oscilador por encima de cero y bajando.
   - `2` — neutral (nivel cero o sin cambios).
   - `3` — oscilador por debajo de cero y decreciendo.
   - `4` — oscilador por debajo de cero y subiendo.
4. Leer dos colores: la barra en `SignalBar + 1` (color anterior) y la barra en `SignalBar` (color actual).
   - Si el color anterior es `0` o `3`, cerrar cortos cuando esté permitido y abrir un largo cuando el color actual es `2`, `1` o `4`.
   - Si el color anterior es `4` o `1`, cerrar largos cuando esté permitido y abrir un corto cuando el color actual es `0`, `1` o `3`.
5. Las órdenes se colocan con el volumen configurado. Los largos y cortos nunca se apilan: las señales de apertura se evalúan solo después de que la lógica de salida haya corrido en la misma barra.

## Gestión de riesgos
- Las órdenes protectoras se gestionan a través de `StartProtection`, usando las distancias de stop-loss y take-profit en pasos de precio.
- No hay trailing ni salidas parciales habilitadas por defecto.

## Notas
- El módulo de gestión de dinero original (cálculos de lotes, manejo de desviación, temporizadores de operaciones) es reemplazado por el control de posición y volumen de StockSharp.
- Los métodos de suavizado que no existen en StockSharp (p.ej., JurX, ParMA, VIDYA, T3) no están disponibles; elegir la alternativa más cercana de la lista proporcionada.
- Las marcas de tiempo de señal en los registros incluyen el tiempo de cierre de la vela más un período completo de vela, reflejando el uso de `TimeShiftSec` en MQL.
