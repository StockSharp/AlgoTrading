# Estrategia G-Channel con EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina la lógica del canal G-Channel con un filtro de tendencia EMA.

Compra cuando el último cruce es descendente y el precio está por debajo de la EMA. Vende cuando el último cruce es ascendente y el precio está por encima de la EMA.

## Detalles

- **Criterios de entrada**: Estado del G-Channel con filtro EMA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `ChannelLength` = 100
  - `EmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: G-Channel, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
