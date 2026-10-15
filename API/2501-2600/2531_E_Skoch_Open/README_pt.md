# Estratégia E-Skoch-Open (Port do StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **E-Skoch-Open** replica o consultor especialista original do MetaTrader 5 que opera um padrão simples de três velas. A implementação do StockSharp processa velas concluídas, avalia reversões de momentum nos fechamentos recentes e abre uma nova posição quando a configuração necessária aparece. O risco é controlado por offsets de stop-loss/take-profit medidos em pontos ajustados (pips) e um objetivo de crescimento de capital que pode achatar todas as posições abertas. O dimensionamento de posição segue um esquema de martingale: após uma operação perdedora o próximo tamanho de ordem é multiplicado por 1.6, enquanto operações lucrativas reiniciam o volume ao valor inicial.

## Lógica de trading
1. Trabalha com o período definido pelo parâmetro `CandleType` (padrão: 1 hora).
2. Aguarda até que pelo menos três velas concluídas estejam disponíveis.
3. **Configuração de compra**: se `Close[n-3] > Close[n-2]` e `Close[n-1] < Close[n-2]`, e as operações compradas estão habilitadas.
4. **Configuração de venda**: se `Close[n-3] > Close[n-2]` e `Close[n-2] < Close[n-1]`, e as operações vendidas estão habilitadas.
5. Se `CloseOnOppositeSignal` estiver habilitado, receber um sinal oposto fecha a posição existente imediatamente e ignora novas entradas para a barra atual.
6. Para cada nova posição, a estratégia anexa níveis estáticos de stop-loss e take-profit calculados a partir do fechamento atual e a distância configurada em pontos ajustados. Quando a máxima/mínima de uma vela concluída atinge um desses níveis, a posição é fechada.
7. A estratégia verifica continuamente o capital da conta. Quando o crescimento do capital relativo ao último momento plano excede `TargetProfitPercent`, todas as posições são fechadas.
8. Após uma operação fechar com perda, o próximo volume de ordem é multiplicado por 1.6. Após uma operação lucrativa, o volume retorna ao tamanho inicial. Os volumes são normalizados usando as restrições do instrumento (`VolumeStep`, `VolumeMin`, `VolumeMax`).

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período usado para detecção de padrões. Funciona com qualquer vela suportada pelo StockSharp. |
| `InitialOrderVolume` | Tamanho de lote base para a primeira operação em uma sequência (padrão: 0.01). |
| `StopLossPoints` | Distância do stop-loss expressa em pontos ajustados. Para instrumentos de 5 ou 3 dígitos o valor do ponto é `PriceStep * 10`, caso contrário `PriceStep`. |
| `TakeProfitPoints` | Distância do take-profit usando a mesma convenção de ponto ajustado. |
| `EnableBuySignals` / `EnableSellSignals` | Ativar ou desativar entradas compradas ou vendidas. |
| `MaxBuyTrades` / `MaxSellTrades` | Número máximo de operações consecutivas permitidas por direção (`-1` remove o limite). O port mantém no máximo uma posição por direção por padrão. |
| `TargetProfitPercent` | Ganho percentual do capital que desencadeia o fechamento de todas as posições (padrão: 1.2%). |
| `CloseOnOppositeSignal` | Se habilitado, um sinal na direção oposta força uma posição plana antes de considerar novas operações. |

## Notas de gestão de risco
- Os níveis de stop-loss e take-profit são simulados a partir dos extremos das velas. No trading ao vivo, a execução intrabar pode diferir do MetaTrader onde ordens protetoras são registradas no servidor.
- O multiplicador de martingale (1.6) pode fazer os volumes crescerem rapidamente durante drawdowns. Garantir que os limites do instrumento (`VolumeMax`) e o capital do portfólio possam suportar a maior posição esperada.
- O bloqueio de lucros baseado em capital funciona apenas quando as informações do portfólio estão disponíveis via `Portfolio.CurrentValue`.

## Dicas de uso
- Ajustar `CandleType` para corresponder ao período usado no consultor especialista original.
- Ajustar `StopLossPoints` / `TakeProfitPoints` à volatilidade do instrumento; são baseados em pips graças ao cálculo do ponto ajustado.
- Desabilitar uma direção se o hedging não for permitido pelo corretor ou política de risco.
- Ficar atento ao objetivo de capital e às configurações de martingale ao executar testes longos para evitar liquidações inesperadas.
