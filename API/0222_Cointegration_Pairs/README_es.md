# Estrategia de Pares por Cointegración
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera dos activos que comparten una relación de cointegración a largo plazo. Calculando el residuo entre el primer activo y un segundo activo ajustado por beta, busca desviaciones que históricamente revierten al equilibrio.

Las pruebas indican un retorno anual promedio de aproximadamente 103%. Funciona mejor en el mercado de acciones.

Una posición larga compra el primer activo y vende el segundo cuando el z-score residual cae por debajo de `-EntryThreshold`. Una posición corta vende el primero y compra el segundo cuando el z-score sube por encima del umbral. Las posiciones se cierran una vez que el diferencial se normaliza hacia cero.

El trading de pares por cointegración es adecuado para arbitrajistas estadísticos cómodos gestionando dos instrumentos simultáneamente. El stop-loss incorporado protege contra movimientos extremos si la relación se rompe temporalmente.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Z-Score Residual < -EntryThreshold
  - **Corto**: Z-Score Residual > EntryThreshold
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando |Z-Score| < 0.5
  - **Corto**: Salir cuando |Z-Score| < 0.5
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `Period` = 20
  - `EntryThreshold` = 2.0m
  - `Beta` = 1.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Arbitraje
  - Dirección: Ambos
  - Indicadores: Cointegración
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
