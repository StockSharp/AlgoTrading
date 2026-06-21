# Ejemplo de Estrategia de Trailing Escalonado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de muestra que demuestra la gestión de operaciones en tres pasos con stop trailing opcional.

La estrategia entra largo cuando la SMA de 14 períodos cruza por encima de la SMA de 28 períodos. El riesgo se controla mediante un stop-loss y tres objetivos de beneficio:
- Después del primer objetivo, el stop se mueve al punto de equilibrio.
- Después del segundo objetivo, el stop se mueve al primer objetivo.
- En el tercer paso, la posición sale en el objetivo tres o inicia un stop trailing.

Este ejemplo muestra cómo escalonar beneficios y proteger posiciones a medida que avanzan.
