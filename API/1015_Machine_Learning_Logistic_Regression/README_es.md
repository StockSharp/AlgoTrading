# Estrategia de Regresión Logística con Machine Learning
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia vuelve a entrenar un modelo simple de regresión logística en cada barra.
El modelo utiliza los precios de cierre recientes y una serie sintética derivada de ellos.
Si la probabilidad predicha de crecimiento supera 0.5, la estrategia entra en posición larga; de lo contrario, va en corto.
Las posiciones se mantienen durante un número fijo de barras.

## Detalles
- **Entrada**: predicción > 0.5 → largo, de lo contrario corto.
- **Salida**: señal opuesta o período de mantenimiento alcanzado.
- **Largo/Corto**: ambos.
- **Marco temporal**: configurable, predeterminado 1 minuto.
