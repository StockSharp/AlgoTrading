# Estrategia de MA Ponderada por Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Beta Weighted MA (BWMA) utiliza una distribución Beta para ponderar los precios recientes, produciendo una media móvil cuyo retraso y suavidad pueden ajustarse con los parámetros alpha y beta. La estrategia entra en una posición larga cuando el precio cruza por encima de la BWMA y en una posición corta cuando el precio cruza por debajo.

## Detalles

- **Criterios de entrada**:
  - El precio cruza por encima de la Beta Weighted Moving Average → entrar largo.
  - El precio cruza por debajo de la Beta Weighted Moving Average → entrar corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - El cruce opuesto cierra la posición actual y abre la inversa.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 50
  - `Alpha` = 3
  - `Beta` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo/Corto
  - Indicadores: Beta Weighted Moving Average
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
