# Estrategia de Desaceleración RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Desaceleración RSI reacciona a lecturas extremas del Índice de Fuerza Relativa que muestran signos de debilitamiento del momentum. Cuando el RSI se aproxima a zonas de sobrecompra o sobreventa y su cambio entre barras cae por debajo de un punto, la estrategia asume que el mercado está listo para una reversión.

Se abre una posición larga cuando el RSI alcanza o supera el nivel superior y el crecimiento del indicador se ralentiza. Se abre una posición corta cuando el RSI cae al nivel inferior con una desaceleración similar. Cualquier posición opuesta existente se cierra antes de entrar en una nueva operación.

La configuración predeterminada utiliza velas de 6 horas y un RSI de 2 períodos con umbrales de 90 y 10. Estos valores imitan la implementación original de MetaTrader.

## Detalles
- **Criterios de entrada**:
  - **Largo**: RSI >= `LevelMax` y `|RSI - prev RSI| < 1` (cuando la desaceleración está habilitada)
  - **Corto**: RSI <= `LevelMin` y `|RSI - prev RSI| < 1` (cuando la desaceleración está habilitada)
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - **Largo**: Señal opuesta o entrada corta.
  - **Corto**: Señal opuesta o entrada larga.
- **Stops**: Sin stops automáticos.
- **Valores predeterminados**:
  - `RsiPeriod` = 2
  - `LevelMax` = 90
  - `LevelMin` = 10
  - `SeekSlowdown` = true
  - `CandleType` = `TimeSpan.FromHours(6)`
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía a swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí (desaceleración)
  - Nivel de riesgo: Medio
