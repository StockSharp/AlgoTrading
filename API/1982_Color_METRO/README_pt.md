# Estratégia ColorMETRO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no indicador ColorMETRO, que constrói linhas escalonadas rápidas e lentas em torno do RSI.
Uma posição comprada é aberta quando a linha rápida cruza acima da linha lenta. Uma posição vendida é aberta quando a linha rápida cruza abaixo da linha lenta. Posições opostas são fechadas nos mesmos sinais.

## Parâmetros
- **Candle Type** – tipo de vela utilizado para os cálculos.
- **RSI Period** – período para o cálculo do RSI.
- **Fast Step** – tamanho do passo para a linha rápida.
- **Slow Step** – tamanho do passo para a linha lenta.
- **Stop Loss** – distância em pontos para proteção de stop-loss.
- **Take Profit** – distância em pontos para proteção de take-profit.
- **Allow Buy** – permissão para abrir posições compradas.
- **Allow Sell** – permissão para abrir posições vendidas.
- **Close Long** – permissão para fechar posições compradas.
- **Close Short** – permissão para fechar posições vendidas.

A estratégia usa `StartProtection` para gerenciar os níveis de stop-loss e take-profit.
