# Estrategia SMTP de Larry Conners
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia solo en largo que compra tras un mínimo de 10 barras cuando la barra actual tiene el mayor rango de las últimas 10 barras y cierra en el 25% superior de su rango. La entrada se coloca un tick por encima del máximo; el stop-loss sigue los mínimos sucesivos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el mínimo actual es igual al mínimo de las últimas 10 barras, el rango de hoy es el mayor de las últimas 10 y el cierre está en el 25% superior del rango; colocar una orden de compra stop en `High + TickSize`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Trailing stop en el mínimo más alto desde la entrada.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `TickSize` = 0.01
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Largo
  - Indicadores: Highest, Lowest
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
