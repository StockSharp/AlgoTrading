# Estratégia Lbs V12
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Lbs V12 é uma conversão do consultor especialista MetaTrader **LBS_V12.mq4**. Ele abre um par de ordens de stop breakout em torno da vela anterior de 15 minutos quando a hora de disparo configurada começa. Ambas as ordens são compensadas pelo valor atual do Average True Range (ATR) para levar em conta a volatilidade de curto prazo. A estratégia tenta capturar o primeiro impulso da sessão de negociação e gerencia as saídas por meio de regras virtuais de stop-loss, take-profit e trailing avaliadas em cada vela finalizada.

## Lógica de negociação
1. A estratégia monitora as velas finalizadas do período selecionado (15 minutos por padrão).
2. Quando uma nova vela com minuto `00` aparece no `TriggerHour` configurado, a vela anterior se torna o intervalo de referência.
3. Se não houver posições abertas nem ordens de trabalho para o dia atual, serão enviadas duas ordens de stop:
   - **Parada de compra** acima da máxima de referência mais o spread do instrumento, uma etapa de preço e o valor mais recente de ATR.
   - **Sell stop** abaixo do mínimo de referência menos os mesmos buffers.
4. Os níveis de preços de proteção para cada lado são armazenados internamente:
   - O stop loss é colocado além do extremo oposto da vela de referência.
   - O take-profit é calculado usando a distância de pontos no estilo MetaTrader.
   - Um trailing stop é ativado quando a negociação se move além da distância configurada.
5. Quando uma posição longa ou curta é aberta, a ordem stop oposta é cancelada. Toda a proteção é aplicada virtualmente: os máximos e mínimos das velas são comparados com os valores de stop/take armazenados e a posição é fechada com ordens de mercado quando os limites são atingidos.
6. A estratégia é executada apenas uma vez por dia. Todas as ordens pendentes e estados internos são eliminados no início de uma nova data de negociação.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-------------|---------|
| `Volume` | Volume de negociação em lotes. | `1` |
| `TriggerHour` | Hora do dia (fuso horário terminal) em que as ordens de breakout devem ser enviadas. | `9` |
| `TakeProfitPoints` | Pontos estilo MetaTrader entre o preço de entrada e a meta de lucro. | `100` |
| `TrailingStopPoints` | Pontos estilo MetaTrader usados para o trailing stop depois que a negociação passa para o lucro. | `20` |
| `AtrPeriod` | Período do indicador ATR que compensa as ordens pendentes. | `3` |
| `CandleType` | Tipo de vela usado para cálculos de sinal. O padrão são velas de período de 15 minutos. | `15m timeframe` |

## Gestão de risco
- As saídas são executadas por meio de ordens de mercado quando os extremos das velas tocam os níveis virtuais de stop-loss ou take-profit.
- O trailing stop aumenta (para posições compradas) ou diminui (para posições vendidas) o nível de proteção sempre que a negociação ganha mais que a distância configurada.
- A reinicialização diária garante que a estratégia não acumule múltiplas posições ou ordens pendentes desatualizadas.

## Notas
- Atualizações precisas de compra/venda melhoram a compensação de spread que é adicionada aos preços de breakout. Se os dados de spread não estiverem disponíveis, a estratégia volta para uma etapa de preço.
- A conversão mantém os padrões originais MetaTrader, mas adapta o tratamento do take-profit para posições curtas para que o alvo seja sempre colocado na direção lucrativa.
