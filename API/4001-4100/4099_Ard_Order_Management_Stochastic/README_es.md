# Estrategia de gestión de pedidos ARD Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **Ard Order Management** es una StockSharp conversión del MetaTrader experto `ARD_ORDER_MANAGEMENT_EA-BETA_1`. El guión original se centraba en cerrar repetidamente posiciones existentes antes de realizar nuevos pedidos y ofrecía rutinas de ayuda para actualizaciones manuales de stop-loss y take-profit. La versión StockSharp mantiene esta disciplina al tiempo que agrega automatización basada en indicadores basada en el oscilador Stochastic.

La configuración predeterminada apunta al comercio de divisas intradía en un gráfico de 5 minutos, pero el tipo de vela es completamente configurable. Toda la lógica comercial se ejecuta en velas terminadas para permanecer fiel al estilo de ejecución de final de barra del experto fuente.

## Lógica de trading
- Un oscilador Stochastic con períodos configurables de **retrospectiva**, **señal** y **desaceleración** genera señales direccionales (valor predeterminado: 5/3/3).
- Cuando %K cierra **por encima del umbral de compra** (80 de forma predeterminada), la estrategia cancela las órdenes pendientes, cierra cualquier exposición corta abierta y entra en una posición larga con el volumen configurado.
- Cuando %K cierra **por debajo del umbral de venta** (20 por defecto), se cancelan todas las órdenes pendientes, se cierra la exposición larga abierta y se abre una nueva venta corta.
- La estrategia permanece en la nueva posición hasta que se dispara la señal opuesta o se activa una salida protectora.

## Gestión de pedidos y riesgos
- Antes de cada nueva entrada, la estrategia emite órdenes de mercado que aplanan completamente la posición actual, replicando el comportamiento `open_order(CLOSE)` del EA.
- `StartProtection` envía automáticamente órdenes iniciales de stop-loss y take-profit de acuerdo con los parámetros `StopLossPips` y `TakeProfitPips`.
- La lógica de seguimiento opcional emula la rama `MODIFY` del EA: cada vela terminada puede actualizar un nivel de parada dinámico (`ModifyStopLossPips`) y un objetivo de ganancias flotante (`ModifyTakeProfitPips`). Cuando el precio toca cualquiera de los niveles finales, la posición se cierra para asegurar ganancias o limitar el riesgo.
- La conversión de pips utiliza el `PriceStep` del instrumento (con un ajuste de 10× para símbolos de divisas de pip fraccionario) para que los parámetros basados en la distancia se mantengan consistentes en todos los mercados.

## Parámetros
- **Volumen** – volumen de operaciones para nuevas entradas; El tamaño adicional se agrega automáticamente para cerrar posiciones opuestas.
- **TakeProfitPips / StopLossPips** – distancias de protección iniciales pasadas al módulo de protección incorporado. Establezca en cero para desactivar cualquier orden.
- **ModifyTakeProfitPips / ModifyStopLossPips**: compensaciones finales (en pips) recalculadas después de cada vela. Establezca en cero para deshabilitar las actualizaciones finales.
- **StochasticPeriod / SignalPeriod / SlowingPeriod**: configuración del oscilador que refleja la llamada `iStochastic` del experto original.
- **BuyThreshold / SellThreshold**: niveles de sobrecompra/sobreventa que desencadenan reversiones largas/cortas.
- **CandleType**: período de tiempo o fuente de datos de velas personalizada que alimenta el indicador.

Cada parámetro expone rangos de optimización sensibles para que pueda probar configuraciones alternativas en el optimizador StockSharp.

## Notas de uso
- Funciona mejor en instrumentos líquidos donde las paradas basadas en pips son significativas (principales pares de divisas, CFD sobre índices, futuros líquidos).
- Aumente el plazo al operar en mercados más lentos para reducir el ruido y las falsas reversiones.
- Cuando se ejecuta en cuentas reales, verifique que el volumen configurado respete los mínimos del corredor y los tamaños de paso.
- La lógica de seguimiento se puede desactivar dejando los parámetros `Modify*` en cero, reproduciendo efectivamente el mantenimiento del orden estático de la fuente EA.
- Combínelo con filtros adicionales (tendencia, volatilidad, sesiones) si desea entradas más selectivas: el código expone propiedades que se pueden ampliar.

## Detalles de conversión
- Archivo fuente: `MQL/9041/ARD_ORDER_MANAGEMENT_EA-BETA_1.mq4`.
- Se recreó la lógica de activación Stochastic insinuada en la rutina `start()` comentada.
- Preservó la disciplina de cierre antes de abrir y la colocación de órdenes de protección a través del alto nivel de StockSharp API.
- Se agregaron salidas finales opcionales para reflejar el bloque manual `MODIFY` del EA mientras se mantiene la implementación basada en indicadores y eventos.
