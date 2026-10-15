# Estratégia de intervalo de sessão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Session Breakout replica o consultor especialista MetaTrader "Session Breakout". Ele assiste a sessão matinal européia
e mede sua faixa de preço. Quando esse intervalo é suficientemente apertado, a estratégia prepara-se para romper as negociações durante os EUA.
sessão da tarde usando StockSharp de alto nível API. A implementação impõe no máximo uma entrada longa e uma curta por dia por
O nd anexa automaticamente ordens de proteção (stop loss e takeprofit) a cada posição.

## Lógica de negociação
- Redefina o estado no início de cada dia de negociação e pule os fins de semana. As segundas-feiras são opcionais e controladas por um parâmetro.
- Acompanhe as velas finalizadas durante a sessão europeia (padrão 06h00-12h00) e registre a máxima mais alta e a mínima mais baixa.
- No início da sessão dos EUA, o intervalo capturado é classificado como "pequeno" quando sua largura é menor que `SmallSessionThreshol
dPips`.
- Se o intervalo for pequeno, monitore as velas da sessão dos EUA (padrão 12h00-16h00) e espere até que pelo menos uma barra dos EUA feche (`Eu
cordaSessionStartHour + 5` to `EuropeSessionStartHour + 10`).
- Um rompimento longo é acionado quando toda a vela permanece acima da máxima europeia, além de um buffer configurável (`BreakoutBuffer
Pips`). Um pequeno rompimento exige que a vela fique abaixo da mínima europeia menos o buffer.
- Depois de entrar em uma posição, a estratégia atribui níveis de stop-loss e take-profit expressos em pips e evita ganhos adicionais.
tenta na mesma direção pelo resto do dia.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `Volume` | Volume de pedidos usado para breakouts longos e curtos. |
| `EuropeSessionStartHour` | Hora em que começa o rastreamento de alcance europeu. |
| `EuropeSessionEndHour` | Hora em que o rastreamento de alcance europeu para. |
| `UsSessionStartHour` | Hora que marca o início da janela da sessão dos EUA. |
| `UsSessionEndHour` | Hora que marca o fim da janela da sessão dos EUA. |
| `SmallSessionThresholdPips` | Largura máxima (em pips) para a faixa europeia ser qualificada como squeeze. |
| `BreakoutBufferPips` | Buffer extra adicionado acima/abaixo do intervalo antes de acionar rompimentos. |
| `TradeOnMonday` | Permite negociação às segundas-feiras. Fins de semana são sempre ignorados. |
| `TakeProfitPips` | Distância entre o preço de entrada e o nível de lucro. |
| `StopLossPips` | Distância entre o preço de entrada e o nível de stop loss. |
| `CandleType` | Série de velas usada para todos os cálculos (velas de 15 minutos por padrão). |

## Notas
- O tamanho do pip é derivado do instrumento `PriceStep`. Ajuste os parâmetros baseados em pip para corresponder à especificação do contrato
s do título selecionado.
- Como os pedidos são gerados quando uma vela qualificada fecha, os preenchimentos acontecem ao preço de fechamento dessa vela nos backtests. Liv
Os preenchimentos podem variar dependendo das condições do mercado.
- Apenas uma negociação longa e uma curta podem ser abertas por dia. A lógica reflete o comportamento original do consultor especialista ao usar S
Ajudantes de gerenciamento de risco baseado em posição do tockSharp.
