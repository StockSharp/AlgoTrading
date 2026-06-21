# Estratégia de Barras Equivolume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em picos de volume em relação à soma dos volumes durante um período de retrospecto.

## Lógica
- Calcular a proporção do volume atual em relação à soma dos volumes anteriores.
- Ir comprado quando a proporção excede o limiar e a vela é de alta.
- Ir vendido quando a proporção excede o limiar e a vela é de baixa.
- Fechar a posição quando a proporção cai abaixo do limiar ou a vela reverte.

## Parâmetros
- `Lookback` – número de barras para a soma de volumes.
- `Volume Threshold` – limiar de proporção para volume alto.
- `Candle Type` – tipo de velas a utilizar.
