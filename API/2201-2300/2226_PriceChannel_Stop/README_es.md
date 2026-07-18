# PriceChannel Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Price Channel Stop.

El indicador calcula el máximo más alto y el mínimo más bajo en el período dado para formar un canal de Donchian. Los niveles de stop se construyen dentro del canal usando el factor `Risk`. Cuando el precio cierra por encima del stop superior, la tendencia cambia a alcista; al cerrar por debajo del stop inferior, la tendencia cambia a bajista. La estrategia abre posiciones en estas reversiones y opcionalmente cierra posiciones opuestas.

## Detalles

- **Criterios de entrada**: El precio cruza los niveles de stop.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `ChannelPeriod` = 5
  - `Risk` = 0.10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Canal de Donchian
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
