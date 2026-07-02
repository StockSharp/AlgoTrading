# Parabolic SAR Estrategia de alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es el puerto StockSharp del MetaTrader 4 asesor experto `pSAR_alert.mq4`. El script original solo reproducía un sonido de alerta cada vez que el indicador Parabolic SAR cambiaba de un lado del precio al otro. La conversión mantiene la misma lógica de decisión pero convierte las alertas en órdenes de mercado reales, lo que permite que la señal se negocie automáticamente dentro de StockSharp.

## Lógica de trading
- La estrategia se suscribe al tipo de vela configurado y ejecuta un indicador Parabolic SAR con el factor de aceleración clásico (0,02) y la aceleración máxima (0,2) de forma predeterminada.
- Para cada vela terminada, la estrategia compara el valor Parabolic SAR con el cierre de la vela y también rastrea el contexto de la vela anterior.
- Cuando la vela anterior cerró por debajo del SAR pero el cierre actual está por encima, el indicador ha volteado hacia abajo y se abre una posición larga (o se revierte una posición corta existente).
- Cuando la vela anterior cerró por encima del SAR pero el cierre actual está por debajo, el indicador se ha volteado hacia arriba y se abre una posición corta (o se revierte una posición larga existente).
- El volumen de operaciones se calcula como el volumen de la estrategia base más la posición actual absoluta, lo que garantiza que las reversiones salgan por completo de la operación anterior antes de entrar en la nueva dirección.
- `StartProtection()` se ejecuta al inicio, por lo que StockSharp gestiona automáticamente las desconexiones inesperadas mientras las posiciones están abiertas.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `AccelerationFactor` | 0,02 | Paso de aceleración inicial que controla la rapidez con la que el Parabolic SAR sigue los movimientos de precios. |
| `MaxAccelerationFactor` | 0,2 | Límite superior para el paso de aceleración, que limita la agresividad con la que SAR acelera durante tendencias fuertes. |
| `CandleType` | marco de tiempo de 5 minutos | Tipo de datos de mercado utilizado para actualizaciones de indicadores; cámbielo para cambiar entre marcos de tiempo u otras representaciones de velas. |

Todos los parámetros están expuestos a través de `StrategyParam<T>` para que puedan optimizarse directamente en el diseñador StockSharp.

## Flujo de trabajo del indicador
1. Suscríbase al flujo de velas configurado a través de `SubscribeCandles`.
2. Vincula la transmisión a un indicador `ParabolicSar` para que StockSharp la actualice automáticamente.
3. Dentro de la devolución de llamada vinculante, compare el valor SAR actual con el precio de cierre y conserve el par SAR/cierre anterior.
4. Detecte cruces evaluando si el SAR se movió de arriba hacia abajo en el cierre (giro alcista) o de abajo hacia arriba (giro bajista).
5. Ejecute `BuyMarket` o `SellMarket` en consecuencia y registre mensajes descriptivos para cada operación.

## Notas prácticas
- Debido a que la estrategia sólo reacciona a los cierres de velas confirmados, evita señales prematuras que pueden desaparecer antes de que termine la barra.
- Los parámetros predeterminados reproducen el comportamiento del script MQL, pero puedes ajustarlos para adaptar la sensibilidad del Parabolic SAR.
- Adjunte la estrategia a instrumentos que tengan una tendencia limpia; la lógica de inversión SAR funciona mejor cuando las inversiones son decisivas en lugar de ruidosas.
- La visualización del gráfico se habilita automáticamente cuando un área del gráfico está disponible: las velas, el indicador Parabolic SAR y las operaciones propias se dibujan para una inspección rápida.

## Archivos
- `CS/ParabolicSarCrossoverAlertStrategy.cs` – Implementación C# de la estrategia.
- `README.md` – Esta documentación en inglés.
- `README_zh.md` – Traducción al chino de la documentación.
- `README_ru.md` – Traducción al ruso de la documentación.
