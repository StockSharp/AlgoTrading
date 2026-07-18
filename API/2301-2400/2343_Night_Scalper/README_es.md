# Estrategia de Scalping Nocturno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera durante la sesión vespertina usando Bandas de Bollinger. Abre posiciones solo después de una hora de inicio especificada cuando el ancho de banda es estrecho y el precio rompe fuera de las bandas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: después de `Start Hour`, el precio cierra por debajo de la Banda de Bollinger inferior y el ancho de banda es menor que `Range Threshold`.
  - **Corto**: después de `Start Hour`, el precio cierra por encima de la Banda de Bollinger superior y el ancho de banda es menor que `Range Threshold`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La posición se cierra si el tiempo cae antes de `Start Hour` del día siguiente.
  - Stop-loss y take-profit protectores gestionados por `StartProtection`.
- **Stops**: Usa `StartProtection` con offsets fijos de stop-loss y take-profit.
- **Valores predeterminados**:
  - `BB Period` = 40
  - `BB Deviation` = 1
  - `Range Threshold` = 450
  - `Stop Loss` = 370
  - `Take Profit` = 20
  - `Start Hour` = 19
  - `Candle Type` = 1h
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
