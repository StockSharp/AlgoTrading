# Estratégia de ATR Não Sazonalizado e Previsão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia analisa o intervalo médio de negociação das velas recentes e prevê o próximo intervalo usando regressão de tendência linear. Ela não realiza nenhuma operação, mas exibe estatísticas que podem ser usadas para decisões manuais.

## Parâmetros

- **SampleSize** – número de velas recentes usadas para os cálculos.
- **DesiredRange** – intervalo alvo usado para a estimativa do intervalo de confiança.
- **CandleType** – série de velas a analisar.

## Indicadores

- SimpleMovingAverage – usado para calcular o intervalo médio.
- StandardDeviation – mede a volatilidade do intervalo.
- Regressão linear (personalizada) – prevê o próximo intervalo e o MAPE.

## Comportamento

Para cada vela concluída, a estratégia:

1. Calcula o intervalo (máximo menos mínimo) e atualiza a média e o desvio padrão.
2. Estima um intervalo de confiança para o intervalo desejado.
3. Constrói uma tendência linear dos intervalos e prevê o próximo.
4. Avalia o erro percentual absoluto médio (MAPE) da previsão.

Os valores são registrados na saída da estratégia e podem ser visualizados no gráfico.

## Notas

- A estratégia é informacional e não executa ordens.
- Os intervalos são medidos em unidades de preço; adapte o parâmetro `DesiredRange` ao seu instrumento.
