# Estrategia Ang Zad C Time MM Recovery
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
La Estrategia Ang Zad C Time MM Recovery es un port en C# del asesor experto de MetaTrader 5 `Exp_Ang_Zad_C_Tm_MMRec`. La estrategia combina el indicador de canal personalizado Ang_Zad_C con un filtro de sesión de trading configurable y un modelo de tamaño de posición adaptativo que reduce el riesgo después de un número configurable de trades perdedores.

## Lógica del indicador
El indicador Ang_Zad_C construye dos envolventes adaptativas alrededor del precio. Cada envolvente se actualiza comparando el precio aplicado elegido de la vela actual y la anterior, moviéndose hacia el nuevo precio con el factor de suavizado **Ki**. Las líneas superior e inferior se evalúan en barras históricas definidas por **Signal Bar** para evitar actuar en velas no terminadas.

## Reglas de trading
* **Entrada larga** – Cuando la línea superior estaba por encima de la línea inferior en la barra de referencia anterior y cruza por debajo o toca la línea inferior en la barra de referencia más reciente. Cuando esto ocurre, cualquier posición corta abierta se cierra antes de abrir una nueva posición larga (si está habilitado).
* **Entrada corta** – Cuando la línea superior estaba por debajo de la línea inferior en la barra de referencia anterior y cruza por encima o toca la línea inferior en la barra de referencia más reciente. Cualquier posición larga abierta se cierra antes de abrir una nueva posición corta (si está habilitado).
* **Salida larga** – Cuando la línea superior está por debajo de la línea inferior en la barra de referencia anterior. La salida puede deshabilitarse mediante **Enable Long Exit**.
* **Salida corta** – Cuando la línea superior está por encima de la línea inferior en la barra de referencia anterior. La salida puede deshabilitarse mediante **Enable Short Exit**.

## Gestión monetaria y protecciones
* El trading solo se permite dentro de la ventana de tiempo configurada cuando **Use Time Filter** está habilitado. Las posiciones abiertas anteriormente se cierran una vez que finaliza la sesión.
* El volumen del trade se selecciona entre **Normal Volume** y **Small Volume** dependiendo de cuántos trades perdedores ocurrieron para cada lado. Después de **Buy Loss Trigger** trades largos perdedores (o **Sell Loss Trigger** trades cortos perdedores) se usa el volumen reducido hasta que un trade rentable resetea el contador.
* Los niveles opcionales de stop-loss y take-profit se registran usando distancias en pasos de precio definidas por **Stop Loss Steps** y **Take Profit Steps**.

## Parámetros
| Nombre | Descripción |
| ------ | ----------- |
| Candle Type | Marco temporal de las velas usadas por el indicador y las señales. |
| Ki | Coeficiente de suavizado de las envolventes Ang_Zad_C. |
| Applied Price | Qué precio de la vela se alimenta al indicador. |
| Signal Bar | Cuántas barras atrás se usan para la evaluación de señales (1 = barra cerrada anterior). |
| Use Time Filter / Trade Start / Trade End | Habilitar trading basado en sesión y establecer la hora de inicio y fin de la sesión. |
| Enable Long/Short Entry | Permitir la apertura de nuevos trades largos o cortos. |
| Enable Long/Short Exit | Permitir que la estrategia cierre posiciones en la reversión del indicador. |
| Buy/Sell Loss Trigger | Número de trades perdedores antes de aplicar el volumen reducido. |
| Small Volume / Normal Volume | Tamaños de orden usados para riesgo reducido y normal. |
| Stop Loss Steps / Take Profit Steps | Distancia para órdenes protectoras expresada en pasos de precio. |

## Notas de conversión
* La lógica sigue el código MQL5 original, incluyendo las verificaciones de cruce direccional y el comportamiento de la ventana de tiempo.
* La gestión monetaria adaptativa se implementa rastreando el beneficio y pérdida realizados por dirección y cambiando al volumen reducido después del número configurado de pérdidas.
* Los cálculos del indicador evitan cualquier acceso directo al buffer y se procesan en velas terminadas usando la API de alto nivel de StockSharp.
