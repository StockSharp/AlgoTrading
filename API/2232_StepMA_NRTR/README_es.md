# Estrategia StepMA NRTR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia basada en el indicador StepMA NRTR. El indicador combina una media móvil escalonada con un mecanismo de reversión Nick Rar Trend y genera señales de compra o venta cuando cambia la tendencia.

## Detalles

- **Criterios de entrada**: Señal de compra/venta de StepMA NRTR
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta de StepMA NRTR
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `Length` = 10
  - `Kv` = 1
  - `StepSize` = 0
  - `UseHighLow` = true
  - `CandleType` = Marco temporal 1 hora
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: StepMA NRTR
  - Stops: Ninguno
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
