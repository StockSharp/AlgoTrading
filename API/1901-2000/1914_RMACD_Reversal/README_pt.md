# Estratégia de Reversão RMACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia usa o indicador Moving Average Convergence Divergence (MACD) para gerar sinais de reversão. Quatro modos diferentes definem como as entradas são detectadas:

1. **Breakdown** – entra comprado quando o histograma MACD cruza abaixo de zero e entra vendido quando cruza acima de zero.
2. **MacdTwist** – procura uma mudança de direção no MACD comparando os dois últimos valores do histograma.
3. **SignalTwist** – monitora a linha de sinal em busca de mudanças de direção.
4. **MacdDisposition** – entra quando o histograma MACD cruza a linha de sinal.

A estratégia sempre usa ordens a mercado e inverte posições quando um novo sinal oposto aparece.

## Parâmetros
- **Fast Length** – período para a EMA rápida dentro do MACD.
- **Slow Length** – período para a EMA lenta dentro do MACD.
- **Signal Length** – período de suavização para a linha de sinal.
- **Candle Type** – período das velas utilizadas para cálculos.
- **Mode** – seleciona o algoritmo de entrada descrito acima.

## Observações
- Os sinais são avaliados apenas em velas finalizadas.
- A estratégia armazena valores anteriores do MACD internamente em vez de solicitar dados históricos.
- Não é usado stop-loss ou take-profit explícito; as posições são fechadas apenas em sinais opostos.
