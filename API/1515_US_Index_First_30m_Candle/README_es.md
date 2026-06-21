# Estrategia de la Primera Vela de 30m del Índice US
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Captura la ruptura del rango de los primeros 30 minutos de la sesión americana con una operación por día.

## Detalles

- **Criterios de entrada**: Tras fijar el rango de los primeros 30m, el precio rompe por encima del máximo o por debajo del mínimo
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop en el nivel opuesto del rango, objetivo al tamaño del rango * riesgo/beneficio
- **Stops**: Sí
- **Valores predeterminados**:
  - `RiskReward` = 1
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
