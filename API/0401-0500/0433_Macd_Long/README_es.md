# Estrategia de MACD Long
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina los extremos del Relative Strength Index con los cruces de MACD para capturar retrocesos dentro de una tendencia. Después de que el RSI alcanza una lectura extrema, el sistema espera un cruce de MACD confirmatorio antes de entrar. Este enfoque filtra los cambios de momentum ruidosos y se centra en reversiones de alta probabilidad.

La estrategia opera en ambas direcciones y puede cambiar rápidamente cuando aparecen señales opuestas. MACD proporciona confirmación de momentum mientras RSI destaca zonas de sobrecompra y sobreventa. Se pueden agregar stops protectores a través de los controles de riesgo del motor.

## Detalles

- **Criterios de entrada**:
  - **Largo**: RSI cae por debajo de la sobreventa, luego la línea MACD cruza por encima de la señal.
  - **Corto**: RSI sube por encima de la sobrecompra, luego la línea MACD cruza por debajo de la señal.
- **Criterios de salida**:
  - Cruce opuesto o stop activado.
- **Indicadores**:
  - RSI (longitud 14, sobreventa 30, sobrecompra 70)
  - MACD (rápida 12, lenta 26, señal 9)
- **Stops**: Implementar mediante StartProtection o gestión de capital externa.
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `Oversold` = 30
  - `Overbought` = 70
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **Filtros**:
  - Reversión de momentum
  - Funciona en varios marcos temporales
  - Indicadores: RSI, MACD
  - Stops: Opcional
  - Complejidad: Básico
