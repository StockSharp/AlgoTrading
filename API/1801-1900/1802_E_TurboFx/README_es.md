# Estrategia E TurboFx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de reversión de momentum adaptada del experto MQL5 "e-TurboFx". El sistema observa una serie de velas cuyos cuerpos crecen en tamaño en la misma dirección. Después de varias velas bajistas con cuerpos en expansión, la estrategia compra esperando un rebote. Después de varias velas alcistas con cuerpos crecientes, vende. El stop-loss y el take-profit opcionales se establecen en puntos de precio brutos.

## Detalles

- **Criterios de entrada**:
  - Largo: `N` velas bajistas consecutivas y cada cuerpo mayor que el anterior
  - Corto: `N` velas alcistas consecutivas y cada cuerpo mayor que el anterior
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop-loss o take-profit
- **Stops**: Puntos mediante `StartProtection`
- **Valores predeterminados**:
  - `BarsCount` = 3
  - `StopLossPoints` = 700
  - `TakeProfitPoints` = 1200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Price Action
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
