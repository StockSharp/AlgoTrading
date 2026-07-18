# Estratégia Painel de Trading Com Piloto Automático
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o exemplo MQL5 **Trade panel with autopilot** para o framework StockSharp.
Ela calcula a pressão altista e baixista em múltiplos períodos. Uma posição é aberta quando o percentual correspondente excede o limiar *Open %* e fechada quando cai abaixo do nível *Close %*. Opcionalmente, um stop loss baseado em fractais pode ser aplicado usando candles de 10 minutos.

## Parâmetros

- **Autopilot** – habilitar ou desabilitar a negociação automatizada.
- **Open %** – limiar de votos necessário para abrir uma posição.
- **Close %** – limiar para fechar a posição existente.
- **Use Fixed Volume** – se verdadeiro, usar o valor de *Fixed Volume*.
- **Fixed Volume** – volume de ordem absoluto.
- **Volume %** – percentual do portfólio utilizado quando o volume é dinâmico.
- **Use Stop Loss** – habilitar stop loss baseado em fractais recentes.

## Lógica

Para cada período de 1 minuto a 1 mês, a estratégia compara o último candle com o anterior. Cada comparação de abertura, máxima, mínima e médias derivadas adiciona um voto de compra ou venda. Os percentuais de votos de compra e venda determinam a colocação de ordens. Quando habilitado, o último fractal dos candles de 10 minutos atua como trailing stop.

Este exemplo tem fins educacionais e não representa aconselhamento de investimento.
