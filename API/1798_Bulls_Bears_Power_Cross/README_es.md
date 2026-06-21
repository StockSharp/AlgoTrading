# Estrategia de Cruce Bulls & Bears Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el cruce de los indicadores Bulls Power y Bears Power en un marco temporal de cuatro horas. Bulls Power mide la presión compradora por encima de un precio promedio, mientras que Bears Power muestra la presión vendedora por debajo de él. Cuando la fuerza compradora supera la fuerza vendedora, el sistema abre una posición larga. Cuando la fuerza vendedora se vuelve dominante, abre una posición corta.

Las pruebas en datos históricos de criptomonedas muestran que los cruces claros suelen preceder a reversiones a corto plazo. La estrategia está diseñada para estar siempre larga o corta, revirtiendo la posición cada vez que los indicadores se cruzan en la dirección opuesta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El valor de Bulls Power cruza por encima de Bears Power.
  - **Corto**: El valor de Bears Power cruza por encima de Bulls Power.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto que invierte la posición.
- **Stops**: Ninguno. Las posiciones se revierten en lugar de cerrarse por stop.
- **Filtros**:
  - Marco temporal: velas de 4 horas por defecto.
  - Indicadores: Bulls Power, Bears Power.
  - Dirección: Reversión basada en cambio de momentum.
