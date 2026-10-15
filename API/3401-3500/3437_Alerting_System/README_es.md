# Estrategia del sistema de alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia del **Sistema de Alertas** es una fiel StockSharp conversión del MetaTrader 4 asesores expertos `AlertingSystem.mq4`. El guión original dibuja dos líneas horizontales y reproduce un sonido cada vez que el mercado las toca. La versión StockSharp logra el mismo objetivo al suscribirse a cotizaciones de Nivel 1 (mejor oferta/demanda) e imprimir mensajes de diario cuando se cruza cualquiera de los niveles de alerta configurables.

## Idea central

1. Registre un flujo de datos de nivel 1 para que la estrategia reciba actualizaciones de ofertas y solicitudes tick-by-tick, reflejando el controlador MQL `OnTick`.
2. Lea los niveles `UpperPrice` y `LowerPrice` definidos por el usuario. Un valor de `0` deshabilita la alerta correspondiente, al igual que eliminar la línea horizontal en MetaTrader.
3. Compare cada oferta entrante con el nivel superior y cada solicitud con el nivel inferior.
4. Emita una única notificación de registro cuando el precio cruce un nivel activo y espere hasta que el mercado regrese a la zona segura antes de activar la alerta nuevamente. Esto evita ruidosas alertas duplicadas y al mismo tiempo preserva la intención del activador de sonido original.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `UpperPrice` | `0` | Nivel de alerta horizontal superior. Establezca en `0` para desactivar la verificación. |
| `LowerPrice` | `0` | Nivel de alerta horizontal inferior. Establezca en `0` para desactivar la verificación. |

Ambos parámetros se exponen a través de la interfaz de usuario del Diseñador. Se pueden cambiar antes del lanzamiento o mientras se ejecuta la estrategia; la próxima actualización de cotización utilizará los nuevos niveles.

## Comportamiento en tiempo de ejecución

- **Suscripción de datos**: `GetWorkingSecurities` solicita datos de Nivel 1, lo que garantiza que la estrategia reciba actualizaciones de oferta/demanda incluso sin velas ni operaciones.
- **Inicialización**: Cuando `OnStarted` se activa, la estrategia registra los niveles configurados actualmente para que el operador pueda verificar la configuración.
- **Detección de alertas**: los métodos auxiliares (`CheckUpperAlert` y `CheckLowerAlert`) almacenan indicadores internos para garantizar que cada infracción produzca exactamente una notificación hasta que el mercado retroceda más allá del umbral.
- **Sin operaciones**: La conversión no envía órdenes. Es puramente una utilidad de alerta, que coincide con el comportamiento del script MetaTrader que solo reproducía un sonido.
- **Restablecer manejo**: `OnReseted` borra los indicadores internos para que la siguiente ejecución comience con nuevos estados de alerta.

## Pasos de uso típicos

1. Seleccione el instrumento deseado en StockSharp Designer y adjunte `AlertingSystemStrategy`.
2. Especifique los niveles de alerta superior y/o inferior. Deje un valor en `0` para ignorar ese lado.
3. Inicia la estrategia. El registro mostrará entradas que confirman qué alertas están activas.
4. Supervise la ventana del diario. Cuando la oferta sube por encima del nivel superior o la demanda cae por debajo del nivel inferior, la estrategia registra un mensaje descriptivo.

## Notas de conversión

- El asesor original MetaTrader creó dos líneas horizontales que se pueden arrastrar. StockSharp utiliza parámetros numéricos en su lugar, lo que mantiene el flujo de trabajo determinista y más adecuado para la ejecución algorítmica.
- MetaTrader activó la función `PlaySound` en cada tick calificado. Para evitar sobrecargar el registro, la conversión rebota alertas hasta que el precio vuelve a entrar en el rango aceptable.
- La lógica se mantiene intencionalmente libre de indicadores: solo se requieren cotizaciones sin procesar, por lo que la estrategia funciona en cualquier marco temporal o instrumento que proporcione datos de Nivel 1.

## Clasificación

- **Categoría**: Utilidades / Alertas
- **Dirección comercial**: Ninguna
- **Estilo de ejecución**: Monitoreo basado en eventos
- **Requisitos de datos**: oferta/demanda de nivel 1
- **Complejidad**: Básico
- **Plazo recomendado**: Cualquiera (según cotización)
- **Gestión de Riesgos**: No aplicable (no hay posiciones abiertas)

Esta documentación resume la implementación de StockSharp y destaca los pasos prácticos necesarios para reproducir el flujo de trabajo de alertas MetaTrader dentro de la plataforma.
