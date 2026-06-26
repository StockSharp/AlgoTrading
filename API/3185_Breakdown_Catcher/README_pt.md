# Estratégia Breakdown Catcher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Breakdown Catcher é um sistema de rompimento barra a barra portado do expert advisor do MetaTrader "Breakdown catcher". Após cada vela concluída, a estratégia coloca níveis virtuais de rompimento acima do máximo anterior e abaixo do mínimo anterior (opcionalmente deslocados por um recuo). Quando a próxima vela perfura um desses níveis, a estratégia entra em uma posição na direção do rompimento e atribui imediatamente stop-loss, take-profit e proteção de trailing opcional expressos em pips.

## Lógica de negociação
1. No fechamento de cada vela, o máximo e o mínimo da barra concluída tornam-se o intervalo de referência para o próximo período.
2. Nível de rompimento de compra = máximo anterior + recuo (em pips). Nível de rompimento de venda = mínimo anterior − recuo.
3. Se a vela atual romper o nível de compra enquanto nenhuma posição está aberta, a estratégia abre uma posição longa a mercado, remove qualquer contexto curto e armazena os níveis protetores.
4. Se a vela atual romper o nível de venda enquanto flat, a estratégia abre uma posição curta a mercado.
5. As distâncias de stop-loss e take-profit são convertidas de pips para preços absolutos usando o passo de preço do instrumento e o ajuste clássico do MetaTrader para instrumentos de 3/5 decimais.
6. Um trailing stop pode ajustar o preço protetor após o trade se mover a favor em pelo menos `TrailingStop + TrailingStep` pips. O passo de trailing imita a lógica do MetaTrader onde o stop só se move após um movimento adicional suficiente.
7. Se ambos os níveis de rompimento forem atingidos dentro da mesma vela, a estratégia ignora a negociação nessa barra para evitar ordem de execução ambígua.
8. Um filtro de spread bloqueia novas entradas sempre que o spread bid-ask atual exceder os `AllowedSpreadPoints` configurados.

## Gestão monetária
* A estratégia usa o `Strategy.Volume` base para o tamanho da ordem. Ao reverter posições, o volume é aumentado pelo valor absoluto da posição atual para garantir uma inversão completa.
* Stop-loss, take-profit e trailing stops são gerenciados internamente emitindo ordens de saída a mercado quando os intervalos de preço incluem os níveis protetores.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `StopLossPips` | Distância de stop-loss em pips. | `30` |
| `TakeProfitPips` | Distância de take-profit em pips. | `90` |
| `TrailingStopPips` | Distância de trailing stop em pips. Defina como `0` para desativar o trailing. | `30` |
| `TrailingStepPips` | Progresso adicional necessário antes de o trailing stop se mover. Deve ser positivo quando o trailing estiver habilitado. | `5` |
| `IndentPips` | Deslocamento extra aplicado aos níveis de rompimento. | `0` |
| `AllowedSpreadPoints` | Spread máximo medido em pontos brutos (unidades `PriceStep`). | `5` |
| `CandleType` | Série de velas usada para detecção de rompimento. | `período de 1h` |

## Notas e limitações
* A conversão de pips segue o mesmo ajuste de dígitos do EA original: se o instrumento tiver 3 ou 5 casas decimais, um pip equivale a dez passos de preço.
* Como a API de alto nível do StockSharp trabalha com eventos de velas, a ordem exata em que ambos os níveis de rompimento são atingidos dentro de uma única vela não pode ser determinada; portanto, a estratégia ignora essas barras.
* As ordens protetoras são modeladas com saídas a mercado, garantindo que a estratégia seja autocontida sem depender de ordens de stop do corretor.
