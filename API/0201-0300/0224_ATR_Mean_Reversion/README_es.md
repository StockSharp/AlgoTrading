# Estrategia de Reversión a la Media con ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Reversión a la Media con ATR mide cuán lejos viaja el precio desde una media móvil en relación con la volatilidad reciente. El ATR proporciona una medida adaptativa para que los umbrales se expandan durante períodos activos y se contraigan cuando los mercados se calman.

Las pruebas indican un retorno anual promedio de aproximadamente 109%. Funciona mejor en el mercado de criptomonedas.

Una configuración larga ocurre cuando el precio cierra por debajo de la media móvil en más de `Multiplier` veces el ATR. Una configuración corta aparece cuando el precio cierra por encima de la media móvil la misma distancia. Las posiciones se cierran una vez que el precio regresa a la media móvil.

Esta técnica está destinada a traders de corto plazo que esperan que los precios reviertan después de movimientos excesivos. El stop basado en ATR mantiene el riesgo proporcional a las condiciones actuales del mercado.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Cierre < MA - Multiplier * ATR
  - **Corto**: Cierre > MA + Multiplier * ATR
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando cierre >= MA
  - **Corto**: Salir cuando cierre <= MA
- **Stops**: Sí, stop-loss alrededor de `2*ATR` por defecto.
- **Valores predeterminados**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: MA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
