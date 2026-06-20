# Estrategia de Ruptura de Barra Interior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una barra interior se forma cuando el rango de una vela está completamente contenido dentro del máximo y el mínimo de la barra anterior. Señala una indecisión de corto plazo que puede llevar a una ruptura una vez que el precio supera el patrón. Esta estrategia espera esa ruptura y luego opera en la dirección de la expansión.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 118%. Funciona mejor en el mercado de acciones.

Cada nueva vela se compara con la anterior. Si aparece una barra interior, el sistema marca su máximo y mínimo y observa un cierre fuera de esos niveles. Una ruptura alcista abre una posición larga con un stop por debajo del mínimo del patrón, mientras que una ruptura bajista activa un corto con un stop por encima del máximo del patrón.

Si el precio no logra romper inmediatamente, la estrategia gestiona las posiciones existentes saliendo si la siguiente vela se mueve en contra de la operación más allá de los extremos de la barra anterior.

## Detalles

- **Criterios de entrada**: Ruptura del máximo o mínimo de una barra interior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Precio cruzando el extremo de la vela anterior o stop-loss.
- **Stops**: Sí, colocados más allá del patrón.
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

