# Estratégia Martingale MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o expert MQL original "MartGreg_1" no framework StockSharp. Utiliza dois indicadores de Convergência/Divergência de Médias Móveis (MACD) para buscar reversões e aplica uma regra de martingala para o dimensionamento de posições.

## Como funciona

- O primeiro MACD busca extremos locais nas últimas três velas completadas.
- O segundo MACD compara os dois últimos valores para determinar a direção do momentum.
- Uma posição comprada é aberta quando o primeiro MACD forma um vale e o segundo MACD decresce.
- Uma posição vendida é aberta quando o primeiro MACD forma um pico e o segundo MACD aumenta.
- Após cada operação perdedora, o tamanho da próxima ordem é dobrado até o limite configurado.
- O stop-loss e o take-profit são definidos em pontos de preço absolutos.

## Parâmetros

- `Shape` – divisor para calcular o volume inicial a partir do saldo da conta.
- `Doubling Count` – número máximo de duplicações consecutivas após perdas.
- `Stop Loss` – stop de proteção em pontos.
- `Take Profit` – meta de lucro em pontos.
- `MACD1 Fast/Slow` – períodos para o primeiro MACD.
- `MACD2 Fast/Slow` – períodos para o segundo MACD.
- `Candle Type` – período para análise.

