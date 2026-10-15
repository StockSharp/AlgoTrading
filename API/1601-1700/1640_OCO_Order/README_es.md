# Estrategia de Ejecución de Órdenes OCO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica un ticket de orden "Una Cancela la Otra" escrito originalmente para MetaTrader. Permite al trader definir hasta cuatro disparadores de precio independientes:

- **Buy Limit Price**
- **Sell Limit Price**
- **Buy Stop Price**
- **Sell Stop Price**

La estrategia se suscribe a datos Level1 para monitorear continuamente la mejor oferta y demanda. Cuando se alcanza un precio disparador, envía una orden de mercado en la dirección correspondiente. Después de ejecutarse una orden, se aplican protecciones de stop-loss y take-profit usando distancias medidas en pips. Estas distancias se convierten automáticamente a precios absolutos basándose en el `PriceStep` del instrumento.

Cuando el **modo OCO** está habilitado, alcanzar cualquier disparador desactivará automáticamente todos los demás, implementando eficazmente el comportamiento clásico de una-cancela-la-otra. Si el modo OCO está deshabilitado, los demás disparadores permanecen activos y pueden abrir posiciones adicionales cuando el precio continúa moviéndose.

## Detalles

- **Criterios de entrada**:
  - Largo cuando `Ask <= BuyLimitPrice` (disparador Buy Limit).
  - Largo cuando `Ask >= BuyStopPrice` (disparador Buy Stop).
  - Corto cuando `Bid >= SellLimitPrice` (disparador Sell Limit).
  - Corto cuando `Bid <= SellStopPrice` (disparador Sell Stop).
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Las posiciones se cierran automáticamente por niveles de stop-loss o take-profit predefinidos.
- **Stops**: Sí, stop-loss y take-profit en pips.
- **Valores predeterminados**:
  - `StopLossPips` = 300.
  - `TakeProfitPips` = 300.
  - `OCO Mode` = habilitado.
- **Filtros**:
  - Categoría: Ejecución de órdenes.
  - Dirección: Ambos.
  - Indicadores: Ninguno.
  - Stops: Sí.
  - Complejidad: Simple.
  - Marco temporal: Basado en ticks.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.
