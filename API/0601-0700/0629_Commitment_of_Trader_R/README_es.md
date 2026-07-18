# Estrategia Commitment of Trader R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador Williams %R para detectar condiciones de sobrecompra y sobreventa. Una media móvil simple actúa como filtro de tendencia opcional.

Se abre una operación larga cuando Williams %R sube por encima del umbral superior y el precio de cierre está por encima de la SMA. Se abre una operación corta cuando Williams %R cae por debajo del umbral inferior y el precio está por debajo de la SMA. Las posiciones se cierran cuando el oscilador abandona la zona de señal.

## Detalles
- **Criterios de entrada**:
  - **Largo**: %R > umbral superior y (precio > SMA si está habilitado)
  - **Corto**: %R < umbral inferior y (precio < SMA si está habilitado)
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - **Largo**: %R < umbral superior
  - **Corto**: %R > umbral inferior
- **Stops**: No
- **Valores predeterminados**:
  - `WilliamsPeriod` = 252
  - `UpperThreshold` = -10
  - `LowerThreshold` = -90
  - `SmaEnabled` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Williams %R, SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
