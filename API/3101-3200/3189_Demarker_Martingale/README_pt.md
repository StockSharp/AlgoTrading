# Estratégia Demarker Martingale (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Demarker Martingale** recria o expert advisor do MetaTrader "Demarker Martingale" usando a API de alto nível do StockSharp. O sistema combina um sinal do oscilador DeMarker de médio prazo com um filtro de tendência MACD de período maior. As entradas são seguidas por dimensionamento de posição estilo martingale, níveis fixos de stop-loss e take-profit, proteção de break-even e um trailing stop que imita o conjunto de ferramentas de gestão monetária do expert original.

## Lógica de negociação principal
1. **Feeds de dados** – a estratégia assina um período de negociação definido pelo usuário (padrão de velas de 15 minutos) para geração de sinais e uma série de período maior (padrão de velas mensais) para calcular o filtro MACD.
2. **Gatilho DeMarker** – quando o valor do DeMarker excede o limite neutro `DemarkerThreshold` (padrão 0.5) e a ação de preço recente forma uma sobreposição altista (`Low[2] < High[1]`), uma configuração longa é considerada. Por outro lado, uma sobreposição baixista com DeMarker abaixo do limiar prepara um curto.
3. **Confirmação MACD** – o MACD de período maior deve concordar com a direção. Um sinal altista requer que a linha principal do MACD esteja acima de sua linha de sinal, enquanto um sinal baixista espera a relação oposta. Isso reproduz o filtro MACD mensal do expert MQL.
4. **Execução de ordens** – sinais válidos colocam ordens de mercado com o volume ajustado pelo martingale atual. Apenas uma posição direcional é mantida por vez.
5. **Monitoramento de posição** – enquanto uma posição está aberta, a estratégia avalia cada vela concluída para detectar gatilhos de stop-loss, take-profit, break-even ou trailing stop. Eventos de violação fecham a posição completa via ordens de mercado.

## Gestão monetária
- **Dimensionamento inicial** – as ordens começam com `InitialVolume` alinhado ao `VolumeStep` do instrumento e limitado por `VolumeMin`/`VolumeMax`.
- **Escalada martingale** – após um trade perdedor, o próximo volume é multiplicado por `MartingaleMultiplier` (`DoubleLotSize = true`) ou incrementado por `LotIncrement`. Trades lucrativos resetam a escada para o volume base. A profundidade de escalada é limitada por `MaxMartingaleSteps` para evitar exposição descontrolada.
- **Stop-loss e take-profit** – as distâncias são expressas em pips no estilo MetaTrader. O tamanho do pip adapta-se automaticamente às cotações Forex de 3/5 dígitos, correspondendo à lógica `ticksize` original.
- **Break-even** – uma vez que o lucro não realizado atinge `BreakEvenTriggerPips`, o stop-loss é deslocado para a entrada mais `BreakEvenOffsetPips` (longo) ou menos o offset (curto).
- **Trailing stop** – lucros além de `TrailingStopPips` movem um limite de trailing interno que se aperta a cada vela, replicando o comportamento `TrailingStop` do EA.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período de negociação usado para sinais DeMarker. |
| `MacdCandleType` | Período maior usado para calcular o filtro de tendência MACD. |
| `DemarkerPeriod` | Período de lookback do DeMarker. |
| `DemarkerThreshold` | Limite neutro entre configurações altistas e baixistas. |
| `MacdFast` / `MacdSlow` / `MacdSignal` | Comprimentos EMA do MACD. |
| `InitialVolume` | Tamanho de ordem base antes dos ajustes martingale. |
| `MartingaleMultiplier` | Fator de multiplicação quando `DoubleLotSize` está habilitado. |
| `LotIncrement` | Incremento aditivo quando a duplicação está desabilitada. |
| `DoubleLotSize` | Alternar entre martingale multiplicativo e aditivo. |
| `MaxMartingaleSteps` | Número máximo de escaladas consecutivas. |
| `StopLossPips` | Distância de stop-loss em pips. |
| `TakeProfitPips` | Distância de take-profit em pips. |
| `TrailingStopPips` | Distância de trailing stop em pips. |
| `UseBreakEven` | Habilitar ou desabilitar a lógica de break-even. |
| `BreakEvenTriggerPips` | Limiar de lucro (em pips) antes de mudar para break-even. |
| `BreakEvenOffsetPips` | Buffer aplicado ao stop de break-even. |

## Notas de conversão
- A conversão de pip espelha o EA MQL (`ticksize == 0.00001` ou `0.001` implica uma escala de pip 10x). Isso preserva distâncias de risco consistentes em cotações de 3/5 dígitos.
- O filtro de tendência MACD usa `MovingAverageConvergenceDivergenceSignal` com os comprimentos EMA originais e processa uma série de velas separada para emular a lógica do gráfico mensal.
- A contabilidade do martingale rastreia preços médios ponderados de entrada e PnL realizado para decidir se o próximo trade deve escalar ou resetar.
- Todas as ações protetoras (stop-loss, take-profit, break-even, trailing) são executadas via saídas de mercado porque a API de alto nível desencoraja modificações diretas de ordens sob a guarda `StartProtection`.

## Dicas de uso
- Certifique-se de que o instrumento atribuído expõe `PriceStep`, `VolumeStep`, `VolumeMin` e `VolumeMax` para alinhar os cálculos de pip e o arredondamento de volume com as restrições da bolsa.
- Experimente com `MacdCandleType` (por exemplo, velas semanais) para ajustar o filtro de tendência para mercados mais rápidos.
- Ao otimizar, ajuste conjuntamente `DemarkerThreshold`, `TrailingStopPips` e os parâmetros martingale para manter os drawdowns sob controle.
- Combine a estratégia com controles de risco no nível do portfólio ou filtros de sessão de negociação ao implantar ao vivo, já que as sequências martingale inerentemente aumentam a exposição após perdas.
