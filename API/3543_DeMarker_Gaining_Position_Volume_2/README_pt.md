# Estratégia DeMarker ganhando posição Volume 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o consultor especialista MetaTrader 5 **"DeMarker ganhando volume de posição 2"** usando o StockSharp de alto nível de API. Ele analisa uma série de velas configuráveis ​​com o oscilador DeMarker e reage quando o valor entra em zonas extremas. A implementação mantém o sabor original de gerenciamento de dinheiro com tamanho de lote fixo, reversão opcional de sinais, tratamento integrado de stop-loss/take-profit e um filtro de sessão de negociação opcional.

## Comportamento original de especialista

* **Plataforma**: MetaTrader 5.
* **Indicador**: oscilador DeMarker clássico (`DEM`), período padrão 14.
* **Entradas**: abra posições compradas quando o DeMarker cair abaixo de um limite inferior, abra posições vendidas quando ele subir acima de um limite superior.
* **Controles de risco**: stop-loss/take-profit fixo expresso em pontos, trailing stop opcional com step, janela de tempo opcional.
* **Gerenciamento de posição**: garanta apenas uma negociação por barra e feche o lado oposto antes de mudar de direção.

A conversão StockSharp segue os mesmos princípios. As ordens de proteção são implementadas com `StartProtection`, portanto, stop-loss, take-profit e trailing são gerenciados automaticamente assim que uma posição é aberta.

## Lógica de negociação

1. Assine o tipo de vela configurado (`CandleType`, velas de 5 minutos por padrão) e calcule o valor DeMarker com o período escolhido (`DeMarkerPeriod`).
2. Quando uma vela fecha, avalie o oscilador:
   * Se `ReverseSignals` for **falso** (padrão):
     * **Configuração longa** – `DeMarker <= LowerLevel`.
     * **Configuração curta** – `DeMarker >= UpperLevel`.
   * Se `ReverseSignals` for **true**, as regras longas/curtas são trocadas.
3. Negocie apenas dentro da janela de sessão opcional definida por `SessionStart`/`SessionEnd` quando `UseTimeFilter` estiver ativado. Sessões noturnas são suportadas.
4. Execute no máximo uma nova entrada por vela. Antes de abrir uma nova posição, a estratégia fecha quaisquer participações opostas para espelhar a lógica MT5.
5. Os volumes são fixados pelo parâmetro `TradeVolume`. Se a estratégia já estiver parcialmente na direção desejada, ela completa o volume solicitado.

## Gestão de risco

* `StopLossPoints` e `TakeProfitPoints` (em etapas de preço) mapeiam as distâncias de stop e take-profit baseadas em pontos do especialista.
* Ativar `EnableTrailing` muda a distância de parada para `TrailingStopPoints` e ativa o mecanismo de rastreamento integrado usando `TrailingStepPoints` como etapa de ajuste.
* `StartProtection` está configurado com `useMarketOrders = true` para que as ordens de proteção sejam executadas imediatamente, semelhante ao comportamento de fechamento de negociação MT5.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `DeMarkerPeriod` | Período médio do indicador DeMarker. |
| `UpperLevel` / `LowerLevel` | Limites de sobrecompra/sobrevenda acionando posições curtas/longas. |
| `ReverseSignals` | Troque condições longas e curtas. |
| `StopLossPoints` | Distância inicial de parada de proteção medida em etapas de preço. |
| `TakeProfitPoints` | Distância de lucro medida em etapas de preço. |
| `EnableTrailing` | Ativa o bloco de trailing stop. |
| `TrailingStopPoints` | Distância do trailing stop quando o trailing estiver ativo. |
| `TrailingStepPoints` | Movimento favorável mínimo antes do avanço do trailing stop. |
| `UseTimeFilter` | Restringe a negociação à janela `SessionStart`–`SessionEnd`. |
| `SessionStart` / `SessionEnd` | Limites de sessão inclusivos/exclusivos (suporta wrap-around). |
| `TradeVolume` | Quantidade a enviar com cada ordem de mercado. |
| `CandleType` | Série de velas a serem analisadas (padrão 5 minutos). |

## Notas de implementação

* O especialista MT5 incluiu um limite de “ativação final”. A proteção final padrão de StockSharp não expõe o mesmo parâmetro, portanto, o rastreamento é ativado imediatamente quando `EnableTrailing` é verdadeiro.
* O tratamento de erros para tamanhos de lote inválidos, níveis de congelamento e lógica de atualização de lance/pedido são tratados pela infraestrutura de StockSharp, portanto, são omitidos da conversão.
* O registro em log é executado por meio da classe base `Strategy` (chame `LogInfo/LogError` se for necessário rastreamento adicional).
