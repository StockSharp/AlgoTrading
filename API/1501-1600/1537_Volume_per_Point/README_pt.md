# Estratégia de Volume por Ponto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula o volume por ponto de preço para cada vela. Uma operação comprada é aberta quando o range da vela diminui mas o volume aumenta e o filtro RSI (se habilitado) confirma o sinal. Uma operação vendida é aberta quando o range se expande enquanto o volume se contrai.

## Parâmetros
- **RSI Length** – período para o cálculo do RSI.
- **RSI Above/Below** – limites para o filtro RSI opcional.
- **Use RSI Filter** – habilitar ou desabilitar a filtragem RSI.
- **Candle Type** – período das velas de entrada.
