# Estrategia de Retroceso en Nube Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del experto de MetaTrader "ichimok2005". Busca retrocesos hacia la nube de Ichimoku y opera en la dirección de la pendiente predominante del kumo. Las señales se evalúan únicamente en velas completadas.

## Descripción general

- Funciona con cualquier instrumento y marco temporal que proporcione datos de velas.
- Usa la configuración estándar de Ichimoku (9/26/52) por defecto, pero son totalmente configurables.
- Opera tanto en largo como en corto. El tamaño de posición se define por la propiedad `Volume` de la estrategia.
- El stop-loss y take-profit opcionales pueden configurarse en unidades de precio absolutas.

## Indicadores y parámetros

- **Ichimoku**: las longitudes de `Tenkan`, `Kijun` y `Senkou Span B` están expuestas como parámetros.
- **Tipo de vela**: elija cualquier tipo de vela agregada soportada por la conexión (por defecto: marco temporal de 1 hora).
- **Stop Loss Offset**: distancia opcional por debajo/encima del precio de entrada que fuerza una salida. Establezca en `0` para deshabilitar.
- **Take Profit Offset**: distancia opcional al objetivo de beneficio desde el precio de entrada. Establezca en `0` para deshabilitar.

## Reglas de entrada

### Configuración larga

1. `Senkou Span A` está por encima de `Senkou Span B`, señalando una nube alcista.
2. La vela completada actual es alcista (`Close > Open`).
3. La vela cierra dentro de la nube (`Close` está entre los dos spans).
4. Cuando todas las condiciones se alinean y la estrategia es plana o corta, envía una orden de compra de mercado dimensionada para cerrar cualquier exposición corta y abrir un nuevo largo.

### Configuración corta

1. `Senkou Span B` está por encima de `Senkou Span A`, señalando una nube bajista.
2. La vela completada actual es bajista (`Open > Close`).
3. La vela cierra dentro de la nube (`Close` está entre los dos spans).
4. Cuando las condiciones se alinean y la estrategia es plana o larga, envía una orden de venta de mercado dimensionada para cerrar cualquier exposición larga y abrir un nuevo corto.

## Reglas de salida

- Las señales opuestas revierten automáticamente la posición combinando el cierre y la nueva entrada en una sola orden de mercado.
- Cuando está habilitado, `Stop Loss Offset` sale en `EntryPrice - Offset` para largos y `EntryPrice + Offset` para cortos, usando el precio de cierre de la vela.
- Cuando está habilitado, `Take Profit Offset` sale en `EntryPrice + Offset` para largos y `EntryPrice - Offset` para cortos.
- El aplanamiento manual (cerrar la estrategia) también restablece el rastreador interno del precio de entrada.

## Notas de gestión de riesgo

- Los offsets se expresan en unidades de precio absolutas. Convierta las distancias en pips o ticks a precio antes de configurarlos.
- Dado que la estrategia usa precios de cierre de velas para las comprobaciones de riesgo, considere offsets más ajustados para marcos temporales menores.
- No se implementan salidas parciales ni trailing; la estrategia siempre cierra la posición completa.

## Detalles adicionales de implementación

- La estrategia se suscribe a velas a través de la API de alto nivel y vincula el indicador Ichimoku con `BindEx`.
- Solo las velas completadas desencadenan lógica; las actualizaciones intermedias se ignoran.
- Un área de gráfico se crea automáticamente (cuando está disponible) para mostrar el precio, la nube de Ichimoku y las operaciones ejecutadas.
- `ManageRisk` se ejecuta antes de buscar nuevas entradas para que las salidas protectoras tengan prioridad.
