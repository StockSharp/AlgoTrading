# Estrategia de Canal MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Canal MA opera rupturas de un canal de medias móviles construido a partir de los precios máximos y mínimos. Se abre una posición cuando el precio sale del canal en la dirección correspondiente y se revierte cuando la tendencia cambia. Los límites del canal se calculan a partir de medias móviles exponenciales con un desplazamiento fijo.

El sistema está diseñado tanto para operar en largo como en corto y reacciona únicamente a velas completadas. Su objetivo es capturar transiciones de tendencia de forma temprana evitando el ruido dentro del canal.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio rompe por encima del canal superior.
  - **Corto**: El precio rompe por debajo del canal inferior.
- **Criterios de salida**:
  - Una ruptura opuesta provoca la reversión de la posición.
- **Indicadores**: Medias móviles exponenciales de máximos y mínimos con longitud y desplazamiento configurables.
- **Stops**: No se usan por defecto; las operaciones se cierran solo con señales opuestas.
- **Valores predeterminados**:
  - `Length` = 8
  - `Offset` = 10
  - `CandleType` = velas de 1 hora
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
