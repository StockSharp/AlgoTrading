# Estratégia Sidus v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Sidus v1 é uma estratégia de acompanhamento de tendências que combina dois conjuntos de médias móveis exponenciais (EMAs) com filtros de índice de força relativa (RSI). O consultor especialista MetaTrader 4 original abre uma posição quando um EMA rápido diverge de um EMA mais lento e o RSI confirma condições de sobrevenda ou sobrecompra. Esta porta StockSharp mantém a lógica central, limitando as negociações a velas com baixo volume e anexando ordens de proteção assimétricas para posições longas e curtas.

## Indicadores usados
- **Fast EMA (perna de compra)** – mede o impulso de curto prazo para entradas longas.
- **EMA lenta (perna de compra)** – representa o filtro de tendência de longo prazo para entradas longas.
- **Fast EMA (perna de venda)** – mede o impulso de curto prazo para entradas curtas.
- **EMA lenta (perna de venda)** – representa o filtro de tendência de longo prazo para entradas curtas.
- **RSI (perna de compra)** – valida condições de sobrevenda para negociações longas.
- **RSI (perna de venda)** – valida condições de sobrecompra para negociações curtas.

## Lógica de negociação
1. Assine a série de velas configurada (período padrão de 15 minutos).
2. Calcule todos os indicadores EMA e RSI em cada vela finalizada.
3. Ignora a avaliação do sinal quando o volume da vela excede o limite configurado (padrão 10).
4. **Condição de compra**:
   - EMA rápida menos EMA lenta está abaixo do limite de compra.
   - O valor de RSI está abaixo do limite de compra de RSI.
   - Nenhuma exposição longa existente (a posição líquida deve ser não positiva).
5. **Condição de venda**:
   - EMA rápida (perna de venda) menos EMA lenta (perna de venda) está acima do limite de venda.
   - RSI (perna de venda) está acima do limite de venda RSI.
   - Nenhuma exposição curta existente (a posição líquida deve ser não negativa).
6. Quando um sinal for acionado, cancele quaisquer ordens de proteção pendentes, execute uma ordem de mercado dimensionada para virar a posição líquida para o lado desejado e coloque imediatamente ordens de take-profit e stop-loss adaptadas à direção da posição.

## Gestão de risco
- As negociações longas colocam um take-profit em `entry + BuyTakeProfitPips * priceStep` e um stop-loss em `entry - BuyStopLossPips * priceStep`.
- As negociações curtas apresentam um take-profit em `entry - SellTakeProfitPips * priceStep` e um stop loss em `entry + SellStopLossPips * priceStep`.
- As ordens de proteção reutilizam a etapa atual do preço do título; altere os parâmetros do pip para se adaptar a instrumentos com diferentes tamanhos de tick.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `FastEmaLength` | Duração do EMA rápido para sinais de compra. | 23 |
| `SlowEmaLength` | Duração da lentidão EMA para sinais de compra. | 62 |
| `FastEma2Length` | Duração do EMA rápido para sinais de venda. | 18 |
| `SlowEma2Length` | Duração da lentidão EMA para sinais de venda. | 54 |
| `RsiPeriod` | RSI período para confirmação de compra. | 67 |
| `RsiPeriod2` | RSI período para confirmação da venda. | 97 |
| `BuyDifferenceThreshold` | Diferença máxima rápida-lenta EMA para permitir compras. | 63 |
| `BuyRsiThreshold` | Nível máximo de RSI para permitir compras. | 59 |
| `SellDifferenceThreshold` | Diferença mínima rápida-lenta EMA para permitir vendas. | -57 |
| `SellRsiThreshold` | Nível mínimo de RSI para permitir vendas. | 60 |
| `BuyTakeProfitPips` | Distância de lucro (pips) para negociações longas. | 95 |
| `BuyStopLossPips` | Distância de stop-loss (pips) para negociações longas. | 100 |
| `SellTakeProfitPips` | Distância de lucro (pips) para negociações curtas. | 17 |
| `SellStopLossPips` | Distância de stop-loss (pips) para negociações curtas. | 69 |
| `OrderVolume` | Volume para posições recém-abertas. | 0,5 |
| `MaxCandleVolume` | Volume máximo de velas permitido para negociação. | 10 |
| `CandleType` | Período usado para cálculos. | Velas de 15 minutos |

## Notas de uso
- Garanta que a segurança conectada suporte ordens simultâneas de mercado, stop e limite para um gerenciamento de risco adequado.
- Ajuste as configurações do pip para refletir o tamanho do tick do instrumento se ele for diferente do valor do ponto MT4 assumido pelo especialista original.
- A estratégia opera em posições líquidas; irá nivelar a exposição oposta antes de estabelecer uma nova negociação na direção oposta.
