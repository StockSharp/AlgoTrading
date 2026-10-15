# Estrategia de Rendimientos Mensuales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Rastrea máximos y mínimos pivote para operar rupturas y calcula los rendimientos mensuales y anuales compuestos del capital de la estrategia.

## Detalles

- **Criterios de entrada**: Comprar cuando el precio rompe por encima del último máximo pivote; vender cuando el precio rompe por debajo del último mínimo pivote.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Las posiciones se invierten con señales opuestas.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `LeftBars` = 2
  - `RightBars` = 1
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo y Corto
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
