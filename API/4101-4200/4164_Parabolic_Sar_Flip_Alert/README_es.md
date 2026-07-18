# Parabolic SAR Estrategia de alerta de volteo (4164)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia reproduce el asesor experto MetaTrader **pSAR_alert2** dentro del marco StockSharp. Monitorea el indicador Parabolic SAR en el instrumento y el período de tiempo seleccionados. Cada vez que el valor SAR cambia de encima del precio de cierre a debajo de él (o viceversa), la estrategia genera una alerta informativa. Opcionalmente, puede enviar órdenes de mercado hacia el giro para transformar la alerta en una entrada automatizada.

## Lógica de trading

1. Suscríbase a la serie de velas configuradas y calcule el indicador Parabolic SAR con la configuración de aceleración proporcionada.
2. Espere a que termine cada vela para emular el tiempo original EA.
3. Compare el valor del indicador con el cierre de la vela:
   - SAR anterior por encima del cierre y SAR actual por debajo del cierre → **giro alcista**.
   - SAR anterior por debajo del cierre y SAR actual por encima del cierre → **giro bajista**.
4. Registre una alerta detallada para cada giro. Cuando el comercio automático esté habilitado, reduzca cualquier exposición opuesta y abra una nueva posición en la dirección de la señal utilizando órdenes de mercado.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Candle Type` | Marco de tiempo utilizado para construir velas y evaluar el indicador Parabolic SAR. |
| `SAR Step` | Factor de aceleración inicial pasado al Parabolic SAR. |
| `SAR Max` | Factor de aceleración máximo del Parabolic SAR. |
| `Enable Auto Trading` | Cuando `true`, se envían órdenes de mercado en cada alerta; cuando `false`, solo se generan registros. |
| `Trade Volume` | El tamaño de la orden se aplica cuando el comercio automático está habilitado. |

## Notas de conversión

- El script MetaTrader original dependía de `Sleep` para acelerar la ejecución. StockSharp se basa en eventos, por lo que la estrategia reacciona a nuevas velas inmediatamente sin demoras manuales.
- Las alertas se generan a través de `AddInfoLog`, manteniendo el comportamiento original de las notificaciones emergentes sin requerir componentes de interfaz de usuario adicionales.
- Se proporciona comercio automático opcional para integrar la lógica de alerta en flujos de trabajo automatizados. Deshabilite el parámetro `Enable Auto Trading` para que coincida exactamente con el comportamiento de MetaTrader.
- La implementación de Python se omite intencionalmente según lo solicitado.
