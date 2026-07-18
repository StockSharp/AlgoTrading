# Estrategia TradePad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia TradePad es un panel de trading manual portado del experto TradePad MQL original. La estrategia configura un panel para gestionar operaciones de forma interactiva. Procesa datos de tick, notificaciones de operaciones, eventos de temporizador y mensajes de gráfico sin reglas automáticas de entrada o salida.

Este ejemplo demuestra cómo construir una interfaz de trading discrecional sobre StockSharp.

## Detalles

- **Criterios de entrada**: Colocación manual de órdenes a través del panel.
- **Largo/Corto**: Ambos, según la acción del usuario.
- **Criterios de salida**: Cierre manual de la posición.
- **Stops**: Ninguno; el usuario puede implementar lógica personalizada.
- **Filtros**: Sin filtros automáticos.
