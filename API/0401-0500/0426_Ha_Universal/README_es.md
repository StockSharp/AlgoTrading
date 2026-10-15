# Estrategia Universal Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta plantilla universal convierte velas estándar en velas Heikin Ashi y opera en la dirección de su cuerpo. El método suaviza el ruido del precio, permitiendo que las tendencias aparezcan con mayor claridad. Es ligero y puede servir como base para filtros o salidas personalizadas.

El sistema entra en largo cuando el cierre de Heikin Ashi está por encima de su apertura y cambia a corto cuando el cierre cae por debajo de la apertura.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `HA_Close > HA_Open`
  - **Corto**: `HA_Close < HA_Open`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Heikin Ashi
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
