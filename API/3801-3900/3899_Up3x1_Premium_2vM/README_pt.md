# Estratégia Up3x1 Premium 2vM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia é uma porta direta do consultor especialista MetaTrader 4 *up3x1_Premium_2vM*. Negocia um único símbolo e mantém no máximo uma posição aberta a qualquer momento. As entradas contam com uma combinação de médias móveis suavizadas, faixas de velas fortes e um filtro diário de rompimento à meia-noite. O risco é gerenciado por meio de distâncias fixas de take-profit e stop-loss expressas em pontos de preço, enquanto um trailing stop opcional reproduz o comportamento do EA original que estreita continuamente os stops quando o mercado se move a favor da posição.

## Como funciona

1. O período principal é configurável; o EA originalmente usava o período do gráfico. Duas médias móveis suavizadas (SMMA) com períodos 12 e 26 estão vinculadas à assinatura da vela usando o preço típico.
2. Um fluxo de vela diário separado reconstrói os dados D1 usados pela lógica MQL para o filtro de breakout da meia-noite e para a média móvel simples diária de 10 períodos.
3. Quando plana, a estratégia avalia as duas velas concluídas anteriores e os valores SMMA armazenados em cache:
   - **Viés longo**: ou o SMMA rápido cruza acima do SMMA lento enquanto ambas as aberturas aumentam, ou a última vela mostra um corpo de alta acima dos limites do intervalo configurado, ou a última vela diária fecha em alta após um grande intervalo. O EA original também comparou o SMA diário com o preço de venda; como a condição sempre é avaliada como verdadeira, ela é preservada para compatibilidade.
   - **Viés de venda**: condições simétricas das regras longas usando faixas de baixa e cruzamentos.
   - Se qualquer condição comprada for satisfeita, uma compra de mercado será emitida; caso contrário, se qualquer condição curta for mantida, uma venda no mercado será colocada. O tamanho do lote solicitado é normalizado para a etapa do volume de segurança antes do envio do pedido.
4. Enquanto uma posição está aberta, a estratégia monitora os valores SMMA rápido/lento da vela anterior. Quando a diferença absoluta fica abaixo de `ConvergenceTolerance` a posição é fechada, reproduzindo a verificação de igualdade no consultor especialista.
5. O módulo final rastreia o preço médio de entrada. Uma vez que o preço ultrapassa a distância final, o nível de stop avança para manter o gap configurado. Tocar nesse nível fecha a posição imediatamente, emulando as repetidas chamadas `OrderModify` de MQL.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | `TimeFrame(1h)` | Período principal usado para entradas. |
| `FastMaPeriod` | `12` | Comprimento da média móvel suavizada rápida (preço típico). |
| `SlowMaPeriod` | `26` | Comprimento da média móvel suavizada lenta (preço típico). |
| `RangeThreshold` | `0.0060` | Faixa mínima de velas exigida pelo filtro de momentum. |
| `BodyThreshold` | `0.0050` | Tamanho mínimo do corpo da vela para a condição de intervalo. |
| `DailyRangeThreshold` | `0.0060` | Distância mínima de abertura e fechamento na última vela diária para o filtro de rompimento da meia-noite. |
| `TakeProfitPoints` | `150` | Distância de lucro expressa em faixas de preço. Defina como `0` para desativar. |
| `StopLossPoints` | `100` | Distância de stop-loss expressa em pontos de preço. Defina como `0` para desativar. |
| `TrailingStopPoints` | `10` | Distância entre o preço e o trailing stop. Defina como `0` para desativar o rastreamento. |
| `TradeVolume` | `0.05` | Tamanho do lote utilizado para ordens de mercado antes da normalização do volume. |
| `ConvergenceTolerance` | `0.00001` | Diferença máxima entre os SMMAs que aciona a liquidação da posição. |

## Notas

- A estratégia mantém a peculiaridade EA original, onde a comparação diária SMA é sempre verdadeira, garantindo a paridade do recurso com a fonte MQL.
- As ordens stop-loss e take-profit são registradas através de `StartProtection` e, portanto, adaptam-se automaticamente ao tamanho do passo da corretora, quando disponível.
- A lógica final requer um valor `TrailingStopPoints` positivo e um `Security.PriceStep` válido. Quando alguma informação estiver faltando, a parada não será rastreada.
- A normalização do volume respeita as restrições de troca (`VolumeStep`, `VolumeMin`, `VolumeMax`). Valores negativos para `TradeVolume` podem ser usados ​​para emular o dimensionamento baseado em porcentagem depois que a lógica personalizada for adicionada.
