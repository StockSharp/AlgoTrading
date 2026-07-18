# Estratégia MACD PatternTrader de Sessão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão direta do consultor especialista do MetaTrader **MacdPatternTraderAll0.01**. Opera um único instrumento usando seis padrões de entrada diferentes baseados em MACD, filtragem opcional de horário de trading, tomada parcial de lucros e uma opção de dimensionamento de posição martingale. Todos os cálculos são realizados em velas completadas entregues pelo `CandleType` configurado.

## Lógica de trading
1. Em cada vela terminada a estratégia atualiza seis indicadores MACD (cada padrão tem seus próprios comprimentos de EMA rápidos e lentos e uma linha de sinal de um período).
2. Se a filtragem de horário de trading estiver habilitada, novas operações só são avaliadas entre `SessionStart` e `SessionEnd`. O gerenciamento de risco está sempre ativo.
3. Cada padrão MACD verifica relações de valores muito específicas entre o valor MACD atual e os dois valores anteriores para detectar reversões de momentum. Quando um padrão é acionado, envia uma ordem de mercado na direção correspondente e define níveis internos de stop-loss e take-profit.
4. O stop-loss é calculado como o extremo recente (máxima mais alta para vendidos, mínima mais baixa para comprados) de um lookback configurável mais/menos um offset medido em passos de preço. O take-profit varre grupos mais antigos de velas em blocos para replicar a busca recursiva de alvo do consultor especialista original.
5. Apenas uma posição líquida é gerenciada por vez. Se um novo sinal aparecer na direção oposta, a posição atual é fechada e uma posição reversa é aberta com o volume ajustado pelo martingale.
6. As posições ativas são monitoradas por `ManageActivePosition`. A lógica emula a rotina de fechamento parcial original:
   - Para comprados: quando o lucro excede `ProfitThreshold` (5 unidades monetárias) e o fechamento anterior está acima da EMA de médio prazo, um terço da posição é vendido. Se o lucro persistir e a máxima anterior estiver acima da média da SMA longa e da EMA muito lenta, metade da posição restante é fechada.
   - Para vendidos: as regras simétricas fecham um terço e depois metade da posição restante quando os objetivos de lucro e os filtros de média móvel são atendidos.
7. O gerenciamento de risco é executado em cada vela independentemente da janela de trading. Se o preço perfura o nível de stop-loss ou take-profit armazenado dentro de uma vela (com base em máximo/mínimo), toda a posição é zerada ao preço de ruptura.
8. Após uma operação ser totalmente fechada, o PnL realizado é avaliado. Quando `UseMartingale` está habilitado, uma operação perdedora dobra o volume da próxima ordem, enquanto qualquer saída lucrativa redefine o volume para o `LotSize` base.

## Padrões-chave
- **Padrão 1:** Detecta picos do MACD acima de `Pattern1MaxThreshold` que começam a cair, e quedas abaixo de `Pattern1MinThreshold` que ricocheteiam.
- **Padrão 2:** Procura cruzamentos do MACD em torno da linha zero com excursões mínimas.
- **Padrão 3:** Usa limiares de dois níveis (`Pattern3MaxThreshold`, `Pattern3SecondaryMax`, `Pattern3MinThreshold`, `Pattern3SecondaryMin`) para detectar reversões de três etapas em ambos os lados. Também conta barras consecutivas acima do máximo secundário para imitar a acumulação `bars_bup` original.
- **Padrão 4:** Opera quando o MACD excede os limiares primários mas a barra anterior fica dentro do intervalo secundário mais estreito, antecipando reversões.
- **Padrão 5:** Responde a rápidas inversões do MACD dentro de intervalos estreitos definidos por `Pattern5PrimaryMax/Min` e os limites secundários.
- **Padrão 6:** Usa contadores (`Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6CountBars`) para exigir múltiplas excursões MACD consecutivas antes de acionar uma operação.

## Gestão de risco
- Os alvos internos de stop-loss e take-profit são recalculados para cada entrada. Os stops usam extremos de preço mais um offset medido em passos de preço. O take-profit busca blocos consecutivos de velas até que um extremo falhe em melhorar, reproduzindo a lógica recursiva do especialista MQL.
- As saídas parciais respeitam o tamanho mínimo de lote original (0.01) e acompanham quantos fechamentos parciais foram executados por direção.
- A estratégia nunca coloca ordens protetoras no corretor; em vez disso, monitora as máximas e mínimas das velas para fechar posições nos preços configurados.

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Série de velas usada para indicadores e sinais de trading. | Velas de 1 hora |
| `LotSize` | Volume de operação base antes dos ajustes martingale. | 0.1 |
| `UseTimeFilter` | Habilitar trading apenas entre `SessionStart` e `SessionEnd`. | true |
| `SessionStart` / `SessionEnd` | Janela de trading (horário local da bolsa). | 07:00 / 17:00 |
| `UseMartingale` | Dobrar `LotSize` após uma operação perdedora. | true |
| `Ema1Period`, `Ema2Period`, `SmaPeriod`, `Ema3Period` | Médias móveis usadas para saídas parciais. | 7, 21, 98, 365 |
| Parâmetros específicos do padrão | Cada padrão tem seu próprio sinalizador de habilitação, lookbacks de stop-loss/take-profit, offsets, comprimentos de EMA e valores de limiar correspondentes às entradas do especialista original. | Ver padrões do construtor |

Todos os limiares e comprimentos de EMA são expostos por meio de objetos `StrategyParam`, permitindo otimização ou ajuste fino.

## Notas
- A estratégia assume que o instrumento fornece `PriceStep` e `PriceStepCost` para traduzir offsets e lucros para a moeda da conta. Quando não disponível, as diferenças de preço são usadas diretamente.
- Stops e alvos são simulados internamente; serão avaliados no fechamento da barra. A execução intrabar em tempo real pode diferir do comportamento do MetaTrader.
- O mecanismo martingale pode aumentar rapidamente a exposição após uma sequência de perdas—use com cautela.
