# Estrategia de Ángulo LSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el ángulo de la Media Móvil de Mínimos Cuadrados (LSMA) para detectar la dirección de la tendencia. El ángulo se aproxima mediante la diferencia entre dos valores de LSMA separados por un número configurable de barras.

- **Entrada larga**: el ángulo LSMA sube por encima del umbral positivo.
- **Salida larga**: el ángulo vuelve por debajo del umbral positivo.
- **Entrada corta**: el ángulo LSMA cae por debajo del umbral negativo.
- **Salida corta**: el ángulo vuelve por encima del umbral negativo.

## Parámetros
- `LSMA Period`: longitud para el cálculo de LSMA.
- `Angle Threshold`: valor absoluto que define la zona neutral alrededor de cero.
- `Start Shift`: barra más antigua utilizada para calcular el ángulo.
- `End Shift`: barra más reciente utilizada para calcular el ángulo.
- `Candle Type`: tipo de datos de vela para el cálculo.

## Notas
- Los valores del ángulo se escalan a puntos según el instrumento (1000 para pares JPY, de lo contrario 100000).
- Funciona solo en velas completadas.
