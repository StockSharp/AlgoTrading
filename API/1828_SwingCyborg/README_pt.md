# Estratégia Swing Cyborg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Swing Cyborg é um assistente discricionário que automatiza a execução com base na própria previsão de tendência do trader. O usuário define a direção esperada da tendência e a janela de tempo em que ela deve ser válida. A estratégia confirma entradas com o indicador RSI e gerencia saídas com alvos fixos.

## Parâmetros
- `Volume` – volume de ordem em lotes.
- `TrendPrediction` – direção esperada da tendência (Uptrend ou Downtrend).
- `TrendTimeframe` – período usado para RSI e negociação (M30, H1 ou H4).
- `TrendStart` – início do período de tendência definido pelo usuário.
- `TrendEnd` – fim do período de tendência definido pelo usuário.
- `Aggressiveness` – preset de gestão de dinheiro:
  - Baixo: take profit 300 pips, stop loss 200 pips.
  - Médio: take profit 500 pips, stop loss 250 pips.
  - Alto: take profit 600 pips, stop loss 300 pips.

## Lógica de trading
1. Aguardar uma nova vela no período selecionado.
2. Operar apenas se o horário atual estiver entre `TrendStart` e `TrendEnd`.
3. Calcular RSI(14).
4. Se não houver posição aberta:
   - Se `TrendPrediction` for Uptrend e RSI ≤ 65 → comprar.
   - Se `TrendPrediction` for Downtrend e RSI ≥ 35 → vender.
5. `StartProtection` fecha automaticamente a posição quando o lucro ou perda atingir o nível predefinido.

A estratégia opera em velas fechadas e não abre uma nova posição enquanto houver uma ativa.
