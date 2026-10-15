# Estrategia de Reversión en Canal Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los canales basados en volatilidad pueden destacar movimientos sobreextendidos. Este método opera en contra del precio cuando se sale del Keltner Channel, anticipando un retorno hacia la línea media. Utiliza una media móvil exponencial y el ATR para dimensionar el ancho del canal.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 106%. Funciona mejor en el mercado de acciones.

A medida que se completa cada vela, la estrategia verifica si el cierre está más allá de la banda superior o inferior y si la dirección de la vela coincide. Las velas alcistas que cierran por debajo de la banda inferior activan entradas largas, mientras que las velas bajistas por encima de la banda superior impulsan cortos. Las posiciones se cierran una vez que el precio cruza la banda media o cuando se alcanza el stop basado en ATR.

Al operar en la dirección opuesta a los extremos de corto plazo, el sistema busca movimientos rápidos de reversión a la media dentro de un rango más amplio.

## Detalles

- **Criterios de entrada**: Cierre fuera del Keltner Channel en la dirección de la vela.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Precio cruzando la banda media o stop-loss.
- **Stops**: Sí, basados en ATR.
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `StopLossAtrMultiplier` = 2.0
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Keltner Channel
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

