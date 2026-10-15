# Estratégia cruzada de preços MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia MA Price Cross é uma conversão direta do MetaTrader 4 consultor especialista "MA Price Cross" para o StockSharp API de alto nível. Ele espera que a média móvel selecionada cruze o preço atual enquanto a negociação é permitida dentro de uma janela de tempo configurável. Quando o cruzamento acontece por baixo, o algoritmo abre uma posição comprada; quando o cruzamento acontece por cima, abre uma posição curta. As distâncias protetoras de stop-loss e take-profit são definidas em MetaTrader pontos e convertidas automaticamente em compensações de preço absoluto usando o `PriceStep` do instrumento.

Ao contrário da implementação original MQL, que reage a cada tick, a versão StockSharp funciona com velas finalizadas e usa a assinatura de alto nível `SubscribeCandles`. Isso garante que as decisões de negociação sejam executadas uma vez por barra e permaneçam compatíveis com o pipeline de vinculação do indicador. A média móvel pode ser configurada para corresponder a todos os quatro modos MetaTrader e aceita diferentes fontes de preços (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado).

## Lógica de negociação

1. Aguarde até que o horário atual caia na janela de negociação `[StartTime, StopTime)`. As janelas noturnas são suportadas por volta da meia-noite.
2. Processe apenas velas concluídas. Alimente a média móvel configurada com o preço aplicado escolhido.
3. Armazene o valor da média móvel anterior para emular a lógica de deslocamento `iMA` usada em MetaTrader.
4. Quando a média anterior estiver abaixo do preço mais recente e a nova média estiver acima do preço, abra (ou inverta) uma posição longa.
5. Quando a média anterior estiver acima do preço mais recente e a nova média estiver abaixo do preço, abra (ou inverta) uma posição curta.
6. Antes de abrir uma nova posição no lado oposto, nivele qualquer exposição existente para espelhar a restrição `OrdersTotal() == 0` do código original.
7. Inicie um stop-loss e um take-profit virtuais com distâncias expressas em MetaTrader pontos multiplicados pelo instrumento atual `PriceStep`.

## Parâmetros padrão

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | `TimeFrame(1m)` | Série de velas que orienta todos os cálculos. |
| `MaPeriod` | `160` | Número de barras utilizadas pela média móvel. |
| `MaMethod` | `Simple` | Tipo de média móvel: Simples, Exponencial, Suavizada ou LinearWeighted. |
| `PriceType` | `Close` | Fonte de preço encaminhada para média móvel (fechamento/abertura/máxima/mínima/mediana/típica/ponderada). |
| `StartTime` | `01:00` | Hora do dia em que a negociação se torna ativa. |
| `StopTime` | `22:00` | Hora do dia em que novas entradas são interrompidas. |
| `StopLossPoints` | `200` | MetaTrader pontos convertidos em uma distância de parada de proteção absoluta. |
| `TakeProfitPoints` | `600` | MetaTrader pontos convertidos em uma distância alvo de lucro absoluto. |
| `OrderVolume` | `0.1` | Volume padrão enviado com ordens de mercado. |

## Notas

- Se `StartTime` for igual a `StopTime`, o filtro de horário será desativado e a negociação será permitida o dia todo.
- Quando `StopLossPoints` ou `TakeProfitPoints` é igual a zero, o nível de proteção correspondente não é registrado.
- O filtro de tempo usa o tempo de fechamento da vela (`candle.CloseTime.TimeOfDay`) para se adaptar ao fuso horário de troca fornecido pelo MarketDataConnector.
- Se a segurança não expor `PriceStep`, as distâncias baseadas em pontos serão usadas diretamente, sem escala.

## Referência de estratégia original

- Fonte: `MQL/44133/MA Price Cross.mq4`
- Autor: JBlanked (2023)
