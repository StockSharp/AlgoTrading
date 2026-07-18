# Estrategia de Seguimiento de Tendencia en Acciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera acciones individuales utilizando un filtro de tendencia simple. Las acciones que cotizan por encima de una media móvil se compran, mientras que las que cotizan por debajo se evitan o se venden en corto.

La cartera se actualiza semanalmente con tamaños de posición iguales y stops por arrastre para proteger el capital.

## Detalles

- **Datos**: Cierres diarios de acciones.
- **Entrada**: Comprar cuando el precio > media móvil; corto cuando está por debajo.
- **Salida**: El precio cruza de vuelta la media o se activa el stop.
- **Instrumentos**: Acciones líquidas.
- **Riesgo**: Trailing stop y límite de posición.

