# Estrategia de Gestión de Riesgo en Salida Long/Short
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia plantilla que muestra cómo gestionar posiciones largas y cortas con controles de riesgo basados en porcentajes. Utiliza disparadores simples de igualdad de precio y salidas opcionales por tiempo.

## Detalles

- **Criterios de entrada**: El precio de cierre es igual al valor largo o corto configurado.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop loss, take profit o salida temporal después de N barras.
- **Stops**: Stop loss y take profit porcentuales con trailing opcional.
- **Valores predeterminados**:
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `ExitBars` = 10
  - `BarsToWait` = 10
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Gestión de riesgo
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
