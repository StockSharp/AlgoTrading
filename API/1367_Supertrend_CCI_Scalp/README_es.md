# Estrategia de Scalp Supertrend & CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Supertrend & CCI Scalp utiliza dos líneas Supertrend y un CCI suavizado para capturar reversiones a corto plazo.
Compra cuando el primer Supertrend está por encima del precio, el segundo está por debajo del precio y el CCI suavizado está por debajo de -100. La lógica corta refleja esta configuración.

## Detalles

- **Criterios de entrada**: Supertrend1 por encima del precio, Supertrend2 por debajo del precio, CCI suavizado < -100 (largo); lo contrario para corto
- **Largo/Corto**: Ambos
- **Criterios de salida**: Alineación opuesta de Supertrend o CCI cruzando ±100
- **Stops**: No
- **Valores predeterminados**:
  - `AtrLength1` = 14
  - `Factor1` = 3
  - `AtrLength2` = 14
  - `Factor2` = 6
  - `CciLength` = 20
  - `SmoothingLength` = 5
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CciLevel` = 100
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Supertrend, CCI, Moving Average
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

