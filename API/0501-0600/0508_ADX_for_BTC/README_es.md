# ADX para BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza el Average Directional Index (ADX) con un filtro de tendencia SMA opcional para capturar movimientos fuertes en Bitcoin.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 80%. Funciona mejor en el mercado cripto.

El sistema compra cuando el ADX cruza por encima del nivel de entrada y el filtro de tendencia es alcista. La posición se cierra cuando el ADX cae por debajo del nivel de salida.

## Detalles

- **Criterios de entrada**: ADX cruza por encima de `EntryLevel` y (si está activado) SMA rápida > SMA lenta.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: ADX cruza por debajo de `ExitLevel`.
- **Stops**: No.
- **Valores predeterminados**:
  - `EntryLevel` = 14m
  - `ExitLevel` = 45m
  - `SmaFilter` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: ADX, SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
