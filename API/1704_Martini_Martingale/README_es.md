# Estrategia Martini Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa una cuadrícula de martingala con cobertura. Comienza colocando órdenes stop en ambos lados del precio actual y duplica el tamaño de la posición en la dirección opuesta siempre que el mercado se mueva contra la exposición actual en un paso especificado. Todas las operaciones se cierran una vez que el beneficio acumulado supera el objetivo.

## Detalles

- **Criterios de entrada**:
  - Colocar un buy stop por encima y un sell stop por debajo del mercado a distancia `Step`.
  - Cuando se activa una orden, cancelar el stop opuesto.
- **Gestión de la posición**:
  - Rastrear el precio de la última orden ejecutada.
  - Si el precio se mueve contra la posición abierta en `Step * orderCount`, enviar una orden de mercado en la dirección opuesta con el doble del volumen anterior.
- **Criterios de salida**:
  - Cerrar todas las posiciones cuando el beneficio no realizado alcanza `ProfitClose`.
- **Largo/Corto**: Ambos.
- **Stops**: Usa órdenes stop para entradas iniciales; sin stop-loss.
- **Indicadores**: Ninguno.
- **Filtros**: Ninguno.

### Parámetros

- `Step` – paso de precio en unidades absolutas.
- `ProfitClose` – umbral de beneficio para cerrar todas las operaciones.
- `InitialVolume` – volumen inicial para la primera orden.
- `CandleType` – serie de velas usada para actualizaciones de precio.
