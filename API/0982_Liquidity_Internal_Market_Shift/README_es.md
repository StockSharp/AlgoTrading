# Estrategia de Cambio Interno del Mercado de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia detecta cambios en la estructura interna del mercado que coinciden con barridas de liquidez en máximos o mínimos recientes. Se abre una operación cuando el precio toca una línea de liquidez y luego cambia de estructura en la dirección opuesta. Las operaciones pueden limitarse solo a configuraciones alcistas, bajistas o ambas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio cierra por encima de la estructura bajista previa y ha tocado la línea de liquidez del mínimo reciente.
  - **Corto**: El precio cierra por debajo de la estructura alcista previa y ha tocado la línea de liquidez del máximo reciente.
- **Largo/Corto**: Ambas direcciones o seleccionable Solo alcista / Solo bajista.
- **Criterios de salida**:
  - Señal opuesta tras la entrada.
  - Stop-loss en `StopLossPips` pips.
  - Take-profit opcional en `TakeProfitPips` pips.
- **Stops**: Sí, stop-loss configurable y take-profit opcional.
- **Filtros**:
  - Solo opera dentro del rango horario especificado.
  - El bloqueo de señal previene entradas repetidas durante varias barras.
