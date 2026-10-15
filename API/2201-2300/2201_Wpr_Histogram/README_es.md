# Estrategia de Histograma WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en función del comportamiento del indicador Williams %R. Monitorea cuándo el indicador abandona las zonas de sobrecompra o sobreventa y abre posiciones en la dirección opuesta.

## Lógica

- Cuando Williams %R sube por encima del nivel alto y luego cae de vuelta, se considera que el mercado abandona la zona de sobrecompra. La estrategia abre una posición larga.
- Cuando Williams %R cae por debajo del nivel bajo y luego sube de vuelta, el mercado abandona la zona de sobreventa. La estrategia abre una posición corta.
- Las posiciones opuestas existentes se cierran antes de abrir una nueva.

## Parámetros

- **WPR Period** – período de cálculo de Williams %R.
- **High Level** – umbral para la zona de sobrecompra.
- **Low Level** – umbral para la zona de sobreventa.
- **Candle Type** – tipo y marco temporal de las velas utilizadas para los cálculos.

## Notas

La estrategia utiliza únicamente órdenes de mercado y no establece niveles de stop-loss ni take-profit. El dimensionamiento de posiciones depende de la propiedad `Volume` definida por el usuario.
