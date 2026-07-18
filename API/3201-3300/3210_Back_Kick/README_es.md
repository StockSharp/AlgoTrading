# Estrategia de Back Kick
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Back Kick** es un sistema de ruptura con cobertura convertido del asesor experto de MetaTrader 5 `Back kick.mq5`. Mantiene continuamente una exposición de dos lados abriendo tanto una posición larga como una corta al cierre de cada barra. Cada pierna está protegida con distancias simétricas de stop-loss y take-profit expresadas en pips. El port de StockSharp mantiene las posiciones emparejadas independientes rastreando su estado manualmente en lugar de depender de la posición neta agregada.

## Lógica de trading

1. Suscribirse a las velas del marco temporal configurado. Cuando una vela cierra y no hay piernas con cobertura activas, solicitar un nuevo par de entradas.
2. Enviar inmediatamente una orden de compra y una de venta de mercado usando el mismo volumen. Cada pierna mantiene sus propios desplazamientos de stop-loss y take-profit convertidos desde distancias en pips.
3. Monitorear los mejores precios de oferta/demanda desde datos de Nivel 1. Si una pierna alcanza su precio de protección, se cierra con una orden de mercado, mientras que la pierna opuesta permanece activa hasta que se active su propia salida.
4. Después de que ambas piernas estén planas, la estrategia espera la siguiente vela completada antes de recrear la cobertura.

Este comportamiento refleja al experto original, que reingresa constantemente en ambas direcciones para capturar "impulsos" abruptos de precio.

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
| ---- | ----------- | ------- | ----- |
| `OrderVolume` | Volumen usado para cada pierna de cobertura. | `0.1` | Normalizado al `VolumeStep` del instrumento, debe respetar `MinVolume`/`MaxVolume`. |
| `StopLossPips` | Distancia del stop-loss en pips. | `50` | Establecer en `0` para deshabilitar el stop de protección para ambas piernas. |
| `TakeProfitPips` | Distancia del take-profit en pips. | `140` | Establecer en `0` para deshabilitar el take-profit de protección. |
| `CandleType` | Marco temporal que activa nuevos pares con cobertura. | `15m` | Acepta cualquier `TimeFrame` compatible con el valor seleccionado. |
| `LogDiagnostics` | Habilita el registro detallado sobre entradas y salidas. | `false` | Útil para depurar secuencias de llenado. |

## Notas de implementación

- **Conversión de pips** – El EA original ajusta el tamaño de pip para símbolos de 3/5 dígitos. El port de StockSharp replica esto multiplicando el paso de precio por `10` cuando es necesario.
- **Modelo de cobertura manual** – StockSharp usa posiciones netas, por lo que la estrategia mantiene el estado por pierna (`PositionState`) y despacha órdenes de mercado explícitas para las salidas. Esto permite que el comportamiento se asemeje al modo de cuenta con cobertura de MT5.
- **Gestión del riesgo** – Los niveles de stop-loss y take-profit son opcionales. Si cualquiera está deshabilitado, esa pierna solo se cerrará cuando se active el nivel de protección opuesto o mediante gestión externa.
- **Servicio de protección** – `StartProtection()` todavía se invoca para que el marco de trabajo monitoree desconexiones inesperadas, aunque se implementa lógica de salida personalizada.

## Uso

1. Adjunte la estrategia a un valor con datos de Nivel 1 confiables (oferta/demanda) y las velas del marco temporal deseado.
2. Configure las distancias de pip y el volumen de operaciones según su perfil de riesgo.
3. Inicie la estrategia; esperará al próximo cierre de vela antes de enviar el par con cobertura.
4. Monitoree los registros o el gráfico para observar cómo cada pierna sale independientemente.

## Diferencias con la versión MT5

- La gestión de dinero basada en porcentaje de riesgo no se transfiere; use `OrderVolume` para controlar el tamaño de la operación.
- Dado que StockSharp agrega posiciones de cartera, la estrategia emula la cobertura a través de registros internos. Esto asegura un comportamiento cercano al experto original mientras permanece compatible con corredores que netean posiciones.
- Las verificaciones de nivel de congelamiento/stop específicas del corredor se omiten. En cambio, la rutina de normalización de volumen lanza excepciones descriptivas si se violan los límites del intercambio.

## Archivos

- `CS/BackKickStrategy.cs` – Implementación de la estrategia usando la API de alto nivel de StockSharp.
- `README.md` – Documentación en inglés (este archivo).
- `README_ru.md` – Documentación en ruso.
- `README_zh.md` – Documentación en chino.
