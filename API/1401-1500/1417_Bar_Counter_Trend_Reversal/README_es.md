# Estrategia de Reversión Contratendencia de Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Busca varias barras consecutivas alcistas o bajistas y toma operaciones contratendencia cuando el precio alcanza los extremos del canal.

## Detalles

- **Criterios de entrada**: serie de subidas o bajadas con confirmación opcional de volumen y canal
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `NoOfRises` = 3
  - `NoOfFalls` = 3
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Keltner Channel o Bollinger Bands
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
