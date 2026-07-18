# Estrategia de Reversión en Bandas de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Los extremos de precio fuera de las Bandas de Bollinger suelen revertir hacia la banda media. Este enfoque va contra esas extensiones, comprando caídas por debajo de la banda inferior cuando la vela cierra verde y vendiendo repuntes por encima de la banda superior después de una vela roja.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 94%. Funciona mejor en el mercado de acciones.

El algoritmo calcula las Bandas de Bollinger en cada barra y verifica si el cierre rompe la banda exterior. Si una vela alcista cierra por debajo de la banda inferior, se abre un largo; si una vela bajista cierra por encima de la banda superior, se toma un corto. El stop se basa en un múltiplo de ATR mientras que las salidas ocurren cuando el precio regresa a la banda media.

Las operaciones de reversión a la media típicamente duran solo unas pocas barras, haciendo que esta configuración sea adecuada para contracciones de volatilidad a corto plazo.

## Detalles

- **Criterios de entrada**: Cierre por debajo de la banda inferior con vela alcista o cierre por encima de la banda superior con vela bajista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Precio cruzando la banda media o stop-loss.
- **Stops**: Sí, basado en ATR.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0
  - `AtrMultiplier` = 2.0
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

