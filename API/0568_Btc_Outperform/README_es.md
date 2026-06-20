# Estrategia de Superación de Rendimiento BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Compara los precios de cierre semanales y trimestrales. Va en largo cuando el precio semanal es mayor que el precio trimestral, y va en corto cuando el precio trimestral es mayor.

## Detalles
- **Criterios de entrada:**
  - **Largo:** cierre semanal > cierre trimestral.
  - **Corto:** cierre trimestral > cierre semanal.
- **Largo/Corto:** Ambos.
- **Criterios de salida:** Señal inversa.
- **Stops:** Ninguno.
- **Valores predeterminados:** Semanal = 7 días, Trimestral = 90 días.
