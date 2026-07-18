# Estrategia de Patrón Estrella Vespertina
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Estrella Vespertina es la imagen especular de la Estrella de la Mañana pero indica un posible techo. Comienza con una fuerte vela alcista, seguida de una pequeña vela de indecisión, y termina con una vela bajista que cierra por debajo del punto medio de la primera barra.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 100%. Funciona mejor en el mercado forex.

El algoritmo observa secuencias de tres velas. Cuando se forma el patrón, entra en corto con un stop por encima del máximo de la pequeña vela central. Las posiciones salen una vez que el precio cae por debajo del mínimo de la vela de confirmación o si el stop es activado.

Dado que la configuración anticipa una reversión rápida desde condiciones de sobrecompra, las operaciones típicamente apuntan a movimientos cortos impulsados por el momentum a la baja.

## Detalles

- **Criterios de entrada**: Patrón de tres velas Estrella Vespertina.
- **Largo/Corto**: Solo cortos.
- **Criterios de salida**: Precio por debajo del mínimo de la barra de confirmación o stop-loss.
- **Stops**: Sí, por encima del máximo de la vela central.
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Corto
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

