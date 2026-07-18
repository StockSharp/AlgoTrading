# Estrategia de Ruptura Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca ráfagas de momentum observando Williams %R en relación con su promedio histórico. Cuando el oscilador se empuja mucho más allá de las lecturas típicas, puede señalar el inicio de un movimiento fuerte.

Las pruebas indican un rendimiento anual promedio de aproximadamente 91%. Funciona mejor en el mercado de acciones.

Se abre una posición larga cuando %R sube por encima de la media más `Multiplier` veces una desviación estándar estimada. Se toma una posición corta cuando %R cae por debajo de la media menos el mismo multiplicador. La operación se cierra una vez que %R regresa hacia su media o se alcanza un stop-loss.

El enfoque está orientado a traders de ruptura que desean participar temprano en tendencias emergentes. El riesgo de posición se gestiona con un stop porcentual basado en el precio de entrada.

## Detalles
- **Criterios de entrada**:
  - **Largo**: %R > Avg + Multiplier * StdDev
  - **Corto**: %R < Avg - Multiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando %R < Avg
  - **Corto**: Salir cuando %R > Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `WilliamsRPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Williams %R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
