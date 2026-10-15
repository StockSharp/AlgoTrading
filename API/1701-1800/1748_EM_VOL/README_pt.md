# Estratégia EM VOL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia rompimentos em torno de níveis de suporte e resistência baseados em pivôs.
Calcula o máximo e mínimo do dia anterior mais um buffer ATR para formar gatilhos de entrada.
As operações são colocadas apenas quando o indicador ADX sinaliza um ambiente de baixa volatilidade.

## Lógica

1. Calcular o pivô da barra anterior e adicionar/subtrair ATR para obter resistência e suporte.
2. Se o ADX estiver abaixo do limiar e o preço fechar acima da resistência, entrar em posição comprada.
3. Se o preço fechar abaixo do suporte, entrar em posição vendida.
4. Após a entrada, ordens de stop de proteção e take profit são colocadas.
5. Um trailing stop pode ajustar o stop de proteção assim que o lucro atingir o nível especificado.

## Parâmetros

- `TakeProfit` — distância do take profit em passos de preço.
- `StopLoss` — distância do stop loss em passos de preço.
- `AtrPeriod` — período do indicador ATR.
- `AdxPeriod` — período do indicador ADX.
- `AdxThreshold` — valor máximo de ADX para permitir negociação.
- `TrailStart` — lucro necessário antes de o trailing stop começar.
- `TrailStep` — distância do trailing stop.
- `CandleType` — período usado para os cálculos.

## Indicadores Utilizados

- Average True Range
- Average Directional Index
