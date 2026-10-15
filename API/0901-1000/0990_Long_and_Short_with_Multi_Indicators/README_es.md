# Estrategia Long y Short con Múltiples Indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza RSI, Rate of Change y una media móvil seleccionable para generar señales largas y cortas. Aplica un stop trailing basado en ATR para las salidas.

## Detalles

- **Criterios de entrada**:
  - Largo: RSI entre sobrevendido y sobrecomprado, ROC > 0 y precio por encima de la MA.
  - Corto: Tendencia bajista confirmada, ROC < 0 y precio por debajo de la MA.
- **Largo/Corto**: Largo y corto.
- **Criterios de salida**:
  - Stop trailing basado en ATR o condiciones de stop por indicador.
- **Stops**: Stop trailing ATR.
- **Valores predeterminados**:
  - `RsiLength` = 5
  - `RsiOverbought` = 70
  - `RsiOversold` = 44
  - `RocLength` = 4
  - `MaLength` = 24
  - `MaTypeParam` = TEMA
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `BearishMaLength` = 200
  - `BearishTrendDuration` = 5
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo & Corto
  - Indicadores: RSI, ROC, MA, ATR
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
