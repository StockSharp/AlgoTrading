# Estratégia SVOS EURJPY D1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão C# do consultor especialista MetaTrader 4 **SVOS_EURJPY_D1**. Opera em velas diárias para EURJPY e
combina um classificador de regime com reconhecimento de padrões e filtros de indicadores. O Filtro Horizontal Vertical (VHF) distingue
entre tendências e estados de mercado variados. Quando o mercado está em tendência, a estratégia depende da inclinação do histograma MACD (OSMA),
enquanto em condições de variação ele volta para o oscilador Stochastic. Padrões de velas, como barras envolventes e
as estrelas da manhã/noite são usadas para fechar posições agressivamente contra ações desfavoráveis dos preços.

## Lógica de negociação
- **Detecção de regime** – o valor VHF do dia anterior é comparado com `VhfThreshold`. Valores acima do limite ativam o
bloco de acompanhamento de tendência, caso contrário, o bloco de intervalo será usado.
- **Confirmação de tendência** – dois EMAs (5 e 20 períodos) são comparados com um EMA lenta (130 períodos, correspondendo ao filtro de seis meses de
o original EA) para dimensionar os tamanhos das posições. Nas tendências de alta, o volume de compra é multiplicado por `RiskBoost`; em tendências de baixa, o volume de vendas é
multiplicado.
- **Filtros indicadores**:
  - Regime de tendência: opere comprado quando o OSMA for positivo e ascendente (`OSMA[1] > 0` e `OSMA[1] > OSMA[2]`). Opere vendido quando OSMA for negativo
e caindo.
  - Regime de alcance: opere comprado quando a linha principal Stochastic cruzar acima de seu sinal, opere vendido quando cruzar abaixo.
  - Guarda de volatilidade: o desvio padrão anterior deve exceder `StdDevMinimum` antes que qualquer sinal seja aceito.
- **Filtros de ação de preço** – a vela concluída mais recentemente não deve formar um doji (proporção `DojiDivisor`) e deve confirmar o
direção (alta para posições compradas, baixa para posições vendidas). Engolfo oposto ou padrões estelares desencadeiam a liquidação imediata do
respectivo lado.
- **Limites de posição** – o número total de ordens abertas é limitado por `MaxTrendOrders` em mercados de tendência e por `MaxRangeOrders`
em mercados variados.
- **Gerenciamento de risco** – cada pedido possui níveis fixos de stop-loss e take-profit (`StopLossPips`, `TakeProfitPips`). Um rastro
stop é ativado quando o lucro flutuante excede `TrailingStopPips`; é recalculado usando os extremos da vela para imitar o
Comportamento MetaTrader.

## Uso do indicador
- **Média Móvel Exponencial (5, 20, 130)** – usada para confirmação de direção e escala de volume.
- **Filtro Horizontal Vertical** – indicador personalizado que mede a relação entre o movimento líquido e o fechamento acumulado
mudanças para detectar tendências versus intervalos.
- **MACD (OSMA)** – a diferença entre MACD e sua linha de sinal impulsiona entradas e saídas de tendência.
- **Stochastic Oscilador** – Os valores %K e %D fornecem sinais de reversão à média para mercados variados.
- **Desvio Padrão** – garante que a volatilidade seja alta o suficiente antes de permitir novas negociações.

## Gerenciamento de pedidos
- As ordens são executadas com `BuyMarket`/`SellMarket` e armazenadas internamente para que paradas e metas individuais possam ser simuladas em
Ambiente de rede de StockSharp.
- Quando os níveis de stop-loss ou take-profit são atingidos dentro da faixa da vela, a parte correspondente da posição é fechada.
- O trailing stop segue a alta da vela (para posições compradas) ou a mínima (para posições vendidas), mantendo a distância configurada.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `LotSize` | Tamanho base do pedido expresso em lotes. | `0.1` |
| `RiskBoost` | Multiplicador aplicado ao tamanho do lote quando o filtro de tendência EMA está alinhado. | `3` |
| `TakeProfitPips` | Distância de lucro em pips. | `350` |
| `StopLossPips` | Distância de stop-loss em pips. | `90` |
| `TrailingStopPips` | Distância do trailing-stop em pips (sempre ativo). | `150` |
| `StochKPeriod` | Comprimento %K do oscilador Stochastic. | `8` |
| `StochDPeriod` | Comprimento %D do oscilador Stochastic. | `3` |
| `StochSlowing` | Fator de suavização aplicado a %K. | `3` |
| `StdDevPeriod` | Janela de lookback para o filtro de desvio padrão. | `20` |
| `StdDevMinimum` | Desvio padrão mínimo necessário antes que novas negociações possam ser abertas. | `0.3` |
| `VhfPeriod` | Comprimento do filtro horizontal vertical. | `20` |
| `VhfThreshold` | Limiar do regime; valores mais altos denotam mercados em tendência. | `0.4` |
| `MaxTrendOrders` | Número máximo de pedidos abertos simultaneamente durante tendências. | `4` |
| `MaxRangeOrders` | Número máximo de ordens abertas simultaneamente durante os intervalos. | `2` |
| `MacdFastLength` | Comprimento EMA rápido dentro de MACD. | `10` |
| `MacdSlowLength` | Comprimento EMA lento dentro de MACD. | `25` |
| `MacdSignalLength` | Comprimento do sinal EMA para MACD. | `5` |
| `DojiDivisor` | Proporção usada para sinalizar velas doji (corpo menor que intervalo/divisor). | `8.5` |
| `CandleType` | Tipo de vela usado para análise (diariamente por padrão). | `1 day` |
| `PipSizeOverride` | Substituição opcional do tamanho do pip; `0` permite a detecção automática de `Security.PriceStep`. | `0` |

## Notas de implementação
- O EA original referia-se a um EMA de seis meses a partir de um período mensal. A porta calcula um período de 130 EMA em fechamentos diários para
reproduza a mesma suavização enquanto mantém uma única assinatura de dados.
- Stops, alvos e lógica de trilha são reproduzidos dentro da estratégia porque StockSharp nets posições por padrão. Cada entrada é
rastreado individualmente para respeitar o comportamento MetaTrader.
- As atualizações de trailing stop usam máximos/mínimos de velas para aproximar os movimentos de preços intradiários. Os resultados podem diferir ligeiramente dos baseados em ticks
seguindo em MetaTrader quando ocorrem grandes reversões intradiárias.
- O tamanho do pip é calculado a partir de `Security.PriceStep`; use `PipSizeOverride` se a corretora usar uma etapa não padrão para pares JPY.

## Uso
1. Anexe a estratégia aos dados diários EURJPY ou atualize `CandleType` se desejar outro período.
2. Verifique se o tamanho do pip foi detectado corretamente; ajuste `PipSizeOverride` se necessário.
3. Configure parâmetros de gerenciamento de dinheiro (`LotSize`, `RiskBoost`) para corresponder às restrições da conta.
4. Execute a estratégia no StockSharp Designer ou API Runner para validar o comportamento antes de negociar ao vivo.
