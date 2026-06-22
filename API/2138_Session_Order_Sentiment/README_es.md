# Estrategia de Sentimiento de Órdenes por Sesión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia opera basándose en el desequilibrio entre órdenes de compra y venta observadas en el libro de órdenes. Mide las proporciones de recuentos de órdenes y volúmenes totales para ambos lados del libro y abre una posición cuando el dominio de un lado supera los umbrales configurables. El trading solo está permitido durante una ventana de tiempo especificada.

Después de abrir una posición, los umbrales se reducen para monitorear el lado opuesto. Si el lado opuesto crece más allá de estos umbrales reducidos, la posición se cierra. También se aplican un stop loss y un take profit usando puntos de precio absolutos.

## Reglas de trading
- **Entrada larga**: Comprar cuando
  - `BUY volume / SELL volume >= DiffVolumesEx` y `BUY orders / SELL orders >= DiffTradersEx`
  - Cualquier lado cumple `MinTraders` y `MinVolume`
  - El tiempo actual pasa `CheckTradingTime`
- **Entrada corta**: Vender cuando la lógica anterior se aplica en espejo para el lado vendedor.
- **Salida**:
  - Cerrar el largo cuando `SELL volume / BUY volume > 1 / DiffVolumes` o `SELL orders / BUY orders > 1 / DiffTraders`
  - Cerrar el corto cuando `SELL volume / BUY volume < DiffVolumes` o `SELL orders / BUY orders < DiffTraders`
  - Cerrar todas las posiciones fuera del horario de trading
- **Stops**: Usa `Stop Loss` y `Take Profit` medidos en puntos de precio.

## Parámetros
- `MinVolume` – volumen total mínimo requerido en cualquier lado del libro (por defecto: 20000)
- `MinTraders` – número mínimo de órdenes en cualquier lado (por defecto: 1000)
- `DiffVolumesEx` – ratio de volumen requerido para entrada (por defecto: 2.0)
- `DiffTradersEx` – ratio de recuento de órdenes requerido para entrada (por defecto: 1.5)
- `MinDiffVolumesEx` – ratio de volumen usado después de abrir posición (por defecto: 1.5)
- `MinDiffTradersEx` – ratio de recuento de órdenes usado después de abrir posición (por defecto: 1.3)
- `SleepMinutes` – retraso entre verificaciones del libro de órdenes en minutos (por defecto: 5)
- `TpPips` – take profit en puntos de precio (por defecto: 500)
- `SlPips` – stop loss en puntos de precio (por defecto: 500)

## Notas
La estrategia no incluye una versión en Python.
