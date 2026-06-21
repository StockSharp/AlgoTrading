# Estratégia Color Trend CF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão do especialista MQL **Exp_ColorTrend_CF**. Utiliza duas médias móveis exponenciais para detectar mudanças de tendência. A EMA rápida reage rapidamente aos movimentos de preço, enquanto a EMA lenta atua como filtro de tendência. Uma posição comprada é aberta quando a EMA rápida cruza acima da EMA lenta. Uma posição vendida é aberta quando a EMA rápida cruza abaixo da EMA lenta.

## Parâmetros

- `Period` – período base para a EMA rápida; a EMA lenta usa o dobro deste valor.
- `StopLoss` – distância de stop-loss em unidades de preço.
- `TakeProfit` – distância de take-profit em unidades de preço.
- `AllowBuyOpen` – permissão para abrir posições compradas.
- `AllowSellOpen` – permissão para abrir posições vendidas.
- `AllowBuyClose` – permissão para fechar posições compradas em sinal de venda.
- `AllowSellClose` – permissão para fechar posições vendidas em sinal de compra.
- `CandleType` – período para cálculo de indicadores.

## Lógica de trading

1. Subscrever velas do período selecionado.
2. Calcular as EMA rápida e lenta.
3. Quando a EMA rápida cruza acima da EMA lenta:
   - Fechar posições vendidas se permitido.
   - Abrir posição comprada se permitido.
4. Quando a EMA rápida cruza abaixo da EMA lenta:
   - Fechar posições compradas se permitido.
   - Abrir posição vendida se permitido.
5. Para posições abertas, aplicar níveis de stop-loss e take-profit.

Esta implementação utiliza a API de alto nível do StockSharp com vinculação de indicadores.
