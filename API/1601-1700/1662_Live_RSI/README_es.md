# Estrategia RSI en Vivo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza múltiples cálculos de RSI (close, weighted, typical, median, open) y Parabolic SAR para detectar reversiones de tendencia. Entra en largo cuando los valores de RSI se alinean en orden alcista y el precio está por encima del SAR; entra en corto cuando la alineación es bajista y el precio está por debajo del SAR. El valor del SAR actúa como trailing stop.

## Detalles

- **Criterios de entrada**:
  - Largo cuando la secuencia RSI es alcista y el precio está por encima del SAR.
  - Corto cuando la secuencia RSI es bajista y el precio está por debajo del SAR.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal de tendencia opuesta o trailing stop SAR.
- **Stops**: Stop-loss fijo opcional más trailing stop basado en SAR.
- **Valores predeterminados**:
  - `RSI Period` = 30
  - `SAR Step` = 0.08
  - `Stop Loss` = 40
  - `Check Hour` = false
  - `Start Hour` = 17
  - `End Hour` = 1
  - `Candle Type` = 1 hora
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: RSI, Parabolic SAR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Opcional (filtro de tiempo)
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
