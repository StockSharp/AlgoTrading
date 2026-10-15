# Estratégia de Cruzamento de Médias Fibo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia converte o assessor especialista do MetaTrader **EA_Fibo_Avg_001a** para o framework StockSharp.
Ela usa duas médias móveis suavizadas. O comprimento da média lenta é a soma do período base e um deslocamento baseado em Fibonacci.
Uma posição comprada é aberta quando a média rápida cruza acima da média lenta, enquanto uma posição vendida é aberta no cruzamento oposto.
As posições são gerenciadas com stop-loss, take-profit e um trailing stop. O gerenciamento de dinheiro opcional pode calcular o volume da ordem a partir do tamanho do portfólio.

## Parâmetros
- `CandleType` – tipo de dados de candles.
- `FiboNumPeriod` – comprimento adicional adicionado à média móvel lenta.
- `MaPeriod` – período base das médias móveis.
- `TrailingStop` – distância do trailing em passos de preço.
- `TakeProfit` – distância do take-profit em passos de preço.
- `StopLoss` – distância do stop-loss em passos de preço.
- `UseMoneyManagement` – ativar gerenciamento de dinheiro simples.
- `PercentMm` – percentual do portfólio usado quando o gerenciamento de dinheiro está ativado.
- `LotSize` – volume de ordem padrão quando o gerenciamento de dinheiro está desativado.

## Lógica
1. Assinar candles e calcular duas médias móveis suavizadas.
2. Quando a média rápida cruza acima da lenta, comprar. Quando cruza abaixo, vender.
3. Após entrar em uma posição, definir níveis de stop-loss, take-profit e trailing.
4. Atualizar o trailing stop à medida que o preço se move a favor e fechar posições quando os níveis de proteção são atingidos ou ocorre o cruzamento oposto.
