# Estratégia MACD EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port StockSharp do expert advisor do MetaTrader 5 `MACD EA (barabashkakvn's edition).mq5` da pasta `MQL/20010`. Recria a mesma lógica de cruzamento MACD, tomada de lucro parcial e recursos de gestão de capital usando a API de alto nível do StockSharp.

## Lógica de trading

* **Fonte do sinal** – Um indicador MACD clássico é calculado com períodos rápidos, lentos e de sinal configuráveis. A estratégia examina a diferença entre a linha MACD e a linha de sinal duas e quatro velas concluídas atrás. Um cruzamento altista (a diferença passa de negativa para positiva) abre uma operação comprada, enquanto a condição oposta abre uma operação vendida.
* **Gestão de posição** – Cada ordem é protegida por offsets configuráveis de stop-loss e take-profit medidos em pips. Os offsets são convertidos em preços usando o passo de preço do instrumento e multiplicando por dez quando o instrumento tem 3 ou 5 casas decimais, imitando o ajuste de ponto do EA original.
* **Lucro parcial** – Quando habilitado, metade da posição aberta é fechada assim que o preço percorre `PartialProfitPips` na direção da operação. A parte restante continua.
* **Breakeven** – Após o preço avançar `BreakevenPips` a favor, a estratégia ativa um guardião de breakeven. Se o preço retornar ao nível de entrada original, a posição é fechada no preço de entrada, assim como o EA move o stop para breakeven.
* **Sinal MACD oposto** – Um cruzamento MACD oposto fecha qualquer exposição restante imediatamente, garantindo que a estratégia nunca mantenha uma posição contra a tendência do indicador.

## Gestão de capital

Quando `UseMoneyManagement` está habilitado, o tamanho da posição aumenta após operações perdedoras consecutivas. A próxima operação usa um multiplicador baseado no número de perdas consecutivas (x2 após uma perda, x3 após duas perdas, até x7 para seis ou mais perdas). O multiplicador é combinado com o parâmetro `RiskMultiplier` para reproduzir o dimensionamento estilo martingale do código original. Operações vencedoras reiniciam o contador de perdas para zero.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | Comprimentos de cálculo do MACD.
| `StopLossPips` | Distância ao stop de proteção em pips (0 o desabilita).
| `TakeProfitPips` | Distância ao alvo de lucro em pips (0 o desabilita).
| `PartialProfitPips` | Pips necessários para fechar metade da posição (0 desabilita a saída parcial).
| `BreakevenPips` | Pips necessários antes de o modo breakeven ser ativado (0 desabilita o breakeven).
| `UseMoneyManagement` | Habilita o dimensionamento dinâmico de posição baseado na sequência de perdas.
| `RiskMultiplier` | Multiplicador adicional aplicado quando a gestão de capital está ativa.
| `BaseVolume` | Volume base de operação antes de qualquer escalonamento.
| `CandleType` | Série de velas usada para cálculos de indicadores.

## Notas

* A estratégia usa `SubscribeCandles` e vinculação de indicadores para seguir o padrão recomendado da API de alto nível.
* Uma versão em Python ainda não está disponível. Apenas a implementação em C# na pasta `CS` é fornecida.
* Testes não foram adicionados ou modificados conforme solicitado.
