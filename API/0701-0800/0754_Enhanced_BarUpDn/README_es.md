# Estrategia BarUpDn Mejorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia busca barras alcistas o bajistas combinadas con Bollinger Bands y confirmación de tendencia. Entra largo en gaps alcistas durante tendencias alcistas y corto en gaps bajistas durante tendencias bajistas. Las salidas utilizan niveles de stop-loss y take-profit basados en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: vela alcista con gap al alza, cierre por encima de la MA de tendencia y por encima de la banda inferior de Bollinger.
  - Corto: vela bajista con gap a la baja, cierre por debajo de la MA de tendencia y por debajo de la banda superior de Bollinger.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - El precio toca el stop-loss o take-profit basado en ATR (1.5× ATR).
- **Stops**: Stop y take-profit basados en ATR.
- **Valores predeterminados**:
  - `BbLength` = 20
  - `BbMultiplier` = 2
  - `MaLength` = 50
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 2
  - `AtrMultiplierTp` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, SMA, ATR
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
