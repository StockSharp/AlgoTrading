# Estratégia de 20 Pips Oposta à Tendência das Últimas N Horas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia do StockSharp é uma portagem de alto nível do Expert Advisor MetaTrader
**«20 Pips Opposite Last N Hour Trend»**. Ela observa velas horárias, mede
como o preço se comportou durante as `N` horas anteriores e, em seguida, abre uma posição na
direção oposta quando a hora de negociação configurada termina. A operação é
gerenciada usando um alvo fixo de take-profit de 20 pips e um tempo limite horário, enquanto
uma escala de volume estilo martingale é aplicada após perdas consecutivas.

A implementação usa as subscrições de velas do StockSharp, o sistema de parâmetros,
e os helpers de ordens (`BuyMarket`, `SellMarket`) para que possa ser executada sem alterações dentro de
Designer, API, Runner ou Shell.

## Lógica de negociação

- A estratégia subscreve o tipo de vela selecionado (padrão: barras de 1 hora).
- Para cada vela concluída mantém o preço de fechamento dentro de um histórico interno.
- Quando uma vela com `OpenTime.Hour == TradingHour` é completada e há historial suficiente
  disponível:
  - Compare o fechamento que ocorreu `HoursToCheckTrend` barras atrás com o
    fechamento anterior (1 barra atrás).
  - Se o preço diminuiu nessa janela (deriva baixista) a estratégia compra;
    se o preço aumentou (deriva altista) vende. Fechamentos iguais ignoram a negociação.
- Apenas uma operação é aberta por dia e exclusivamente na hora de negociação configurada.
  Todas as outras velas são usadas puramente para gestão.

## Gestão de posição

- Um alvo de 20 pips (ajustado para símbolos de 3/5 dígitos) é calculado logo após a
  entrada. Quando qualquer vela concluída mostra que o máximo/mínimo tocou o alvo, a
  posição é fechada nesse nível.
- Se o alvo não for alcançado durante a próxima hora, a posição é fechada ao
  final da vela seguinte para evitar exposição noturna.
- Os contadores diários são redefinidos automaticamente quando um novo dia de negociação começa, para que
  o próximo sinal elegível possa disparar na sessão seguinte.

## Gestão de capital

- `Volume` define o tamanho base da ordem. `MaxVolume` limita o tamanho resultante de qualquer
  passo de martingale.
- Após uma saída perdedora a estratégia aumenta a próxima posição pelo
  multiplicador apropriado: primeira perda → `FirstMultiplier`, segunda perda →
  `SecondMultiplier`, etc. Sequências de perdas além de cinco operações reutilizam o quinto
  multiplicador. Qualquer fechamento lucrativo ou no ponto de equilíbrio reinicia a sequência.
- Os cálculos de volume dependem do último preço de posição executado, para que a detecção de lucro/perda
  permaneça determinista mesmo sem dados completos de PnL do broker.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `MaxPositions` | 9 | Máximo de operações permitidas por dia. Defina como 0 para desabilitar a negociação. |
| `Volume` | 0.1 | Volume base para a primeira operação de uma sequência. |
| `MaxVolume` | 5 | Limite máximo para o volume ajustado após multiplicadores. |
| `TakeProfitPips` | 20 | Distância de take-profit em pips. 0 desabilita o TP. |
| `TradingHour` | 7 | Hora do dia (0-23) habilitada para abrir uma posição. |
| `HoursToCheckTrend` | 24 | Número de fechamentos horários usados para medir a tendência anterior. |
| `FirstMultiplier` | 2 | Multiplicador aplicado após a primeira perda consecutiva. |
| `SecondMultiplier` | 4 | Multiplicador aplicado após a segunda perda consecutiva. |
| `ThirdMultiplier` | 8 | Multiplicador aplicado após a terceira perda consecutiva. |
| `FourthMultiplier` | 16 | Multiplicador aplicado após a quarta perda consecutiva. |
| `FifthMultiplier` | 32 | Multiplicador aplicado a partir da quinta perda em diante. |
| `CandleType` | H1 | Tipo de dados de vela usado para geração de sinais e gestão. |

## Notas adicionais

- O tamanho do pip é calculado a partir de `Security.PriceStep` e do número de decimais para que
  o alvo de 20 pips funcione corretamente em símbolos FX de 4 e 5 dígitos.
- `StartProtection()` é invocado quando a estratégia inicia, habilitando as proteções integradas
  do StockSharp (stop automático para posições sem limite, reinicios de carteira).
- A lógica usa apenas velas concluídas e nunca lê valores de indicadores
  diretamente, cumprindo as diretrizes do `AGENTS.md`.

> **Aviso de risco:** O dimensionamento de posição estilo martingale pode levar a
> drawdowns substanciais. Sempre teste os parâmetros em dados históricos e use limites de risco prudentes
> antes de implementar em negociação ao vivo.
