# Estrategia de Tick Marubozu
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Identifica velas Marubozu en datos de tick y las confirma con alto volumen. Compra en Marubozu alcistas y vende en bajistas.

## Detalles

- **Criterios de entrada**: Marubozu alcista o bajista con volumen por encima de la SMA
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `TickSize` = 5
  - `VolLength` = 20
  - `CandleType` = 1-minute time frame
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
