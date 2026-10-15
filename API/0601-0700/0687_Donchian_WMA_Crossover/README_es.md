# Estrategia de Cruce Donchian WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El cruce del mínimo del canal Donchian por encima de una media móvil ponderada activa entradas largas solo durante el año calendario 2025. Las posiciones se cierran cuando se alcanza un nivel de take-profit, el cruce se invierte con una WMA descendente, o la fecha sale de 2025.

## Detalles

- **Criterios de entrada**:
  - Largo: `DonchianLow` cruza por encima de `WMA` y la fecha está dentro de 2025
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - Take profit mediante `TakeProfitPercent`
  - Cruce descendente de `DonchianLow` por debajo de `WMA` mientras `WMA` cae
  - Fecha fuera de 2025
- **Stops**: Solo take profit
- **Valores predeterminados**:
  - `DonchianLength` = 7
  - `WmaLength` = 62
  - `TakeProfitPercent` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo
  - Indicadores: Canal Donchian, Media Móvil Ponderada
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Medio plazo
  - Estacionalidad: Solo el año 2025
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
