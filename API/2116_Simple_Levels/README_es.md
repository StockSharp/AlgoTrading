# Estrategia de Niveles Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Abre operaciones cuando el precio cruza líneas de tendencia definidas por el usuario. Cada línea puede activar operaciones largas, cortas o ambas direcciones. El stop loss y el take profit se establecen en pasos de precio.

## Detalles

- **Criterios de entrada**: Precio cruzando una línea de tendencia configurada
- **Largo/Corto**: Determinado por la dirección de la línea (Buy/Sell/Both)
- **Criterios de salida**: Niveles de stop loss o take profit
- **Stops**: Sí
- **Valores predeterminados**:
  - `StopLoss` = 300 steps
  - `TakeProfit` = 900 steps
  - `Volume` = 1
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoría: Niveles
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Uso

1. Crear y configurar líneas de tendencia mediante `AddLine`.
2. Iniciar la estrategia para monitorear las velas entrantes.
3. Cuando el precio cruza una línea activa en la dirección especificada, la estrategia envía una orden de mercado.
4. La posición se cierra cuando se alcanza el stop loss o el take profit.
