# Estratégia ProffessorV3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

Esta estratégia é uma conversão completa do especialista do MetaTrader *ProffessorV3* para a
API de alto nível do StockSharp. Mantém o conceito original de combinar a filtragem de regime
ADX com uma grade de ordens de proteção e médio.

- **Indicador**: Average Directional Index (ADX) de 14 períodos com valores +DI/-DI.
- **Modos**: regime plano (ADX abaixo do limiar) e regime de tendência (ADX acima do limiar).
- **Ordens**: abre uma posição de mercado e rodeia o preço com ordens pendentes
  para hedge, piramidação ou reversão à média.
- **Saída**: fecha todas as posições e ordens pendentes quando o nível configurado de
  lucro ou perda é atingido.
- **Horário**: opera apenas dentro do intervalo de horas selecionado.

## Lógica de Trading

### Detecção de regime
1. Assinar o tipo de vela configurado e calcular os valores ADX.
2. Atrasar o sinal ADX pelo número configurado de velas fechadas (`BarOffset`)
   para replicar o uso original de `CopyBuffer(handle, shift)`.
3. Quando não há posição aberta, avaliar os últimos valores ADX atrasados:
   - *Plano altista*: `ADX < AdxFlatLevel` e `+DI > -DI`.
   - *Plano baixista*: `ADX < AdxFlatLevel` e `+DI < -DI`.
   - *Tendência altista*: `ADX ≥ AdxFlatLevel` e `+DI > -DI`.
   - *Tendência baixista*: `ADX ≥ AdxFlatLevel` e `+DI < -DI`.

### Colocação de ordens
Para cada modo, a estratégia abre uma posição de mercado com o volume base e
depois coloca uma grade simétrica ao redor do preço atual. As distâncias da grade são
expressas em "pontos" exatamente como no código MQL e são automaticamente escaladas
pelo passo de preço do instrumento.

- **Plano altista**: entrada longa no mercado, sell-stop de proteção abaixo do bid, buy limits
  abaixo do ask e sell limits acima do bid para capturar oscilações.
- **Plano baixista**: entrada vendida no mercado, buy-stop de proteção acima do ask, buy limits
  em pullbacks e sell limits mais altos para recarregar vendidos.
- **Tendência altista**: entrada longa no mercado, sell-stops para hedge e buy-stops
  para piramidação em rompimento.
- **Tendência baixista**: entrada vendida no mercado, sell-stops para seguir a tendência e
  buy-stops para limitar reversões.

O espaçamento da grade é calculado com a mesma fórmula do original: cada nível
adiciona `GridStep + GridDeltaIncrement * level / 2`. O volume para cada ordem pendente
é ajustado com `LotMultiplier` e `LotAddition`, depois normalizado para o passo de volume
da bolsa e seus limites.

### Gestão de saída
- O lucro não realizado é calculado a partir do preço médio da posição estratégica
  e do fechamento da última vela.
- Se o lucro exceder `ProfitTarget` ou cair abaixo de `LossLimit` (quando este
  último é diferente de zero), a estratégia fecha a posição líquida e cancela todas as
  ordens pendentes.
- O trading é ignorado fora do intervalo `[StartHour, EndHour)`, correspondendo ao
  auxiliar `Time()` original.

## Notas de Implementação

- Os preços bid/ask para ordens pendentes são aproximados a partir do fechamento do último
  candle mais/menos metade do passo de preço. Isso reflete a lógica baseada em ticks
  em um ambiente orientado por velas.
- Os valores de ponto são escalados pelo passo de preço do símbolo e ajustados para cotações
  de três e cinco dígitos exatamente como a variável MQL `m_adjusted_point`.
- A normalização de volume e preço respeita o passo, mínimo e máximo do símbolo
  antes de enviar qualquer ordem.
- A estratégia processa apenas velas terminadas para evitar sinais prematuros.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `Volume` | Volume base da ordem de mercado. |
| `LotMultiplier` | Multiplicador aplicado ao volume de cada ordem pendente. |
| `LotAddition` | Volume adicional adicionado às ordens pendentes após o multiplicador. |
| `MaxLevels` | Número máximo de níveis de grade por lado. |
| `GridDeltaIncrement` | Incremento adicionado ao espaçamento da grade à medida que os níveis se aprofundam (pontos). |
| `GridInitialOffset` | Distância até a primeira ordem de proteção (pontos). |
| `GridStep` | Distância base entre níveis consecutivos (pontos). |
| `ProfitTarget` | Nível de lucro não realizado que aciona o fechamento de tudo. |
| `LossLimit` | Nível de perda não realizada que aciona o fechamento de tudo (0 desabilita). |
| `AdxFlatLevel` | Limiar ADX que separa os regimes plano e de tendência. |
| `BarOffset` | Número de velas fechadas usadas para atrasar os valores ADX. |
| `StartHour` | Hora em que a janela de trading abre (UTC). |
| `EndHour` | Hora em que a janela de trading fecha (UTC). |
| `CandleType` | Série de velas usada para os cálculos. |
