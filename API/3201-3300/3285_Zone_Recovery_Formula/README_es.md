# Estrategia Zone Recovery Formula
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **estrategia Zone Recovery Formula** es una adaptación del asesor experto de MetaTrader 4 "Zone Recovery Formula". El algoritmo sigue una dirección de tendencia impulsada por medias móviles y después aplica una técnica de recuperación por zonas para mitigar movimientos adversos del precio. La idea central es alternar ciclos largos y cortos con volumen que aumenta gradualmente hasta que la acción del precio sale de la zona de recuperación definida, bloqueando ganancia incluso después de varias inversiones.

## Funcionamiento

1. **Detección de señales** - La estrategia se suscribe a velas de marco temporal (15 minutos por defecto) y rastrea una media móvil simple rápida y una lenta. Un cruce alcista inicia un ciclo de recuperación largo, mientras que un cruce bajista inicia un ciclo corto.
2. **Orden inicial** - Cuando empieza un nuevo ciclo, la estrategia abre una posición de mercado con el multiplicador de volumen base. Las distancias de take-profit y recuperación se calculan desde la configuración de pips y el tamaño de tick del instrumento.
3. **Recuperación por zonas** - Si el precio se mueve contra la posición abierta por la distancia de recuperación configurada, la estrategia invierte la dirección y aumenta el tamaño de la orden usando la secuencia de fórmula original (hasta el número máximo de operaciones). Esto crea una exposición neta alternante que busca cubrir pérdidas previas cuando el precio vuelve al objetivo de ganancia.
4. **Gestión de ganancias** - El algoritmo monitoriza la ganancia no realizada:
   - Las condiciones de take-profit monetario y porcentual pueden cerrar todas las posiciones de inmediato.
   - La gestión trailing opcional captura ganancias después de un beneficio predefinido y las protege con una distancia de trailing stop.
5. **Reinicio del ciclo** - Cuando se alcanzan los objetivos de ganancia o la protección trailing cierra la posición, el ciclo de recuperación se reinicia y la estrategia espera la siguiente señal de media móvil.

## Parámetros clave

- **Usar TP dinero / TP dinero** - Habilita y configura el take-profit monetario.
- **Usar TP % / Porcentaje TP** - Habilita y configura take-profit porcentual basado en el balance de la cartera.
- **Habilitar trailing / TP trailing / SL trailing** - Activa la captura de ganancia trailing y define el nivel de activación junto con la distancia de protección.
- **TP pips / Zona pips** - Distancias (en pips) que definen el objetivo de take-profit y la zona disparadora de recuperación.
- **Volumen base / Máx. operaciones** - Tamaño inicial de orden y número de pasos de recuperación permitidos en un ciclo.
- **MA rápida / MA lenta** - Medias móviles que generan señales de entrada.
- **Desplazamiento de ganancia** - Ajuste opcional usado en la fórmula original de volumen de recuperación.

## Notas

- La estrategia usa la API de alto nivel de StockSharp con suscripciones a velas y vinculación de indicadores.
- Las posiciones de cobertura se emulan invirtiendo la dirección de la posición neta y escalando el volumen, lo que mantiene la lógica compatible con la contabilidad de posición neta de StockSharp.
- Las comprobaciones de trailing y take-profit dependen de la ganancia no realizada calculada desde el precio actual de la posición. Ajuste los valores monetarios para que coincidan con el valor de tick del instrumento.
- Pruebe siempre en un entorno simulado antes de desplegar en una cuenta real.

## Archivos

- `CS/ZoneRecoveryFormulaStrategy.cs` - implementación C# de la estrategia.
- `README.md` - este archivo de documentación en inglés.
- `README_ru.md` - documentación en ruso.
- `README_zh.md` - documentación en chino.
