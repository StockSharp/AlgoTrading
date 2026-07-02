# FT Bill Williams Estrategia AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia FT Bill Williams AO** es una versión StockSharp de alto nivel del MetaTrader 4 experto `FT_BillWillams_AO`. el original
El robot fue publicado en FORTRADER.RU y combina los fractales Bill Williams, el indicador Alligator y el Awesome Oscillator para
identificar oportunidades de ruptura tempranas. La versión StockSharp mantiene la lógica original pero funciona con una única posición neta en lugar de
múltiples pedidos simultáneos.

El algoritmo opera con velas completadas desde un período de tiempo configurable. Cada barra:

1. Detecta fractales alcistas y bajistas formados a partir de un número impar de velas.
2. Filtra fractales comprobando si el precio del fractal está fuera de la línea de dientes Alligator.
3. Espera a que Awesome Oscillator (AO) forme el clásico patrón de aceleración de tres barras.
4. Coloca un disparador de ruptura por encima o por debajo del máximo o mínimo reciente desplazado por un número definido por el usuario de MetaTrader puntos.
5. Aplica la rutina de seguimiento de Bill Williams' Gragus y reglas de salida opcionales basadas en la mandíbula.

## Lógica de entrada
### Entradas largas
- Aparece un fractal alcista y su alto precio se sitúa por encima de los dientes Alligator.
- Los valores de AO tomados hace `SignalShift + 2`, `SignalShift + 1` y `SignalShift` velas satisfacen `A > B`, `B < C`, y los tres son
positivo.
- Un nivel de ruptura pendiente se calcula como `High[SignalShift] + IndentPoints * price step`.
- Cuando una vela completa cruza ese nivel y AO aún aumenta (`C > B`), la estrategia abre o revierte a una posición larga.

### Entradas cortas
- Aparece un fractal bajista y su mínimo está debajo de los dientes Alligator.
- Los valores de AO satisfacen `A < B`, `B > C` y los tres son negativos.
- Se coloca un activador de ruptura en `Low[SignalShift] - IndentPoints * price step`.
- Se abre una posición corta (o reversión de una posición larga) cuando la vela cae por debajo de ese disparador mientras AO sigue cayendo (`C < B`).

## Salida y gestión de riesgos.
- El stop-loss y la toma de ganancias iniciales se expresan en MetaTrader puntos y se traducen en distancia de precio real a través del instrumento.
paso de precio.
- El modo **CloseDropTeeth** puede cerrar posiciones cuando el cierre actual o el cierre anterior cruza la mandíbula Alligator.
- **CloseReverseSignal** determina si un fractal opuesto o la activación de la señal de ruptura opuesta debería forzar una
salir.
- El interruptor **UseTrailing** habilita la rutina original de parada dinámica de Gragus: cuando los labios Alligator avanzan más rápido que un corto
SMA, el stop se traslada a los labios; de lo contrario, arrastra los dientes. Ambos movimientos requieren que el precio se mantenga al menos a 12 puntos de distancia.
desde la línea objetivo.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `TradeVolume` | Tamaño del pedido en lotes. También está escrito en `Strategy.Volume`. |
| `CandleType` | Tipo de datos y período de tiempo de las velas de entrada. |
| `FractalPeriod` | Número impar de velas utilizadas para confirmar fractales (por defecto 5). |
| `IndentPoints` | MetaTrader puntos agregados por encima/debajo del máximo/mínimo de la vela de ruptura. |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Longitud de las medias móviles suavizadas utilizadas por las líneas Alligator. |
| `JawShift`, `TeethShift`, `LipsShift` | Desplazamiento hacia adelante (en velas) aplicado a las líneas Alligator. |
| `CloseDropTeeth` | Comportamiento de la regla de cierre basada en mandíbula: deshabilitada, cruce de cierre actual o cruce de cierre anterior. |
| `CloseReverseSignal` | Condición de salida en señales opuestas: desactivada, en un nuevo fractal o una vez que la fuga opuesta esté armada. |
| `UseTrailing` | Habilita o deshabilita la rutina de parada dinámica de Gragus. |
| `TrendSmaPeriod` | Período del auxiliar SMA utilizado por la comparación final. |
| `StopLossPoints` | Distancia inicial de stop-loss en MetaTrader puntos. Establezca en cero para desactivar. |
| `TakeProfitPoints` | Distancia inicial de obtención de beneficios en MetaTrader puntos. Establezca en cero para desactivar. |
| `SignalShift` | Número de velas completamente cerradas omitidas al leer los valores de AO y los máximos/mínimos recientes. |

## Notas
- La estrategia supone que la seguridad expone un `PriceStep` válido (recurre a `MinPriceStep`); si faltan ambos, un valor predeterminado de
Se utiliza `0.0001`.
- Sólo se gestiona una posición neta. Las señales de inversión cierran automáticamente la posición opuesta antes de abrir una nueva.
- Para obtener mejores resultados, mantenga `FractalPeriod` impar; El experto original usó 5 velas.
- `IndentPoints`, `StopLossPoints` y `TakeProfitPoints` imitan MetaTrader puntos. Ajústalos según el precio del instrumento.
escala.
