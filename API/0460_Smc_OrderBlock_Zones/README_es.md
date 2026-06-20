# Estrategia de Zonas SMC de Bloques de Órdenes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia identifica máximos y mínimos de swing para definir zonas premium y de descuento. Una media móvil simple actúa como filtro de tendencia y los bloques de órdenes recientes confirman las entradas. Las operaciones se ejecutan cuando el precio se mueve de una zona hacia el equilibrio con la confirmación del bloque de órdenes, usando un stop loss porcentual para protección.

## Detalles

- **Criterios de entrada**:
  - Cierre por debajo del equilibrio pero por encima de la zona de descuento y SMA para operaciones largas.
  - Cierre por encima del equilibrio pero por debajo de la zona premium y SMA para operaciones cortas.
  - El precio debe tocar el nivel del bloque de órdenes respectivo.
- **Largo/Corto**: Largo, corto o ambos, configurable.
- **Criterios de salida**: Señal opuesta o stop loss.
- **Stops**: Stop loss porcentual.
- **Valores predeterminados**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoría: Tendencia y SMC
  - Dirección: Definido por el usuario
  - Indicadores: SMA, Highest, Lowest
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
