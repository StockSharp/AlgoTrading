# Estrategia de Semáforo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un enfoque de seguimiento de tendencia que utiliza un conjunto de medias móviles coloreadas como un semáforo para determinar la dirección de trading.
La estrategia espera a que el precio esté dentro de una zona predefinida y luego verifica el orden de las medias antes de entrar al mercado.

## Detalles

- **Zona de entrada**:
  - Por defecto: el precio debe estar entre las SMA roja (lenta) y amarilla (media).
  - Si `UseBlueRange` está activado: el precio debe estar entre las líneas alta y baja del canal EMA azul.
- **Criterios de entrada**:
  - Largo: `green EMA > blueHigh EMA > yellow SMA > red SMA` y `price > green EMA`.
  - Corto: `green EMA < blueLow EMA < yellow SMA < red SMA` y `price < green EMA`.
- **Criterios de salida**:
  - Opcional: si `CloseOnCross` está activado la posición se cierra cuando la EMA verde cruza la SMA amarilla en dirección opuesta.
- **Stops**: Take profit y stop loss opcionales medidos en pasos de precio.
- **Largo/Corto**: Ambos.
- **Valores predeterminados**:
  - `RedMaPeriod` = 120
  - `YellowMaPeriod` = 55
  - `GreenMaPeriod` = 5
  - `BlueMaPeriod` = 24
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `TakeProfitTicks` = 120
  - `StopLossTicks` = 60
  - `UseBlueRange` = false
  - `CloseOnCross` = true
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Medias móviles
  - Stops: Opcional
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
