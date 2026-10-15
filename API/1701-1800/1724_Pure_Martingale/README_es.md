# Estrategia Martingale Puro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema de Martingale básico. Abre operaciones en una dirección aleatoria y duplica el tamaño de la posición y la distancia de stop/take después de cada operación perdedora. Tras una operación ganadora, se restablece al volumen y la distancia iniciales.

El enfoque asume que el precio eventualmente volverá a la rentabilidad, pero el riesgo crece exponencialmente. Úselo solo en instrumentos líquidos con spreads ajustados.

## Detalles

- **Criterios de entrada**:
  - Sin posición abierta: comprar o vender aleatoriamente en el cierre de la vela.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cerrar cuando el precio se mueva a favor o en contra de la posición la distancia configurada.
- **Stops**: Stop loss y take profit virtuales gestionados por la estrategia.
- **Filtros**:
  - Ninguno.
