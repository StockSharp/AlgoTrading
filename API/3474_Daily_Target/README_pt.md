# Estratégia de meta diária
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`DailyTargetStrategy` replica o MetaTrader 4 consultor especialista "Daily Target". A estratégia mantém a negociação de posições abertas até
os lucros e perdas combinados do dia atual atingem uma meta de lucro configurada ou violam um limite máximo de perdas. Como
assim que qualquer um dos limites for atingido, todas as ordens ativas serão canceladas e a posição será achatada, de modo que a negociação permanecerá pausada até o
dia seguinte começa.

## Lógica de negociação

1. **Inicialização**
   - A estratégia chama `ResetDailySnapshot` durante `OnStarted` para armazenar a data atual e a linha de base do PnL realizada.
   - `SubscribeLevel1()` fornece atualizações de lance/venda que são necessárias para avaliar o lucro flutuante com precisão.
   - `SubscribeTrades()` captura o último preço executado, fornecendo um substituto quando faltam cotações.
   - Um tick `Timer` de um minuto garante que as alterações de data sejam detectadas mesmo quando nenhum dado de mercado chegar.
2. **Avaliação PnL**
   - `EvaluateDailyThresholds` recalcula o PnL realizado (atual `PnL` menos a linha de base armazenada) e adiciona o PnL flutuante
calculado a partir do último preço de compra/venda ou do último preço de negociação.
   - Se o PnL diário total ultrapassar a meta configurada ou cair abaixo do limite de perda negativa, a estratégia chama
`TriggerDailyStop`.
3. **Saída de emergência**
   - `TriggerDailyStop` grava uma entrada de registro informativa, cancela todas as ordens pendentes e envia a ordem de mercado apropriada para
nivelar a exposição longa ou curta restante.
   - `_dailyStopTriggered` impede a reentrada durante o mesmo dia. Quando a data do calendário muda, `ResetDailySnapshot` limpa isso
sinalizador e registra uma nova linha de base do PnL.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `DailyTarget` | `10` | Meta de lucro na moeda do portfólio. A negociação é interrompida durante o resto do dia quando o PnL diário total atinge ou excede esse valor. |
| `DailyMaxLoss` | `0` | Perda máxima tolerada na moeda do portfólio. Defina como zero para desativar o filtro de perda. A negociação é interrompida no dia quando o PnL diário total cai abaixo do limite negativo. |

## Notas

- A estratégia gerencia apenas o `Security` primário atribuído à instância da estratégia, espelhando o comportamento de símbolo único do
MQL especialista.
- O PnL flutuante utiliza o melhor lance para posições longas e o melhor pedido para posições curtas. Se nenhuma cotação estiver disponível, a última negociação
o preço atua como um substituto para evitar a paralisação da avaliação.
- Nenhuma porta Python é fornecida; apenas a implementação de alto nível do C# está incluída neste pacote.
