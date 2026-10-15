# Estratégia de Hedge HTH Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma conversão direta do expert advisor MetaTrader "HTH Trader". Opera uma cesta forex de quatro pernas e tenta capturar a reversão à média diária entre EURUSD e uma cesta espelhada de USDCHF, GBPUSD e AUDUSD. O port para StockSharp mantém os controles de risco originais e as regras de tempo, usando a API de alto nível para negociação multi-ativo.

Características principais:

- Abre uma cesta hedgeada uma vez por dia entre 00:05 e 00:12 horário do terminal.
- Usa os dois fechamentos diários anteriores do EURUSD para decidir a direção da cesta.
- Gerencia quatro instrumentos simultaneamente: EURUSD (ativo principal), USDCHF, GBPUSD e AUDUSD.
- Rastreia o lucro aberto em pips e suporta alvos de lucro e perda para toda a cesta.
- Inclui uma função de duplicação de emergência que adiciona às pernas lucrativas quando o drawdown da cesta ultrapassa um limiar.
- Fecha todas as operações às 23:00 horário do terminal ou quando a cesta atinge os limites configurados de lucro/perda.

## Requisitos de dados

- **Candles intradiários**: Todos os quatro símbolos devem entregar candles intradiários para o período configurado em `IntradayCandleType` (padrão 5 minutos). Esses candles fornecem o preço mais recente e o relógio de sessão.
- **Candles diários**: Cada símbolo deve fornecer candles diários para que a estratégia possa monitorar os dois últimos fechamentos diários completos.

## Lógica de negociação

1. Ao final de cada candle intradiário finalizado, a estratégia verifica o lucro aberto atual:
   - Se `AllowEmergencyTrading` estiver habilitado e o lucro aberto total ≤ `-EmergencyLossPips`, a estratégia duplica cada perna que estiver atualmente lucrativa e desabilita mais operações de emergência para aquele dia.
   - Se `UseProfitTarget` estiver habilitado e o lucro aberto total ≥ `ProfitTargetPips`, a cesta é fechada imediatamente.
   - Se `UseLossLimit` estiver habilitado e o lucro aberto total ≤ `-LossLimitPips`, a cesta é fechada imediatamente.
2. Quando o relógio chega às 23:00, a cesta é fechada independentemente do lucro.
3. Quando não há posições abertas e o relógio está dentro da janela 00:05–00:12, a estratégia verifica os dois últimos fechamentos diários completos do símbolo primário (EURUSD por padrão):
   - Se a variação percentual dia a dia for **positiva**, a estratégia abre: comprado EURUSD, comprado USDCHF, vendido GBPUSD, comprado AUDUSD.
   - Se a variação for **negativa**, abre: vendido EURUSD, vendido USDCHF, comprado GBPUSD, vendido AUDUSD.
   - Se a variação for zero ou qualquer fechamento diário estiver faltando, a estratégia pula a negociação para aquele dia.
4. Todas as posições são fechadas usando ordens a mercado via `ClosePosition`.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeEnabled` | Habilita ou desabilita a colocação de ordens. | `true` |
| `ShowProfitInfo` | Registra o lucro da cesta em pips a cada atualização enquanto há posições abertas. | `true` |
| `UseProfitTarget` | Habilita o fechamento automático quando `ProfitTargetPips` é atingido. | `false` |
| `UseLossLimit` | Habilita o fechamento automático quando `LossLimitPips` é atingido. | `false` |
| `AllowEmergencyTrading` | Permite a função de duplicação de emergência. | `true` |
| `EmergencyLossPips` | Drawdown da cesta (em pips) que aciona a duplicação de emergência. | `60` |
| `ProfitTargetPips` | Lucro da cesta (em pips) que aciona o fechamento quando `UseProfitTarget` está habilitado. | `80` |
| `LossLimitPips` | Perda da cesta (em pips) que aciona o fechamento quando `UseLossLimit` está habilitado. | `40` |
| `TradingVolume` | Volume da ordem para cada perna. | `0.01` |
| `Symbol2` | Segundo ativo (USDCHF por padrão). | `null` |
| `Symbol3` | Terceiro ativo (GBPUSD por padrão). | `null` |
| `Symbol4` | Quarto ativo (AUDUSD por padrão). | `null` |
| `IntradayCandleType` | Período intradiário usado para agendamento e atualizações de preço. | Candles de `5` minutos |

## Notas de uso

- Atribua o ativo principal (`Strategy.Security`) ao EURUSD (ou ao par líder desejado) e mapeie `Symbol2`, `Symbol3`, `Symbol4` para os instrumentos correlacionados antes de iniciar.
- Certifique-se de que cada ativo tenha um `PriceStep` válido; caso contrário, os cálculos de lucro em pips não podem ser realizados e a lógica de emergência permanecerá inativa.
- A função de duplicação de emergência só adiciona às pernas que estão atualmente lucrativas; pernas perdedoras são deixadas intactas para evitar amplificar o drawdown.
- A implementação assume que ordens a mercado são executadas próximo ao último fechamento de candle. Para contabilização precisa, conecte a estratégia a um feed de dados que entregue candles intradiários oportunos.
- Como a lógica é impulsionada por uma única barra por minuto (ou período escolhido), o comportamento tick a tick original do MQL pode diferir ligeiramente no tempo de execução, mas o sequenciamento e as condições de operação correspondem ao expert advisor de referência.
