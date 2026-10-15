# Vlado Williams Estrategia de umbral %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de umbral %R de Vlado Williams** es una conversión directa del MetaTrader 4 asesor experto `Vlado_www_forex-instruments_info.mq4`. El robot original intercambia un único oscilador Williams %R y cambia su exposición al mercado cada vez que el indicador cruza un nivel definido por el usuario. Este puerto StockSharp reproduce el mismo comportamiento de cambio de régimen al tiempo que expone cada valor ajustable como un parámetro de estrategia para la optimización y el control de la interfaz de usuario.

### Conceptos clave
- Intercambia la dirección del oscilador Williams %R en relación con un umbral (predeterminado `-50`).
- Mantiene como máximo una posición de mercado a la vez y se revierte solo después de cerrar la operación anterior.
- Tamaño de posición opcional basado en el riesgo que imita la MetaTrader fórmula de administración de dinero `AccountFreeMargin * MaximumRisk / price`.
- Funciona con cualquier período de vela a través del parámetro `CandleType` (barras predeterminadas de 15 minutos).

## Lógica de trading
1. Suscríbase al flujo de velas configurado y calcule un Williams %R de longitud `WprLength` (predeterminado 100).
2. Cuando Williams %R sube **por encima** de `WprLevel`, la estrategia marca un sesgo alcista:
   - Si no hay ninguna posición abierta y la operación anterior no fue larga, envíe una orden de **compra** de mercado.
   - Si existe una posición corta, se cierra inmediatamente; Se consideran nuevas posiciones largas en la siguiente vela después de que la posición sea plana.
3. Cuando el Williams %R cae **por debajo** de `WprLevel`, el sesgo cambia a bajista:
   - Si no hay ninguna posición abierta y la operación anterior no fue corta, envíe una orden de **venta** de mercado.
   - Si existe una posición larga, se aplana de inmediato.
4. El tamaño de la posición está determinado por `CalculateOrderVolume`:
   - Cuando `UseRiskMoneyManagement` es **verdadero**, la estrategia estima el volumen negociable a partir del valor actual de la cartera: `Portfolio.CurrentValue × MaximumRiskPercent ÷ 100 ÷ ClosePrice`.
   - De lo contrario, se utiliza la base `Strategy.Volume`.
   - Los lotes resultantes se alinean con el instrumento `VolumeStep` y se sujetan con `MinVolume`/`MaxVolume` si estos límites están disponibles.

La estrategia evita intencionalmente abrir una posición de reversión en la misma vela que desencadenó la salida, coincidiendo con el flujo EA original (`CheckForClose` se ejecuta antes de `CheckForOpen`).

## Notas de conversión
- Los valores predeterminados de administración del dinero siguen el script MT4: `MaximumRiskPercent` comienza en `10`, coincidiendo con la constante `MaximumRisk = 10` original que apuntaba aproximadamente a un mini lote por operación.
- El parámetro MetaTrader `shift` (cambio de indicador) siempre es cero en el archivo fuente; por lo tanto fue omitido.
- Los argumentos de color MT4 (por ejemplo, `Red`, `Blue`) no tienen equivalente StockSharp y se ignoran.
- No se requieren entradas de deslizamiento porque StockSharp órdenes de mercado ya utilizan el mejor precio actual.

## Parámetros
| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | plazo de 15 minutos | Plazo tanto para el cálculo de señales como para la activación de órdenes. |
| `WprLength` | `int` | 100 | Período retrospectivo del oscilador Williams %R. |
| `WprLevel` | `decimal` | `-50` | Umbral que separa los regímenes alcistas y bajistas. |
| `UseRiskMoneyManagement` | `bool` | `false` | Alterna el tamaño de la posición basado en el riesgo. |
| `MaximumRiskPercent` | `decimal` | `10` | Porcentaje de capital de la cartera desplegado por operación cuando la gestión de riesgos está activada. |

> **Consejo:** Combine la estrategia con `StartProtection()` o controles de riesgo externos si necesita un manejo automático de stop-loss. El EA original también se basaba en la supervisión manual y no definía paradas bruscas.

## Pautas de uso
1. Adjunte la estrategia a un valor que exponga límites precisos de `PriceStep`, `StepPrice`, `VolumeStep` y de volumen para que el asistente de dimensionamiento de posiciones pueda normalizar los pedidos correctamente.
2. Establezca `Volume` en el tamaño de lote alternativo que desee. Se utilizará siempre que el capital de la cartera no esté disponible o `UseRiskMoneyManagement` esté deshabilitado.
3. Optimice `WprLevel` y `WprLength` para adaptar el sistema a diferentes mercados. Los niveles estrechos (por ejemplo, `-20` / `-80`) hacen que la estrategia sea más selectiva, mientras que los umbrales amplios (`-50`) garantizan que casi siempre se invierta.
4. La estrategia sigue la tendencia: se revertirá con frecuencia en condiciones variables. Considere combinarlo con filtros como comprobaciones de tendencias con períodos de tiempo más altos o umbrales de volatilidad cuando sea necesario.

## Diferencias frente a la versión MetaTrader
- Utiliza suscripciones de velas y enlaces de indicadores dla API de alto nivel de StockSharp; no hay bucle de pedidos manual ni escaneo del historial.
- El tamaño del riesgo depende de `Portfolio.CurrentValue`. Cuando falta la valoración de la cuenta, la lógica vuelve al `Volume` estático, que coincide con el comportamiento de MT4 donde `mm=0` forzó un tamaño de lote fijo.
- Todos los comentarios y descripciones de parámetros están en inglés para mantener la coherencia con las pautas del repositorio.

## Lista de verificación de validación
- ✅ Archivo de estrategia compilado con las convenciones de plantilla de estrategia StockSharp (pestañas, espacio de nombres con ámbito de archivo, documento heredado XML).
- ✅ Parámetros creados a través de `Param()` y marcados para optimización cuando corresponda.
- ✅ Williams Valores %R consumidos a través de `Bind`, sin ningún acceso directo a `GetValue()`.
