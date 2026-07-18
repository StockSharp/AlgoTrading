# Estratégia Simples de Crossover FX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Porta de alto nível do consultor especialista MetaTrader 4 *simplefx2.mq4* (Simple FX 2.0).
- Negocia cruzamentos entre uma média móvel simples rápida e lenta em velas acabadas.
- Mantém apenas uma posição aberta e muda quando a tendência dominante se inverte.

## Lógica de negociação
1. Construa velas usando o parâmetro de prazo configurável.
2. Calcule duas médias móveis simples (rápida e lenta) nos preços de fechamento das velas.
3. Confirme uma tendência de alta quando a vela atual e a anterior mostrarem a MM rápida acima da MM lenta. Confirme uma tendência de baixa quando ambas as velas mostrarem a MM rápida abaixo da MM lenta.
4. Quando a tendência confirmada diferir do estado de tendência armazenado, feche qualquer posição oposta e abra imediatamente uma ordem de mercado na nova direção usando o volume configurado.
5. Podem ser habilitadas proteções opcionais de stop-loss e take-profit expressas em etapas de preço. Eles usam o serviço de proteção integrado do StockSharp para emular as configurações de risco do MT4.

A estratégia processa apenas velas finalizadas, nunca ticks intrabar, para permanecer próxima do comportamento original do consultor especialista. O registro é fornecido em cada nova entrada para que cada decisão cruzada possa ser auditada.

## Parâmetros
| Nome | Descrição | Padrão | Otimização |
| --- | --- | --- | --- |
| `ShortPeriod` | Comprimento da média móvel simples rápida. | 50 | 10 → 150 passo 5 |
| `LongPeriod` | Comprimento da média móvel simples lenta. | 200 | 50 → 400 passo 10 |
| `Volume` | Volume de pedidos enviado em cada negociação no mercado. | 0,1 | 0,1 → 2 passo 0,1 |
| `StopLossPoints` | Distância de parada protetora nas etapas de preço do instrumento (0 desabilita). | 0 | - |
| `TakeProfitPoints` | Distância alvo de lucro nas etapas de preço do instrumento (0 desativa). | 0 | - |
| `CandleType` | Candle timeframe used for analysis. | 1 hora | - |

## Notas e diferenças da versão MT4
- O arquivo de persistência MT4 (`simplefx.dat`) não é necessário; a última direção da tendência é rastreada na memória pelo estado da estratégia.
- As opções de derrapagem, comentário do pedido, número mágico e cor da seta do consultor especialista original não são expostas porque StockSharp lida com o roteamento de maneira diferente.
- As distâncias de stop-loss e take-profit são interpretadas em **etapas de preço** (ticks do instrumento). Ajuste-os para corresponder à definição de pip do seu corretor.
- Apenas uma posição pode ser aberta por vez; a estratégia depende de `ClosePosition()` antes de mudar de direção, garantindo uma mudança clara entre negociações longas e curtas.

## Uso
1. Anexe a estratégia a um título/instrumento e defina o período de vela desejado.
2. Configure períodos de média móvel e parâmetros de risco.
3. Inicie a estratégia; ele assinará velas, gerenciará o estado da tendência e enviará ordens de mercado quando um cruzamento for confirmado em duas velas consecutivas.
