# Estrategia I-Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia I-Gap** replica el asesor experto "i-GAP" de MetaTrader. Monitoriza la brecha de precio entre el cierre de la vela anterior y la apertura de la vela actual. Una brecha de apertura bajista que supere un número especificado de pasos de precio puede desencadenar una entrada larga y opcionalmente cerrar posiciones cortas existentes. Una brecha alcista funciona de la misma manera para las posiciones cortas.

## Detalles
- **Criterios de entrada**: La brecha de apertura entre velas consecutivas supera el tamaño configurado.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal de brecha opuesta.
- **Stops**: Sin stop loss ni take profit fijos.
- **Valores predeterminados**:
  - `CandleType` = 1 hour
  - `GapSize` = 5
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
