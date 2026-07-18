# Estrategia BB Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia monitorea la contracción y expansión de las Bandas de Bollinger para aprovechar las rupturas de volatilidad. Define un squeeze como un período en el que la distancia entre las bandas superior e inferior se vuelve estrecha en relación con la banda media. Una vez que la volatilidad se expande y el precio cierra fuera de la banda tras un squeeze, el sistema entra en la dirección de la ruptura.

Las posiciones se abren con órdenes de mercado. Se crea una posición larga cuando el precio cierra por encima de la banda superior después de un squeeze, mientras que se abre una posición corta cuando el precio cierra por debajo de la banda inferior. Solo se procesan velas completadas, evitando señales prematuras durante su formación.

El algoritmo rastrea los cambios en el ancho de las bandas sin almacenar historiales completos de velas. Al comparar el ancho actual con el anterior, asegura que la expansión realmente ocurra antes de colocar órdenes. Esto evita entrar durante fases prolongadas de baja volatilidad donde no se desarrolla ninguna ruptura.

Los parámetros predeterminados utilizan una Banda de Bollinger de 20 períodos con un multiplicador de ancho de 2. El umbral de squeeze se establece en 0.05, lo que significa que las bandas deben estar dentro del cinco por ciento de la línea media para registrar baja volatilidad. El marco temporal de la vela y todos los valores numéricos son totalmente configurables y admiten optimización en el entorno StockSharp.
