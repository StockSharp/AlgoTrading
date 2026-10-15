# Estrategia de Cierre de Posiciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Cierra posiciones abiertas basándose en reglas de ganancia, pérdida o tiempo. Esta estrategia no abre nuevas órdenes.

## Detalles

- **Criterios de entrada**: ninguno, se asume que las posiciones son abiertas externamente.
- **Criterios de salida**:
  - Se alcanza el límite de ganancia o pérdida medido en pips.
  - La antigüedad de la posición supera el límite de tiempo en minutos.
  - El tiempo actual es posterior al horario de cierre configurado.
- **Stops**: umbrales implícitos de ganancia y pérdida.
- **Filtros**: hora del día y tiempo de mantenimiento.
