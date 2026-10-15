# Estrategia de Señal de Vela Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en función del cruce entre los valores de momentum calculados a partir de los precios de apertura y cierre de las velas. Cuando el momentum del precio de apertura cae por debajo del momentum del precio de cierre, señala una presión alcista creciente y la estrategia entra en una posición larga. El cruce opuesto indica presión bajista y activa una posición corta.

Por defecto, la estrategia opera en velas de 12 horas con un período de momentum de 12.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El momentum de apertura cruza por debajo del momentum de cierre.
  - **Corto**: El momentum de apertura cruza por encima del momentum de cierre.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: Ninguno.
- **Filtros**: Ninguno.
