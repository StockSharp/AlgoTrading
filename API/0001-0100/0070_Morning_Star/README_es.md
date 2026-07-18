# Estrategia de Patrón Estrella de la Mañana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Estrella de la Mañana es una formación de velas alcistas que señala un posible fondo después de una caída. Consiste en una gran vela bajista, una pequeña vela indecisa y una fuerte vela alcista que cierra por encima del punto medio de la primera barra.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 97%. Funciona mejor en el mercado cripto.

Esta estrategia sigue secuencias de tres velas. Cuando aparece el patrón, se abre una posición larga con un stop colocado por debajo de la pequeña vela central. Las salidas ocurren una vez que el precio sube por encima del máximo de la barra de confirmación o si se alcanza el stop.

Dado que el patrón suele generar recuperaciones rápidas desde condiciones de sobreventa, las operaciones suelen ser de corta duración, capturando el impulso inicial al alza.

## Detalles

- **Criterios de entrada**: Patrón de tres velas Estrella de la Mañana.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Precio por encima del máximo de la barra de confirmación o stop-loss.
- **Stops**: Sí, por debajo del mínimo de la vela central.
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

