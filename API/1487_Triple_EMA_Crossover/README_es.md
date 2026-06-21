# Estrategia de Cruce Triple EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en tres medias móviles simples.
Se abre una operación larga cuando la SMA corta cruza por encima de la SMA media mientras las tres están alineadas al alza.
Se abre una operación corta en el cruce opuesto y alineación.
El precio al cruzar de nuevo la SMA corta cierra la posición.

## Detalles

- **Criterios de entrada**: Cruces de SMA1 y SMA2 con filtro de tendencia.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Precio cruzando SMA1 o stops protectores.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Sma1Period` = 9
  - `Sma2Period` = 21
  - `Sma3Period` = 55
  - `StopLossTicks` = 200
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Fijo
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
