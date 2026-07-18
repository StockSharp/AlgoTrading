# Estratégia inicial de rateio superior V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este documento descreve a porta StockSharp do consultor especialista MetaTrader **earlyTopProrate_V1**. A estratégia procura movimentos intradiários que se estendem para além da abertura diária e saem da posição usando três metas de lucro. Ele foi convertido usando o StockSharp API de alto nível, preservando as ideias originais de gerenciamento de dinheiro e gerenciamento comercial.

## Lógica principal

1. **Contexto diário** – a estratégia reconstrói a abertura, a máxima e a mínima do dia atual a partir das velas processadas. A direção dominante é definida comparando `high - open` e `open - low`.
2. **Janela de entrada** – novas negociações só poderão ser abertas entre `StartHour` (inclusive) e `EndHour` (exclusivo). A configuração padrão negocia no início da sessão europeia.
3. **Condições de entrada** –
   - Quando a direção dominante é de alta e o último preço de fechamento está acima da abertura diária, a estratégia abre uma posição longa.
   - Quando a direção dominante é de baixa e o último preço de fechamento está abaixo da abertura diária, a estratégia abre uma posição curta.
   - Apenas uma posição de mercado é permitida ao mesmo tempo (`MaxPositions = 1` por padrão).
4. **Gestão de dinheiro** – o volume de cada entrada é obtido a partir do modo de gestão de dinheiro selecionado (veja abaixo). O valor é arredondado usando o passo de volume do instrumento e fixado entre o volume mínimo e máximo do câmbio.
5. **Tratamento de posição** – depois de entrar em uma posição, a estratégia aplica as regras de saída em camadas listadas na próxima seção. As regras refletem o consultor especialista original, mas são implementadas com ordens StockSharp de alto nível em vez de modificações diretas de stop-loss/take-profit.
6. **Fechamento da sessão** – se uma posição permanecer aberta quando `ClosingHour` for alcançado, a estratégia forçará uma saída no mercado.

## Detalhes de gerenciamento comercial

O consultor especialista MQL original depende de ajustes manuais de stop e take-profit. A porta StockSharp reproduz o comportamento com verificações explícitas em cada vela finalizada:

- **Resgate do ponto de equilíbrio** (`BreakEvenTrigger`) – se o preço se mover contra a entrada pelo número de pontos configurado, a estratégia aguarda uma recuperação de volta ao preço de entrada e então sai no ponto de equilíbrio.
- **Parada de emergência** (`StopLoss0`) – quando a excursão adversa ultrapassa esta distância a posição é fechada imediatamente.
- **Stop para entrada** (`StopLoss1`) – após um movimento positivo da distância especificada, o stop de proteção é movido para o preço de entrada.
- **Stop no lucro** (`StopLoss2`) – quando o lucro atinge esse limite, o stop de proteção é empurrado acima (longo) ou abaixo (curto) da entrada. O deslocamento é igual a `StopLoss2 - StopLoss1`, reproduzindo a lógica `setSL2-35` de MetaTrader.
- **Escalonamento** (`TakeProfit1/2/3` e `Ratio1/2/3`) – três objetivos de lucro acionam fechamentos parciais do volume de posição restante. Os índices representam porcentagens da posição atual para que as metas subsequentes trabalhem na exposição reduzida. O terceiro alvo fecha todo o restante.

Todos os parâmetros baseados em distância operam em *pontos*. O parâmetro auxiliar `PointMultiplier` multiplica o instrumento `PriceStep` para reproduzir a aritmética `value * 10 * Point` do script original (multiplicador padrão = 10).

## Modos de gerenciamento de dinheiro

O parâmetro `MoneyManagementType` seleciona um dos quatro modelos de dimensionamento:

| Modo | Descrição |
| --- | --- |
| `0` or `1` | Tamanho de lote fixo igual a `BaseVolume` (espelha o comportamento de MQL onde os modos 0 e 1 são idênticos). |
| `2` | Modelo de raiz quadrada – usa `0.1 * sqrt(balance / 1000) * MoneyManagementFactor`. O valor atual do portfólio é usado quando disponível. |
| `3` | Modelo de risco patrimonial – calcula `equity / price / 1000 * MoneyManagementRiskPercent / 100`, aproximando a fórmula `AccountEquity/Close[0]` de MetaTrader. |

Cada resultado é normalizado usando a etapa de volume do instrumento e o volume mínimo/máximo de troca.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `CandleType` | Série de velas usadas para decisões. O padrão é velas de 5 minutos. |
| `StartHour` / `EndHour` | Janela de negociação em horas (0–23). |
| `ClosingHour` | Hora em que qualquer posição aberta é fechada. |
| `TimeZoneShift` | Deslocamento de fuso horário informativo mantido para compatibilidade. |
| `BaseVolume` | Tamanho base do lote antes dos ajustes de gestão de dinheiro. |
| `MaxPositions` | Máximas posições simultâneas (padrão = 1). |
| `TakeProfit1`, `TakeProfit2`, `TakeProfit3` | Distâncias, em pontos, das três metas de lucro. |
| `BreakEvenTrigger` | Perda, em pontos, que aciona a saída de resgate do ponto de equilíbrio. |
| `StopLoss0`, `StopLoss1`, `StopLoss2` | Limites adversos/lucrativos controlando a lógica de parada de proteção. |
| `Ratio1`, `Ratio2`, `Ratio3` | Porcentagens da posição atual fechada em cada alvo. |
| `MoneyManagementType` | Modo de gerenciamento de dinheiro (0–3). |
| `MoneyManagementFactor` | Multiplicador para o modelo de raiz quadrada. |
| `MoneyManagementRiskPercent` | Percentual de risco para o modelo de ações. |
| `PointMultiplier` | Multiplicador aplicado à etapa de preço do instrumento ao converter pontos em compensações de preço reais. |

## Notas de uso

- Escolha um tipo de vela que corresponda à granularidade dos dados disponíveis no local selecionado. A série padrão de 5 minutos fornece um equilíbrio entre capacidade de resposta e filtragem de ruído.
- Ao converter distâncias baseadas em pontos em preços reais, a estratégia multiplica `PriceStep * PointMultiplier`. Ajuste o multiplicador se o corretor definir pontos de forma diferente do ambiente MetaTrader original.
- A lógica de ponto de equilíbrio e trailing requer velas concluídas, portanto o comportamento intrabarra pode diferir ligeiramente da execução MetaTrader baseada em tick. O README destaca essa aproximação para que possa ser considerada durante o teste.
- `TimeZoneShift` é preservado para documentação. Os próprios horários de negociação devem ser configurados usando `StartHour`, `EndHour` e `ClosingHour`.

## Primeiros passos

1. Adicione a estratégia ao seu projeto StockSharp ou execute-a dentro do Designer/Runner.
2. Configure a série de velas (`CandleType`) e o horário de negociação do instrumento que você pretende negociar.
3. Ajuste os limites e proporções baseados em pontos de acordo com a volatilidade do instrumento.
4. Selecione um modo de gerenciamento de dinheiro e defina os parâmetros correspondentes (`BaseVolume`, `MoneyManagementFactor`, `MoneyManagementRiskPercent`).
5. Execute primeiro a estratégia na negociação de papel para validar se o comportamento corresponde às suas expectativas antes de usá-la com capital real.
