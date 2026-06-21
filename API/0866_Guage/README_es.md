# Estrategia Gauge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia imita la biblioteca Gauge de TradingView midiendo la posición del precio entre un mínimo y un máximo definidos por el usuario. Cuando el porcentaje cruza los umbrales superior o inferior, entra en operaciones en la dirección correspondiente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: ratio del gauge por encima del umbral superior.
  - **Corto**: ratio del gauge por debajo del umbral inferior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Una señal opuesta genera una salida.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - Min value = 0, Max value = 100.
  - Upper threshold = 75%, Lower threshold = 25%.
- **Filtros**:
  - Categoría: Rango / Utilidad
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
