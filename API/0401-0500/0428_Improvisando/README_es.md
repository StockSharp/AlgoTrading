# Estrategia Improvisando
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Improvisando combina un filtro de tendencia EMA básico con oscilaciones del RSI. El objetivo es seguir la dirección prevaleciente indicada por la EMA mientras se entra solo cuando el RSI cruza la línea neutral de 50. El diseño original también experimentó con momentum estilo MACD, pero esta versión simplificada se centra en la claridad y la facilidad de ajuste.

El usuario puede habilitar operaciones largas y/o cortas por separado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close > EMA` y `RSI > 50`
  - **Corto**: `Close < EMA` y `RSI < 50`
- **Largo/Corto**: Configurable
- **Criterios de salida**:
  - Señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `EmaLength` = 10
  - `RsiLength` = 14
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Configurable
  - Indicadores: EMA, RSI
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
