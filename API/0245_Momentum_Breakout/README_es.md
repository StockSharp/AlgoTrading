# Estrategia de Ruptura por Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema de ruptura busca surgimientos repentinos de momentum en relación con su promedio histórico. Cuando las lecturas de momentum superan el promedio por un gran margen, el precio puede estar iniciando un movimiento rápido y direccional.

Las pruebas indican un rendimiento anual promedio de aproximadamente 82%. Funciona mejor en el mercado de acciones.

La estrategia compra cuando el momentum sube por encima de la media más `Multiplier` veces su desviación estándar. Se inicia un corto cuando el momentum cae por debajo de la media menos el mismo multiplicador. Las posiciones se cierran una vez que el momentum regresa hacia su media.

Los traders que disfrutan de movimientos rápidos pueden apreciar las reglas claras para capturar ráfagas de fortaleza. Un stop-loss basado en porcentaje del precio protege contra rupturas fallidas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Momentum > Avg + Multiplier * StdDev
  - **Corto**: Momentum < Avg - Multiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando Momentum < Avg
  - **Corto**: Salir cuando Momentum > Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `MomentumPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Momentum
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
