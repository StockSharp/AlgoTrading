# Estratégia de modelo simples Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica a ideia original do MetaTrader "Modelo Martingale simples" em StockSharp. Ele analisa velas concluídas de um período configurável usando um par de médias móveis simples (SMA). Um filtro de rompimento verifica se o fechamento da vela anterior rompe a máxima ou a mínima de uma vela ainda anterior para confirmar a direção. O tamanho da posição segue uma sequência de martingale: após cada ciclo perdedor, o próximo volume de negociação é multiplicado, enquanto os ciclos lucrativos redefinem o volume para o tamanho base configurado.

## Lógica de negociação
1. Assine velas do período `CandleType`. Apenas velas finalizadas participam da geração do sinal.
2. Calcule um SMA rápido e um SMA lento no fechamento da vela.
3. Gere um sinal de **compra** quando:
   - o último fechamento da vela concluído está acima do rápido SMA,
   - o rápido SMA está acima do lento SMA,
   - na vela anterior, o rápido SMA estava abaixo do lento SMA, e
   - o último fechamento da vela concluído está acima da máxima da vela há duas barras.
4. Gere um sinal de **venda** quando as condições simétricas ocorrerem no lado negativo, incluindo o fechamento abaixo da mínima da vela há duas barras.
5. Quando um sinal disparar e não houver posições abertas ou ordens ativas, envie uma ordem de mercado usando o volume de martingale atualmente calculado.
6. Anexe níveis sintéticos de stop-loss e take-profit monitorando velas futuras. Quando o preço atingir qualquer nível, feche a posição aberta.
7. Após o fechamento de uma posição e a atualização do saldo da carteira:
   - se o saldo aumentar, redefina o volume para o valor `BaseVolume`;
   - se o saldo diminuiu, multiplique o último volume de negociação por `Multiplier` e alinhe-o com a etapa de volume do instrumento.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `StopLossPoints` | Distância da entrada até a parada de proteção nas faixas de preço. |
| `TakeProfitPoints` | Distância da entrada até a meta de lucro em faixas de preço. |
| `BaseVolume` | Tamanho inicial do lote para o ciclo martingale. |
| `Multiplier` | Fator aplicado ao tamanho do lote anterior após perda. |
| `FastPeriod` | Comprimento do SMA rápido usado para polarização direcional. |
| `SlowPeriod` | Comprimento da lentidão SMA para confirmação de tendência. |
| `CandleType` | Prazo de velas processadas pela estratégia. |

## Gestão de capital
- A escada martingale reage estritamente às mudanças de equilíbrio realizadas. Pequenas flutuações (±0,01 unidades monetárias) são ignoradas para evitar ruído.
- Os volumes são alinhados ao instrumento `VolumeStep`, `MinVolume` e `MaxVolume` para garantir tamanhos de pedido válidos.
- Os níveis de stop-loss e take-profit são monitorados nos extremos das velas (alta/baixa) em vez de colocar ordens de câmbio, refletindo a implementação original do MQL que usava saídas de mercado.

## Notas de uso
- Escolha um período de tempo e uma combinação de símbolos que produza velas históricas suficientes para que ambos os SMAs se formem antes de permitir a negociação.
- Ajuste `StopLossPoints` e `TakeProfitPoints` para corresponder ao tamanho do tick do símbolo; eles representam contagens de pontos, não unidades de preço.
- Considere testar diferentes multiplicadores e volumes básicos para controlar os requisitos de capital porque as sequências de martingale crescem rapidamente.
- A estratégia chama `StartProtection()` no início para integração com os recursos padrão de gerenciamento de risco de StockSharp.
