# Ichimoku com expoente de Hurst
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de tendência que combina cruzamentos Tenkan/Kijun, a nuvem Ichimoku e um filtro Hurst sobre retornos logarítmicos. Os sinais usam velas fechadas.

## Cálculo

Tenkan e Kijun são `(maior High + menor Low) / 2` das respectivas janelas. Senkou A sem deslocamento é a média das duas linhas; Senkou B é o ponto médio de `SenkouSpanBPeriod`. São usadas as linhas causais deslocadas do StockSharp Ichimoku, com deslocamento `KijunPeriod` e seu buffer de inicialização; velas futuras não são lidas.

Os últimos `HurstPeriod` fechamentos geram `n = HurstPeriod - 1` retornos `r[i] = ln(C[i] / C[i-1])`. Com média `mu`, `X[k] = sum(r[i] - mu)`, `R = max(X) - min(X)` e desvio padrão populacional `S`, o filtro é `H = ln(R / S) / ln(n)`. StockSharp HurstExponent processa esses retornos desde a primeira vela fechada. São necessários ao menos onze fechamentos, a janela completa e variância positiva. Um fechamento não positivo reinicia a janela; sem Hurst válido nenhuma entrada é permitida.

## Negociação

- **Compra:** posição zerada, fechamento estritamente acima das duas linhas, Tenkan cruza Kijun de `<=` para `>`, `H > HurstThreshold`, ATR formado e positivo e espera encerrada.
- **Venda:** posição zerada, fechamento estritamente abaixo das linhas e cruzamento inverso; valem os mesmos requisitos de Hurst, ATR e espera.
- **Saída por sinal:** fechar toda a compra abaixo das duas linhas ou no cruzamento descendente de Tenkan; fechar toda a venda nas condições inversas. As saídas permanecem ativas durante a espera e não exigem o filtro Hurst de entrada.
- **Ordens:** entrada a mercado de `Volume` e saída pelo saldo real da posição. Uma ordem ainda não concluída bloqueia novas ordens por sinal. A posição é encerrada antes de uma entrada posterior.
- **Espera:** cada ordem por sinal reinicia `SignalCooldownBars`; a próxima entrada pode ocorrer após esse número de velas fechadas.

## Proteção

O ATR de Wilder usa `AtrPeriod`. Cada entrada fixa `StopLossAtr * ATR` da vela de sinal como distância ao preço real da execução. A proteção local processa Level1 e pode zerar a posição a mercado entre velas de sinal. Mudanças posteriores do ATR não alteram a distância. `StopLossAtr = 0` desativa a proteção para a execução. Não há take-profit nem trailing stop.

## Valores padrão

| Parâmetro | Valor |
|---|---|
| `TenkanPeriod` | 9 |
| `KijunPeriod` | 26 |
| `SenkouSpanBPeriod` | 52 |
| `HurstPeriod` | 100 fechamentos |
| `HurstThreshold` | 0.5 |
| `SignalCooldownBars` | 6 |
| `AtrPeriod` | 14 |
| `StopLossAtr` | 2 |
| `CandleType` | 15 minutos |

Os testes comparam cada ordem por sinal e suas execuções com nuvens e R/S de retornos calculados independentemente no arquivo BTC/TON incluído. Também verificam aquecimento do Hurst, distâncias ATR fixas e execuções protetoras reais dentro da vela.

A comparação de proteção usa períodos Tenkan/Kijun/Senkou B 15/40/70 e HurstThreshold 0.45 para acionar a distância padrão de 2 × ATR no arquivo. Os sinais padrão 9/26/52 são verificados separadamente. Os testes de proteção usam volume de entrada 3.
