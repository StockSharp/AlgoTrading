# Exp RJTX Correspondências Suavizadas Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia recria o comportamento do assessor especialista MetaTrader 5 `Exp_RJTX_Matches_Smoothed_Duplex.mq5`. Dois blocos de sinal RJTX independentes analisam preços de abertura e fechamento suavizados em seus respectivos períodos de tempo. Cada bloco classifica cada vela completada como altista ou baixista, dependendo se o fechamento suavizado sobe acima da abertura suavizada de `Period` barras atrás. "Correspondências" altistas acionam entradas para o módulo comprado, enquanto correspondências baixistas gerenciam o módulo vendido.

## Geração de sinais
1. **Suavização** – ambos os blocos alimentam aberturas e fechamentos de velas no algoritmo de suavização selecionado. O mesmo método é aplicado aos fluxos de abertura e fechamento, mas instâncias separadas são usadas para manter os buffers internos independentes.
2. **Comparação** – assim que houver histórico suficiente disponível, o fechamento suavizado atual é comparado com a abertura suavizada registrada `Period` barras antes.
3. **Detecção de correspondência** – se o fechamento for maior, a vela recebe uma correspondência altista; caso contrário, torna-se baixista. Os sinais são avaliados após o deslocamento de `SignalBar` velas fechadas, assim como o acesso ao buffer MT5.

## Gestão de posições
- O **bloco comprado** abre uma posição comprada (cobrindo qualquer vendido existente se permitido) quando uma correspondência altista alcança a janela de avaliação. Uma correspondência baixista fecha a posição comprada se as saídas compradas estiverem habilitadas.
- O **bloco vendido** espelha essa lógica: uma correspondência baixista abre uma operação vendida (fechando a exposição comprada se permitido) e uma correspondência altista cobre o vendido.
- As estratégias do StockSharp são neteadas. Portanto, módulos opostos fecham a posição atual antes de abrir uma nova, em vez de manter duas posições hedgeadas independentes como a versão MT5. Desative o parâmetro `Allow ... Close` correspondente para proibir a cobertura automática.

## Gestão de risco
Stops e metas de lucro são expressos em passos de preço (`PriceStep × points`). Para cada vela terminada, a estratégia verifica se o intervalo da barra toca o nível ativo de stop-loss ou take-profit e fecha a posição correspondente imediatamente. Isso emula o comportamento das ordens de proteção MT5 sem depender de ordens gerenciadas pelo broker.

## Parâmetros
| Seção | Parâmetro | Padrão | Descrição |
| --- | --- | --- | --- |
| Long | `LongCandleType` | H4 | Período usado pelo bloco RJTX comprado. |
| Long | `LongVolume` | 0.1 | Volume aberto quando um sinal comprado é executado. |
| Long | `LongAllowOpen` | `true` | Habilitar abertura de posições compradas. |
| Long | `LongAllowClose` | `true` | Habilitar fechamento de posições compradas em correspondências baixistas. |
| Long | `LongStopLossPoints` | 1000 | Distância de stop-loss para operações compradas em passos de preço (0 desabilita a verificação). |
| Long | `LongTakeProfitPoints` | 2000 | Distância de take-profit para operações compradas em passos de preço (0 desabilita a verificação). |
| Long | `LongSignalBar` | 1 | Deslocamento aplicado ao ler buffers RJTX (`0` = vela fechada atual). |
| Long | `LongPeriod` | 10 | Número de barras entre o fechamento suavizado atual e a abertura suavizada histórica. |
| Long | `LongMethod` | `Sma` | Algoritmo de suavização para o bloco comprado (`Sma`, `Ema`, `Smma`, `Lwma`, `Jjma`, `Jurx`, `Parma`, `T3`, `Vidya`, `Ama`). |
| Long | `LongLength` | 12 | Comprimento do filtro de suavização aplicado às séries de abertura/fechamento. |
| Long | `LongPhase` | 15 | Parâmetro de fase para filtros estilo Jurik (mantido por compatibilidade). |
| Short | `ShortCandleType` | H4 | Período usado pelo bloco RJTX vendido. |
| Short | `ShortVolume` | 0.1 | Volume aberto quando um sinal vendido é executado. |
| Short | `ShortAllowOpen` | `true` | Habilitar abertura de posições vendidas. |
| Short | `ShortAllowClose` | `true` | Habilitar fechamento de posições vendidas em correspondências altistas. |
| Short | `ShortStopLossPoints` | 1000 | Distância de stop-loss para operações vendidas em passos de preço (0 desabilita a verificação). |
| Short | `ShortTakeProfitPoints` | 2000 | Distância de take-profit para operações vendidas em passos de preço (0 desabilita a verificação). |
| Short | `ShortSignalBar` | 1 | Deslocamento aplicado ao ler buffers RJTX para o bloco vendido. |
| Short | `ShortPeriod` | 10 | Número de barras entre o fechamento suavizado atual e a abertura suavizada histórica. |
| Short | `ShortMethod` | `Sma` | Algoritmo de suavização para o bloco vendido. |
| Short | `ShortLength` | 12 | Comprimento do filtro de suavização aplicado aos sinais vendidos. |
| Short | `ShortPhase` | 15 | Parâmetro de fase para filtros estilo Jurik no bloco vendido. |

## Notas
- `Jjma` corresponde ao Jurik Moving Average. `Jurx`, `Parma` e `Vidya` são aproximados com Zero-Lag EMA, Arnaud Legoux MA e EMA respectivamente, porque o StockSharp não expõe filtros idênticos da biblioteca SmoothAlgorithms.
- A lógica de stop-loss / take-profit é avaliada nos extremos da vela. Spikes intrabarra mais curtos que o máximo/mínimo da vela não acionarão saídas.
- Os sinais são processados apenas em velas completadas; correspondências intrabarra são ignoradas conforme o comportamento `IsNewBar` do MT5.
