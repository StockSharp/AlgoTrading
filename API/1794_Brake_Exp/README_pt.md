# Estratégia Brake Exp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no indicador **BrakeExp**. O indicador desenha um canal adaptativo de suporte e resistência construído a partir de uma curva exponencial. Uma mudança do canal da linha inferior para a superior gera um sinal de venda, e uma mudança da superior para a inferior gera um sinal de compra.

## Como funciona

- Quando o indicador reporta um **sinal de alta**, a estratégia fecha posições vendidas e abre uma nova posição comprada.
- Quando aparece um **sinal de baixa**, as posições compradas existentes são fechadas e uma posição vendida é aberta.
- Se apenas uma **tendência de alta** for detectada, a estratégia encerra posições vendidas.
- Se apenas uma **tendência de baixa** for detectada, a estratégia encerra posições compradas.

Os sinais são processados apenas em velas fechadas.

## Parâmetros

- `A` – fator de aceleração da curva do indicador BrakeExp.
- `B` – passo de preço utilizado para a largura do canal.
- `CandleType` – série de velas para cálculo do indicador.
- `Volume` – volume da ordem utilizado ao entrar no mercado.

## Observações

A estratégia utiliza a API de alto nível do StockSharp e pode ser executada no Designer, Shell ou qualquer outro produto StockSharp.
