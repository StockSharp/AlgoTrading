# Estratégia MTF RSI SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina leituras do **Índice de Força Relativa (RSI)** em quatro períodos, **Parabolic SAR** e **Bandas de Bollinger** para capturar a continuação de tendências após breves correções. Os sinais são gerados em candles de 5 minutos enquanto períodos mais altos atuam como filtros de confirmação.

## Conceito

1. **Filtro RSI** – Os valores de RSI de 5, 15, 30 e 60 minutos devem estar todos acima de 50 para entradas compradas ou abaixo de 50 para entradas vendidas. Esta confirmação multi-período visa alinhar as operações com a tendência mais ampla.
2. **Filtro Parabolic SAR** – Os valores do Parabolic SAR nos gráficos de 5, 15 e 30 minutos devem estar abaixo do candle atual para comprados ou acima para vendidos. Isso garante que o preço esteja tendendo na direção desejada.
3. **Gatilho de Bandas de Bollinger** – No gráfico de 5 minutos o fechamento do candle deve romper a banda superior para comprados ou a banda inferior para vendidos. As Bandas de Bollinger fornecem um gatilho de sobrecompra/sobrevenda.
4. **Entrada e saída** – Uma posição comprada é aberta quando todos os filtros ativos apontam para cima. Uma posição vendida é aberta quando todos os filtros ativos apontam para baixo. O sinal oposto fecha uma posição aberta.

Qualquer um dos três filtros pode ser desabilitado individualmente via parâmetros, permitindo que a estratégia opere apenas com RSI, apenas com Bandas de Bollinger, apenas com SAR, ou qualquer combinação dos acima.

## Parâmetros

- `UseRsi` – ativar filtro RSI (padrão: true).
- `UseBollinger` – ativar gatilho de Bandas de Bollinger (padrão: true).
- `UseSar` – ativar filtro Parabolic SAR (padrão: true).
- `RsiPeriod` – período de cálculo do RSI (padrão: 14).
- `BollingerPeriod` – número de barras para Bandas de Bollinger (padrão: 20).
- `BollingerWidth` – largura (multiplicador de desvio padrão) para Bandas de Bollinger (padrão: 2).
- `SarStep` – fator de aceleração para Parabolic SAR (padrão: 0.02).
- `SarMax` – fator de aceleração máximo para Parabolic SAR (padrão: 0.2).
- `CandleType` – período de candle base, 5 minutos por padrão.

## Regras de Trading

- **Comprado**: todos os filtros habilitados fornecem sinais de alta.
- **Vendido**: todos os filtros habilitados fornecem sinais de baixa.
- **Saída**: o sinal oposto fecha a posição.

## Notas

- A estratégia opera em um ativo com quatro assinaturas de candle: períodos de 5, 15, 30 e 60 minutos.
- Projetada como exemplo educacional de confirmação multi-período usando a API de alto nível do StockSharp.
- Não há stop-loss fixo nem metas de lucro; o gerenciamento de risco deve ser adicionado externamente se necessário.
