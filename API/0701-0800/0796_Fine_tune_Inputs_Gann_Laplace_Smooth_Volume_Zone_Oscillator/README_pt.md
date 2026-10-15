# Estratégia de Ajuste Fino de Entradas Gann + Oscilador de Zona de Volume Suavizado por Laplace
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza um oscilador de volume suavizado por médias móveis exponenciais.
Uma posição comprada é aberta quando o oscilador suavizado sobe acima do limiar.
Uma posição vendida é aberta quando ele cai abaixo do limiar negativo.
Se os sinais desaparecerem e **Close All** estiver habilitado, qualquer posição aberta é fechada.

## Parâmetros
- **Fast Volume EMA** – período para a média rápida de volume.
- **Slow Volume EMA** – período para a média lenta de volume.
- **Smooth Length** – período de suavização do oscilador.
- **Threshold** – nível de sinal para entradas.
- **Close All** – fechar posição quando não há sinal.
- **Candle Type** – tipo de vela utilizado para os cálculos.
