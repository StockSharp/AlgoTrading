# Estratégia Volume Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
- Port do especialista MetaTrader 5 **"Volume trader" (ID 21050)** de Vladimir Karputov.
- Recriado sobre a API de estratégia de alto nível do StockSharp.
- Opera na direção da mudança mais recente de volume de tick enquanto um filtro de sessão de trading personalizado está ativo.

## Lógica de Trading
1. Assina velas definidas por `CandleType` (padrão: período de 1 hora) e lê seu volume de tick (`TotalVolume`).
2. Em cada vela finalizada, a estratégia compara os volumes das **duas** velas fechadas anteriores, emulando o script MQL5 que é executado no nascimento de uma nova barra.
3. Se o volume mais recente é maior que o anterior e não há posição comprada, a estratégia compra contratos de `Volume` e adicionalmente cobre uma posição vendida existente.
4. Se o volume mais recente é menor que o anterior e não há posição vendida, a estratégia vende contratos de `Volume` e adicionalmente fecha uma posição comprada existente.
5. Sinais de trading são ignorados quando o horário de abertura da próxima barra cai fora da janela `[StartHour, EndHour]`. O intervalo padrão 09:00–18:00 replica as entradas originais.
6. Nenhum stop loss ou take profit é definido por padrão; a estratégia simplesmente reverte no sinal oposto.

## Gestão de Ordens
- Ordens de entrada são enviadas via `BuyMarket` ou `SellMarket` para inverter a posição imediatamente no início de uma nova vela.
- Quando um sinal de reversão aparece, a estratégia automaticamente negocia o tamanho absoluto da posição mais o `Volume` configurado, garantindo que a posição anterior seja fechada antes que uma nova seja aberta.
- Não há lógica de dimensionamento de posição incorporada além do parâmetro fixo `Volume`.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | Período de 1 hora | Série de velas usada para calcular o volume de tick. Ajustar para corresponder ao período usado no especialista original. |
| `StartHour` | 9 | Hora inclusiva (0–23) que marca o início da sessão de trading. Sinais antes desta hora são ignorados. |
| `EndHour` | 18 | Hora inclusiva (0–23) que marca o fim da sessão de trading. Sinais após esta hora são ignorados. |
| `Volume` | 0.1 | Volume de ordem para novas entradas. Também usado ao inverter uma posição existente. |

## Notas de Uso
- Garantir que a fonte de dados forneça volume de tick nas mensagens de vela. Quando apenas o volume negociado real estiver disponível, o comportamento seguirá esses dados.
- Alinhar o parâmetro `CandleType` com o período do gráfico que se deseja reproduzir do MetaTrader.
- Considerar envolver a estratégia com gestão de risco externa (stop loss, take profit, limites de perda diária) se exigido pelas regras de trading.
- A estratégia chama `LogInfo` quando uma posição é aberta, facilitando a auditoria de decisões de sinal no log.

## Diferenças vs. Implementação MQL Original
- Usa o pipeline de assinatura de velas do StockSharp em vez de chamar `CopyTickVolume` manualmente.
- A filtragem de sessão baseia-se no `CloseTime` da vela finalizada (a hora de início da próxima barra) para permanecer alinhado com a lógica MQL que é executada na abertura da barra.
- A execução de ordens é tratada por helpers de API de alto nível (`BuyMarket`, `SellMarket`) em vez de chamadas diretas a `CTrade`.
