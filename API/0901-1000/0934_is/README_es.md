# Estrategia IS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia simple que abre una posición larga cuando la fuente seleccionada es igual al valor de activación largo y la cierra cuando el valor cambia al opuesto. Si la venta en corto está habilitada, la estrategia también abre una posición corta en la señal opuesta. El take-profit y el stop-loss se especifican como porcentajes del precio de entrada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La fuente es igual al valor largo y el valor anterior era diferente.
  - **Corto**: La fuente es igual al valor corto y el valor anterior era diferente (si los cortos están habilitados).
- **Criterios de salida**: Señal inversa o stop de protección.
- **Stops**: Sí, take-profit y stop-loss como porcentajes.
