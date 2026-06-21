# Estrategia de Scalping EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un sistema de scalping simple que mantiene constantemente dos órdenes pendientes: un buy stop por encima del mercado y un sell stop por debajo. Cuando el precio de mercado se acerca demasiado a una orden o se aleja demasiado, la orden se reemplaza para mantener una distancia fija respecto al precio actual. Las órdenes ejecutadas utilizan offsets fijos de take profit y stop loss.

La estrategia no depende de indicadores y reacciona únicamente a los cambios de precio por tick.

## Detalles

- **Criterios de entrada**:
  - Colocar buy stop 100 puntos por encima del precio y sell stop 100 puntos por debajo.
  - Las órdenes se reemplazan si la distancia al precio se vuelve demasiado pequeña o demasiado grande.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cada orden lleva take profit y stop loss fijos.
- **Stops**: Sí, distancia fija.
- **Filtros**: Ninguno.
