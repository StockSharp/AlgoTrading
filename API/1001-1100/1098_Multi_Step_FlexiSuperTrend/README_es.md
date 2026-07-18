# Estrategia Multi-Paso FlexiSuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un filtro SuperTrend combinado con un oscilador de desviación suavizado.
La estrategia incluye tres niveles de take profit configurables.

## Detalles

- **Criterios de entrada**:
  - Precio por debajo de SuperTrend y desviación (SMA del precio menos SuperTrend) > 0 → comprar.
  - Precio por encima de SuperTrend y desviación < 0 → vender.
- **Largo/Corto**: Largo, corto o ambas direcciones.
- **Criterios de salida**:
  - Take profit parcial en 3 niveles.
  - Posición restante cerrada en reversión de tendencia cuando el precio cruza SuperTrend.
- **Stops**: Sin lógica de stop por defecto.
- **Valores predeterminados**:
  - Período ATR = 10.
  - Factor ATR = 3.0.
  - Longitud SMA = 10.
  - Niveles de take profit = 2%, 8%, 18%.
  - Porcentajes de take profit = 30%, 20%, 15%.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend, SMA
  - Stops: Take profit
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
