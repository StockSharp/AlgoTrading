# Estrategia de Límites de Precio de Futuros de Acciones CME
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula los niveles de límite de precio diarios para futuros de acciones CME. Captura un precio de referencia a una hora especificada y calcula los límites al alza/baja (+/-5%), así como los niveles de límite a la baja de -7%, -13% y -20%. Los resultados se registran en el log para su monitoreo.

## Parámetros

- **ManualReference** – precio de referencia manual (0 para deshabilitar).
- **ShowLimitDownLevels** – habilitar el registro de niveles -7/-13/-20%.
- **OffsetHour** – hora (0-23) para capturar el precio de referencia.
- **CandleType** – tipo de vela a procesar (por defecto 1 minuto).
