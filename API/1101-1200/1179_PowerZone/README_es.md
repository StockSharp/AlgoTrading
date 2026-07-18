# Estrategia PowerZone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia identifica bloques de órdenes "PowerZone" creados por una vela bajista seguida de velas alcistas consecutivas (y viceversa). Una ruptura por encima de la zona alcista activa una operación larga, mientras que una ruptura por debajo de la zona bajista abre una corta. Los objetivos y stops se basan en el rango de la zona.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Vela bajista hace `Periods+1` barras seguida de `Periods` velas alcistas y el precio rompe por encima del máximo de la zona.
  - **Corto**: Vela alcista hace `Periods+1` barras seguida de `Periods` velas bajistas y el precio rompe por debajo del mínimo de la zona.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Take profit y stop loss como múltiplos del rango de la zona.
- **Indicadores**: Ninguno.
- **Valores predeterminados**:
  - `Periods` = 5
  - `Threshold` = 0
  - `UseWicks` = false
  - `Take Profit Factor` = 1.5
  - `Stop Loss Factor` = 1
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
