# Estrategia de Entrada y Salida Aleatoria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza números aleatorios para entrar y salir de posiciones. Para cada vela finalizada se genera un valor aleatorio entre 0 y 1. Si el valor está por debajo del umbral de entrada, se abre una operación. Otro valor aleatorio controla las salidas. Las operaciones largas y cortas se pueden habilitar por separado.

## Detalles

- **Criterios de entrada**: valor aleatorio < Umbral de entrada.
- **Criterios de salida**: valor aleatorio < Umbral de salida.
- **Largo/Corto**: Ambos, configurable individualmente.
