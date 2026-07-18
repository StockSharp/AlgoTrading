# Estratégia de Tendência de Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia abre posições com base na direção de velas consecutivas.
Uma posição comprada é aberta após um número especificado de velas de alta aparecerem seguidas, enquanto uma posição vendida é aberta após o mesmo número de velas de baixa.
As posições existentes podem ser fechadas quando ocorrer o sinal oposto.

## Parâmetros

- **Candle Type**: Período das velas usadas para análise.
- **Trend Candles**: Número de velas consecutivas em uma direção necessárias para acionar uma ação.
- **Take Profit %**: Take-profit opcional expresso como percentual do preço de entrada.
- **Stop Loss %**: Stop-loss opcional expresso como percentual do preço de entrada.
- **Enable Long Entry**: Permitir abrir posições compradas.
- **Enable Short Entry**: Permitir abrir posições vendidas.
- **Enable Long Exit**: Permitir fechar posições compradas em sinal oposto.
- **Enable Short Exit**: Permitir fechar posições vendidas em sinal oposto.

## Lógica

1. Assinar dados de velas do período selecionado.
2. Rastrear o número de velas de alta e de baixa consecutivas.
3. Quando o contador de alta atinge o número necessário:
   - Fechar posições vendidas se permitido.
   - Abrir uma posição comprada se permitido.
4. Quando o contador de baixa atinge o número necessário:
   - Fechar posições compradas se permitido.
   - Abrir uma posição vendida se permitido.
5. Ordens de proteção opcionais são definidas usando `StartProtection`.

## Notas

- Os sinais são processados apenas em velas concluídas.
- A estratégia usa `BuyMarket` e `SellMarket` para entradas e saídas.
- Todos os comentários no código são escritos em inglês conforme exigido.
