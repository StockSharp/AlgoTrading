# Estrategia MARE5.1 con Media Móvil Desplazada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia MARE5.1 con Media Móvil Desplazada** es un port directo del asesor experto original de MetaTrader 5 "MARE5.1" a la API de alto nivel de StockSharp. El sistema monitorea velas de un minuto (configurables) y compara dos medias móviles simples (SMA) que comparten un desplazamiento hacia adelante configurable. La lógica busca patrones de cruce confirmados por relaciones históricas de SMA y la dirección de la última vela completada.

## Lógica de Trading

- La estrategia usa dos SMAs: una SMA rápida y una SMA lenta. Ambas están desplazadas hacia adelante el mismo número de barras, replicando el comportamiento del asesor experto original.
- Se abre una **posición corta** cuando todo lo siguiente es verdadero:
  1. La SMA lenta está al menos un paso de precio por encima de la SMA rápida en la vela actual.
  2. Dos velas atrás, la SMA rápida estaba al menos un paso de precio por encima de la SMA lenta.
  3. Cinco velas atrás, la SMA rápida estaba al menos un paso de precio por encima de la SMA lenta.
  4. La vela completada más reciente (barra anterior) es bajista.
- Se abre una **posición larga** cuando ocurre el patrón opuesto:
  1. La SMA rápida está al menos un paso de precio por encima de la SMA lenta en la vela actual.
  2. Dos velas atrás, la SMA lenta estaba al menos un paso de precio por encima de la SMA rápida.
  3. Cinco velas atrás, la SMA lenta estaba al menos un paso de precio por encima de la SMA rápida.
  4. La vela completada más reciente (barra anterior) es alcista.
- Solo puede estar abierta una posición a la vez. El tamaño predeterminado de la orden proviene del parámetro `TradeVolume`.
- El trading solo está permitido entre las horas de sesión configuradas (inclusive). Esta ventana replica el filtro basado en horas del asesor experto original.

## Gestión de Riesgos

La estrategia replica las distancias fijas de toma de ganancias y stop-loss originales. Se definen en "pips" (puntos ajustados para instrumentos de tres y cinco dígitos) y se convierten en unidades de precio absolutas cuando la estrategia comienza. Las órdenes de protección se gestionan a través de `StartProtection` con salidas de órdenes de mercado.

## Indicadores y Datos

- **SMA rápida** – longitud definida por `FastPeriod`.
- **SMA lenta** – longitud definida por `SlowPeriod`.
- **Fuente de datos** – por defecto velas de un minuto, pero cualquier tipo de vela compatible con StockSharp puede seleccionarse a través del parámetro `CandleType`.

## Parámetros

| Nombre | Valor predeterminado | Descripción |
|--------|----------------------|-------------|
| `TradeVolume` | 0.01 | Volumen de orden utilizado para entradas. |
| `TakeProfitPips` | 35 | Distancia de toma de ganancias en pips ajustados. Establecer en cero para deshabilitar. |
| `StopLossPips` | 55 | Distancia de stop-loss en pips ajustados. Establecer en cero para deshabilitar. |
| `FastPeriod` | 14 | Período de la SMA rápida. |
| `SlowPeriod` | 79 | Período de la SMA lenta. |
| `MovingAverageShift` | 4 | Desplazamiento hacia adelante (en barras) aplicado a ambas SMAs. |
| `SessionOpenHour` | 2 | Inicio de la ventana de trading permitida (0–23, inclusive). |
| `SessionCloseHour` | 3 | Fin de la ventana de trading permitida (0–23, inclusive). Debe ser mayor que `SessionOpenHour`. |
| `CandleType` | Velas de 1 minuto | Tipo de datos de velas utilizado por la estrategia. |

## Notas

- Las señales se evalúan en velas completadas. Los valores históricos de SMA se almacenan internamente para replicar las comparaciones basadas en índice del código MQL original.
- El valor del paso de precio del instrumento activo se usa al comparar diferencias de SMA para asegurar que la distancia requerida sea al menos un tick.
- Los niveles de stop-loss y toma de ganancias dependen del paso de precio del instrumento. Para instrumentos de tres y cinco decimales, el tamaño de pip se expande automáticamente diez veces, coincidiendo con el comportamiento de MetaTrader.
- No se implementa ningún escalado automático de posiciones. La estrategia espera a que todas las posiciones abiertas se cierren antes de buscar la siguiente señal de entrada.
- Este repositorio contiene solo la implementación en C#; no hay port en Python para esta estrategia.
