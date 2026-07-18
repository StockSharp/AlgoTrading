# Estrategia Sea Dragon 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sea Dragon 2 es una estrategia de cuadrícula con cobertura que abre posiciones en ambas direcciones y añade nuevas órdenes cuando el precio se mueve un paso definido por el usuario. Los tamaños de las órdenes siguen una secuencia predefinida y los niveles de toma de ganancias se adaptan según el equilibrio entre la exposición larga y corta.

## Detalles

- **Órdenes iniciales**: Abre tanto una orden de compra como una de venta con el mismo volumen al inicio.
- **Adición de órdenes**: Cuando el mercado se mueve *Step* puntos desde el precio de la última orden, se agrega un nuevo par de órdenes. El lado con mayor exposición recibe la orden más grande según la secuencia.
- **Secuencia de volumen**: 1,1,2,3,6,9,14,22,33,48,82,111,122,164,185 escalado por *Volume Scale*.
- **Toma de ganancias**:
  - Cuando los conteos largo y corto son iguales, cada lado usa *Take Profit*.
  - Si un lado domina, ese lado usa *Alt Take Profit* mientras el otro mantiene *Take Profit*.
- **Stop Loss**: Cada lado tiene un stop colocado a *Max Stop* puntos de su precio promedio.
- **Fuente de datos**: La estrategia opera en velas completadas de tipo *Candle Type*.
- **Largo/Corto**: Ambos, con cobertura.
- **Salida**: Las posiciones se cierran cuando el precio alcanza los niveles de toma de ganancias o stop.
