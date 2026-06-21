# Estrategia Revelations
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia de ruptura de volatilidad que entra en fuertes picos de ATR confirmados por extremos locales y un índice de régimen. El tamaño de posición se adapta a la intensidad del pico.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Pico de ATR hacia arriba en un mínimo local con confirmación de régimen.
  - **Corto**: Pico de ATR hacia abajo en un máximo local con confirmación de régimen.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Alcance de objetivo de ganancia o stop loss.
- **Stops**: Stops de porcentaje fijo.
- **Valores predeterminados**:
  - `ATR Fast` = 14
  - `ATR Slow` = 21
  - `ATR StdDev` = 12
  - `Spike Threshold` = 0.5
  - `Super Spike Mult` = 1.5
  - `Regime Window` = 8
  - `Regime Events` = 3
  - `Local Window` = 3
  - `Max Quantity` = 2
  - `Min Quantity` = 1
  - `Stop %` = 0.9
  - `Take Profit %` = 1.8
- **Filtros**:
  - Categoría: Ruptura de volatilidad
  - Dirección: Largo/Corto
  - Indicadores: ATR, SMA, Highest/Lowest
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
