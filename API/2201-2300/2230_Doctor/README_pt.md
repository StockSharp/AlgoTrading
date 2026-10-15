# Estratégia Doctor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia #15233 "Doctor" convertida de MQL para StockSharp.

## Visão geral
A estratégia combina vários indicadores clássicos para detectar a direção da tendência e o momentum:

- **Detecção de inclinação** usando uma Média Móvel Ponderada de 40 períodos para avaliar a direção da tendência.
- **Localização linear** via uma Média Móvel Ponderada de 400 períodos comparada com as máximas e mínimas das últimas três velas.
- **Confirmação de momentum** com o Índice de Força Relativa dos períodos 14 e 5.
- **Filtro de reversão de tendência** do Parabolic SAR.

Uma posição comprada é aberta quando todas as condições de alta se alinham, e uma posição vendida quando todas as baixistas se alinham. Posições existentes são fechadas em sinais opostos ou quando os níveis de proteção são atingidos. Um trailing stop opcional avança o stop loss assim que metade da distância do stop é atingida.

## Parâmetros
- `StopLossTicks` – distância do stop loss em ticks.
- `TakeProfitTicks` – distância do take profit em ticks.
- `TrailingStop` – ativa a lógica do trailing stop.
- `CandleType` – período de tempo usado para as velas (padrão 30 minutos).

## Regras de trading
1. **Comprar** quando:
   - A inclinação de WMA(40) está subindo.
   - WMA(400) está acima das máximas das últimas três velas.
   - RSI(14) está acima de 50 e RSI(5) está abaixo de RSI(14).
   - Sem posição comprada aberta.
2. **Vender** quando:
   - A inclinação de WMA(40) está caindo.
   - WMA(400) está abaixo das mínimas das últimas três velas.
   - RSI(14) está abaixo de 50 e RSI(5) está acima de RSI(14).
   - Sem posição vendida aberta.
3. **Sair** quando as condições opostas ocorrem ou os níveis de stop loss/take profit são atingidos. O trailing stop atualiza o nível de stop após o preço se mover metade da distância do stop a favor.

## Indicadores
- Média Móvel Ponderada (40, 400)
- Índice de Força Relativa (14, 5)
- Parabolic SAR
